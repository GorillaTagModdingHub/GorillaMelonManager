using GorillaMelonManager.Models.Mods;
using GorillaMelonManager.Models.Persistence;
using GorillaMelonManager.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace GorillaMelonManager.Services
{
    public static class ItemInstaller
    {
        public static async Task InstallFromGameBanana(BrowserMod modToInstall)
        {
            using var client = new HttpClient();
            byte[] data = await client.GetByteArrayAsync(modToInstall.DownloadUrl);
            string hash = GetMD5(data);

            if (modToInstall.ValidHash != hash)
                return;

            var metadata = new GameBananaInfo(
                modToInstall.ThumbnailImageUrl,
                modToInstall.ModAuthor,
                modToInstall.ModShortDescription
            );

            ExtractModArchive(data, metadata);
        }

        public static async Task InstallFromUrl(string url, string localPath)
        {
            using var client = HttpUtils.MakeGMClient();

            string fullPath = Path.Combine(ManagerSettings.Default.GamePath, localPath);
            byte[] data = await client.GetByteArrayAsync(url);
            ZipFile.ExtractToDirectory(new MemoryStream(data), fullPath, true);
        }

        // https://stackoverflow.com/questions/42543679/get-md5-checksum-of-byte-arrays-conent-in-c-sharp
        public static string GetMD5(byte[] inputData)
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(inputData, 0, inputData.Length);
            stream.Seek(0, SeekOrigin.Begin);

            using (var md5Instance = MD5.Create())
            {
                var hashResult = md5Instance.ComputeHash(stream);
                return BitConverter.ToString(hashResult).Replace("-", "").ToLowerInvariant();
            }
        }

        private static void ExtractModArchive(byte[] zipBytes, GameBananaInfo? metadata)
        {
            string gamePath = ManagerSettings.Default.GamePath;
            string activeModsPath = DataUtils.Plugins();
            string melonModsPath = Path.Combine(gamePath, "Mods");
            string bepinPluginsPath = Path.Combine(gamePath, "BepInEx", "plugins");

            using var stream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            string? wrapper = GetSingleWrapperFolder(archive.Entries);
            HashSet<string> metadataKeys = [];

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                string relPath = NormalizeEntryPath(entry.FullName);
                if (string.IsNullOrEmpty(relPath))
                    continue;

                if (!string.IsNullOrEmpty(wrapper) && relPath.StartsWith(wrapper, StringComparison.OrdinalIgnoreCase))
                    relPath = relPath[wrapper.Length..];

                if (string.IsNullOrEmpty(relPath))
                    continue;

                var (targetRoot, targetRelativePath) = MapArchivePath(relPath, activeModsPath, melonModsPath, bepinPluginsPath);
                if (string.IsNullOrEmpty(targetRelativePath))
                    continue;

                string destinationPath = Path.GetFullPath(Path.Combine(targetRoot, targetRelativePath));
                string destinationRoot = Path.GetFullPath(targetRoot) + Path.DirectorySeparatorChar;

                if (!destinationPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                    continue;

                string? destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir))
                    Directory.CreateDirectory(destinationDir);

                using var entryStream = entry.Open();
                using var fileStream = File.Create(destinationPath);
                entryStream.CopyTo(fileStream);

                string ext = Path.GetExtension(destinationPath);
                if ((ext.Equals(".dll", StringComparison.OrdinalIgnoreCase) || ext.Equals(".disabled", StringComparison.OrdinalIgnoreCase))
                    && destinationPath.StartsWith(Path.GetFullPath(activeModsPath), StringComparison.OrdinalIgnoreCase))
                {
                    metadataKeys.Add(Path.GetFileNameWithoutExtension(destinationPath));
                }
            }

            if (metadata != null && metadataKeys.Count > 0)
            {
                WriteSharedGameBananaMetadata(activeModsPath, metadataKeys, metadata);
            }
        }

        private static void WriteSharedGameBananaMetadata(string activeModsPath, IEnumerable<string> keys, GameBananaInfo metadata)
        {
            Directory.CreateDirectory(activeModsPath);
            string sharedPath = Path.Combine(activeModsPath, "gamebanana.json");

            var map = new Dictionary<string, GameBananaInfo>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(sharedPath))
            {
                string json = File.ReadAllText(sharedPath);

                var parsedMap = JsonConvert.DeserializeObject<Dictionary<string, GameBananaInfo>>(json);
                if (parsedMap != null)
                {
                    foreach (var kv in parsedMap)
                        map[kv.Key] = kv.Value;
                }
                else
                {
                    var legacySingle = JsonConvert.DeserializeObject<GameBananaInfo>(json);
                    if (legacySingle != null)
                    {
                        foreach (string key in keys)
                            map[key] = legacySingle;
                    }
                }
            }

            foreach (string key in keys)
                map[key] = metadata;

            File.WriteAllText(sharedPath, JsonConvert.SerializeObject(map));
        }

        private static (string targetRoot, string targetRelativePath) MapArchivePath(
            string relPath,
            string activeModsPath,
            string melonModsPath,
            string bepinPluginsPath)
        {
            if (relPath.StartsWith("Mods/", StringComparison.OrdinalIgnoreCase))
                return (melonModsPath, relPath["Mods/".Length..]);

            if (relPath.StartsWith("Plugins/", StringComparison.OrdinalIgnoreCase))
                return (bepinPluginsPath, relPath["Plugins/".Length..]);

            if (relPath.StartsWith("BepInEx/plugins/", StringComparison.OrdinalIgnoreCase))
                return (bepinPluginsPath, relPath["BepInEx/plugins/".Length..]);

            return (activeModsPath, relPath);
        }

        private static string NormalizeEntryPath(string path)
        {
            return path.Replace('\\', '/').TrimStart('/');
        }

        private static string? GetSingleWrapperFolder(IEnumerable<ZipArchiveEntry> entries)
        {
            string? commonFirstSegment = null;

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                string normalized = NormalizeEntryPath(entry.FullName);
                int sep = normalized.IndexOf('/');
                if (sep <= 0)
                    return null;

                string first = normalized[..sep];
                if (commonFirstSegment == null)
                {
                    commonFirstSegment = first;
                    continue;
                }

                if (!commonFirstSegment.Equals(first, StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            if (string.IsNullOrEmpty(commonFirstSegment))
                return null;

            if (commonFirstSegment.Equals("Mods", StringComparison.OrdinalIgnoreCase)
                || commonFirstSegment.Equals("Plugins", StringComparison.OrdinalIgnoreCase)
                || commonFirstSegment.Equals("BepInEx", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return commonFirstSegment + "/";
        }
    }
}

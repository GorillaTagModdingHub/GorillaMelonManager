using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace GorillaMelonManager.Services
{
    public class MelonLoaderService
    {
        private readonly HttpClient _httpClient;

        public MelonLoaderService()
        {
            _httpClient = new HttpClient();
        }

        public enum ModLoaderStatus
        {
            None,
            BepInEx,
            MelonLoader,
            BepInExBackedUp
        }

        public ModLoaderStatus CheckModLoaderStatus(string gorillaTagPath)
        {
            if (string.IsNullOrEmpty(gorillaTagPath) || !Directory.Exists(gorillaTagPath))
            {
                return ModLoaderStatus.None;
            }

            var bepInExPath = Path.Combine(gorillaTagPath, "BepInEx");
            var melonLoaderPath = Path.Combine(gorillaTagPath, "MelonLoader");
            var bepInExBackupPath = Path.Combine(gorillaTagPath, "BepInExBackup");

            if (Directory.Exists(melonLoaderPath))
            {
                return ModLoaderStatus.MelonLoader;
            }

            if (Directory.Exists(bepInExBackupPath))
            {
                return ModLoaderStatus.BepInExBackedUp;
            }

            if (Directory.Exists(bepInExPath))
            {
                return ModLoaderStatus.BepInEx;
            }

            return ModLoaderStatus.None;
        }

        public async Task<bool> BackupBepInExAsync(string gorillaTagPath)
        {
            try
            {
                var bepInExPath = Path.Combine(gorillaTagPath, "BepInEx");
                var backupPath = Path.Combine(gorillaTagPath, "BepInExBackup");

                if (!Directory.Exists(bepInExPath))
                {
                    return false;
                }

                if (Directory.Exists(backupPath))
                {
                    Directory.Delete(backupPath, true);
                }

                Directory.CreateDirectory(backupPath);

                await Task.Run(() => CopyDirectory(bepInExPath, backupPath));
                
                var filesToBackup = new[] { "winhttp.dll", "doorstop_config.ini", "changelog.txt", ".doorstop_version" };
                
                foreach (var fileName in filesToBackup)
                {
                    var sourceFile = Path.Combine(gorillaTagPath, fileName);
                    var destFile = Path.Combine(backupPath, fileName);
                    
                    if (File.Exists(sourceFile))
                    {
                        File.Copy(sourceFile, destFile, true);
                    }
                }

                Directory.Delete(bepInExPath, true);

                foreach (var fileName in filesToBackup)
                {
                    var filePath = Path.Combine(gorillaTagPath, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RestoreBepInExAsync(string gorillaTagPath)
        {
            try
            {
                var backupPath = Path.Combine(gorillaTagPath, "BepInExBackup");
                var bepInExPath = Path.Combine(gorillaTagPath, "BepInEx");
                var melonLoaderPath = Path.Combine(gorillaTagPath, "MelonLoader");

                if (!Directory.Exists(backupPath))
                {
                    return false;
                }

                if (Directory.Exists(melonLoaderPath))
                {
                    Directory.Delete(melonLoaderPath, true);
                }
                
                var melonLoaderFiles = new[] { "version.dll", "dobby.dll" };
                foreach (var fileName in melonLoaderFiles)
                {
                    var filePath = Path.Combine(gorillaTagPath, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                if (Directory.Exists(bepInExPath))
                {
                    Directory.Delete(bepInExPath, true);
                }

                await Task.Run(() => CopyDirectory(backupPath, bepInExPath));

                var filesToRestore = new[] { "winhttp.dll", "doorstop_config.ini", "changelog.txt", ".doorstop_version" };
                foreach (var fileName in filesToRestore)
                {
                    var sourceFile = Path.Combine(backupPath, fileName);
                    var destFile = Path.Combine(gorillaTagPath, fileName);
                    
                    if (File.Exists(sourceFile))
                    {
                        File.Copy(sourceFile, destFile, true);
                    }
                }

                Directory.Delete(backupPath, true);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> InstallMelonLoaderAsync(string gorillaTagPath)
        {
            try
            {
                var melonLoaderPath = Path.Combine(gorillaTagPath, "MelonLoader");
                
                if (Directory.Exists(melonLoaderPath))
                {
                    return true;
                }

                Directory.CreateDirectory(melonLoaderPath);

                const string melonLoaderUrl = "https://github.com/LavaGang/MelonLoader/releases/download/v0.6.5/MelonLoader.x64.zip";
                var tempZipPath = Path.Combine(gorillaTagPath, "MelonLoader_temp.zip");

                try
                {
                    using (var response = await _httpClient.GetAsync(melonLoaderUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = File.Create(tempZipPath))
                        {
                            await contentStream.CopyToAsync(fileStream);
                        }
                    }

                    ZipFile.ExtractToDirectory(tempZipPath, gorillaTagPath, overwriteFiles: true);

                    File.Delete(tempZipPath);

                    return true;
                }
                catch (Exception)
                {
                    if (File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
                    }
                    
                    if (Directory.Exists(melonLoaderPath) && Directory.GetFileSystemEntries(melonLoaderPath).Length == 0)
                    {
                        Directory.Delete(melonLoaderPath);
                    }
                    
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> SetupMelonLoaderAsync(string gorillaTagPath)
        {
            if (string.IsNullOrEmpty(gorillaTagPath) || !Directory.Exists(gorillaTagPath))
            {
                return "Invalid Gorilla Tag install path. Please set a valid path in settings.";
            }

            var status = CheckModLoaderStatus(gorillaTagPath);

            switch (status)
            {
                case ModLoaderStatus.MelonLoader:
                    return "MelonLoader is already installed!";

                case ModLoaderStatus.BepInEx:
                    var backupSuccess = await BackupBepInExAsync(gorillaTagPath);
                    if (!backupSuccess)
                    {
                        return "Failed to backup BepInEx. Installation cancelled.";
                    }
                    
                    var installSuccess = await InstallMelonLoaderAsync(gorillaTagPath);
                    return installSuccess 
                        ? "BepInEx backed up successfully and MelonLoader installed!" 
                        : "BepInEx backed up, but MelonLoader installation failed.";

                case ModLoaderStatus.BepInExBackedUp:
                case ModLoaderStatus.None:
                    var success = await InstallMelonLoaderAsync(gorillaTagPath);
                    return success 
                        ? "MelonLoader installed successfully!" 
                        : "MelonLoader installation failed.";

                default:
                    return "Unknown status.";
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
    }
}


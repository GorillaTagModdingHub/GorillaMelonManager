using System;
using System.IO;

namespace GorillaMelonManager.Models.Persistence
{
    public static class DataUtils
    {
        public static bool DoesPluginsExist()
        {
            return Directory.Exists(Plugins());
        }

        public static string Plugins()
        {
            if (IsMelonLoaderInstalled())
            {
                return Path.Combine(ManagerSettings.Default.GamePath, "Mods");
            }
            return Path.Combine(ManagerSettings.Default.GamePath, "BepInEx", "plugins");
        }

        public static string SetGamePath(string path)
        {
            ManagerSettings.Default.GamePath = path;
            ManagerSettings.Default.Save();
            ManagerSettings.Default.Reload();

            return path;
        }

        public static bool IsBepInExInstalled()
        {
            return Directory.Exists(Path.Combine(ManagerSettings.Default.GamePath, "BepInEx"));
        }

        public static bool IsMelonLoaderInstalled()
        {
            return Directory.Exists(Path.Combine(ManagerSettings.Default.GamePath, "MelonLoader"));
        }

        public static bool IsBepInExBackedUp()
        {
            return Directory.Exists(Path.Combine(ManagerSettings.Default.GamePath, "BepInExBackup"));
        }

        public static bool IsModLoaderInstalled()
        {
            return IsBepInExInstalled() || IsMelonLoaderInstalled();
        }

        public static string GorillaTagExePath()
        {
            return Path.Combine(ManagerSettings.Default.GamePath, "Gorilla Tag.exe");
        }

        public static bool HasValidGamePath()
        {
            string gamePath = ManagerSettings.Default.GamePath;
            return !string.IsNullOrWhiteSpace(gamePath)
                && Directory.Exists(gamePath)
                && File.Exists(GorillaTagExePath());
        }
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GorillaMelonManager.Services;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GorillaMelonManager.Models.Persistence;

namespace GorillaMelonManager.Views;

public partial class Settings : UserControl
{
    private MelonLoaderService _melonLoaderService;

    public Settings()
    {
        InitializeComponent();

        _melonLoaderService = new MelonLoaderService();

        GorilaPath.IsReadOnly = true;

        GorilaPath.Text = ManagerSettings.Default.GamePath;

        HandlePathButtons();
    }

    private void HandlePathButtons()
    {
        InstallMelonLoader.IsEnabled = false;
        UninstallMelonLoader.IsEnabled = false;
        RestoreBepInEx.IsEnabled = false;
        InstallMelInEx.IsEnabled = false;

        if (File.Exists(Path.Combine(ManagerSettings.Default.GamePath, "Gorilla Tag.exe")))
        {
            LaunchGame.IsEnabled = true;
            GotoGame.IsEnabled = true;
                
            if (File.Exists(Path.Combine(ManagerSettings.Default.GamePath, "Plugins/MelInEx.dll")))
                InstallMelInEx.Content = "Uninstall MelInEx";
            else
                InstallMelInEx.Content = "Install MelInEx";
                
            var status = _melonLoaderService.CheckModLoaderStatus(ManagerSettings.Default.GamePath);

            switch (status)
            {
                case MelonLoaderService.ModLoaderStatus.None:
                    InstallMelonLoader.IsEnabled = true;
                    InstallMelonLoader.Content = "Install MelonLoader";
                    break;

                case MelonLoaderService.ModLoaderStatus.BepInEx:
                    InstallMelonLoader.IsEnabled = true;
                    InstallMelonLoader.Content = "Migrate to MelonLoader";
                    break;
                    
                case MelonLoaderService.ModLoaderStatus.MelonLoader:
                    UninstallMelonLoader.IsEnabled = true;
                    InstallMelInEx.IsEnabled = true;
                    break;

                case MelonLoaderService.ModLoaderStatus.BepInExBackedUp:
                    UninstallMelonLoader.IsEnabled = DataUtils.IsMelonLoaderInstalled();
                    InstallMelInEx.IsEnabled = UninstallMelonLoader.IsEnabled;
                    InstallMelonLoader.IsEnabled = !UninstallMelonLoader.IsEnabled;
                    RestoreBepInEx.IsEnabled = true;
                    break;
            }
        }
        else
        {
            LaunchGame.IsEnabled = false;
            GotoGame.IsEnabled = false;
        }
    }

    public async void OnPathClick(object sender, RoutedEventArgs args)
    {
        IReadOnlyList<IStorageFile> files = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Select 'Gorilla Tag.exe'." ,
            FileTypeFilter =
            [
                new FilePickerFileType("Gorilla Filter") 
                { 
                    Patterns =
                    [
                        "Gorilla Tag.exe"
                    ]
                } 
            ]
        });

        if (files.Count <= 0)
            return;

        GorilaPath.Text = DataUtils.SetGamePath(Path.GetDirectoryName(files[0].Path.LocalPath)!);

        string setupResult = await _melonLoaderService.SetupMelonLoaderAsync(ManagerSettings.Default.GamePath);
        await MessageBoxManager
            .GetMessageBoxStandard("MelonLoader Migration", setupResult, ButtonEnum.Ok)
            .ShowAsync();

        HandlePathButtons();
    }

    public async void ModLoaderButtons(object sender, RoutedEventArgs args)
    {
        string objectName = ((Button)sender).Name ?? string.Empty;

        switch (objectName)
        {
            case "InstallMelonLoader":
                var installResult = await _melonLoaderService.SetupMelonLoaderAsync(ManagerSettings.Default.GamePath);
                await MessageBoxManager
                    .GetMessageBoxStandard("MelonLoader Installer", installResult, ButtonEnum.Ok)
                    .ShowAsync();

                HandlePathButtons();
                break;

            case "UninstallMelonLoader":
                var confirmBox = MessageBoxManager
                    .GetMessageBoxStandard("Uninstaller", 
                        "Are you sure you want to uninstall MelonLoader and all associated files including your installed mods?",
                        ButtonEnum.YesNo);

                ButtonResult result = await confirmBox.ShowAsync();

                if (result == ButtonResult.Yes)
                {
                    try
                    {
                        var melonLoaderPath = Path.Combine(ManagerSettings.Default.GamePath, "MelonLoader");
                        var modsPath = Path.Combine(ManagerSettings.Default.GamePath, "Mods");
                        var userDataPath = Path.Combine(ManagerSettings.Default.GamePath, "UserData");
                        var userLibsPath = Path.Combine(ManagerSettings.Default.GamePath, "UserLibs");

                        if (Directory.Exists(melonLoaderPath))
                            Directory.Delete(melonLoaderPath, true);

                        if (Directory.Exists(modsPath))
                            Directory.Delete(modsPath, true);

                        if (Directory.Exists(userDataPath))
                            Directory.Delete(userDataPath, true);

                        if (Directory.Exists(userLibsPath))
                            Directory.Delete(userLibsPath, true);

                        var filesToDelete = new[] { "version.dll", "dobby.dll" };
                        foreach (var fileName in filesToDelete)
                        {
                            var filePath = Path.Combine(ManagerSettings.Default.GamePath, fileName);
                            if (File.Exists(filePath))
                                File.Delete(filePath);
                        }

                        await MessageBoxManager
                            .GetMessageBoxStandard("Uninstaller", "Uninstalled MelonLoader successfully.",
                                ButtonEnum.Ok).ShowAsync();
                    }
                    catch (Exception e)
                    {
                        await MessageBoxManager
                            .GetMessageBoxStandard("Uninstaller", $"Error: {e.Message}",
                                ButtonEnum.Ok).ShowAsync();
                    }

                    HandlePathButtons();
                }
                break;

            case "RestoreBepInEx":
                bool melInEx = false;
                    
                if (File.Exists(Path.Combine(ManagerSettings.Default.GamePath, "Plugins/MelInEx.dll")))
                    melInEx = await MessageBoxManager.GetMessageBoxStandard("Restore BepInEx",
                        "MelInEx detected, restore BepInEx as a MelInEx install?", ButtonEnum.YesNo).ShowAsync() == ButtonResult.Yes;

                ButtonResult restoreResult = ButtonResult.None;
                    
                if (!melInEx)
                {
                    var restoreConfirmBox = MessageBoxManager
                        .GetMessageBoxStandard("Restore BepInEx", 
                            "This will remove MelonLoader and restore your BepInEx installation. Continue?",
                            ButtonEnum.YesNo);

                    restoreResult = await restoreConfirmBox.ShowAsync();
                }

                if (restoreResult == ButtonResult.Yes || melInEx)
                {
                        
                    bool success = await _melonLoaderService.RestoreBepInExAsync(ManagerSettings.Default.GamePath, melInEx);
                        
                    string message = success 
                        ? "BepInEx has been restored successfully!" 
                        : "Failed to restore BepInEx. The backup may not exist.";

                    await MessageBoxManager
                        .GetMessageBoxStandard("Restore BepInEx", message)
                        .ShowAsync();

                    HandlePathButtons();
                }
                break;
            case "InstallMelInEx":
                if (File.Exists(Path.Combine(ManagerSettings.Default.GamePath, "Plugins/MelInEx.dll")))
                {
                    ButtonResult bakResult = await MessageBoxManager
                        .GetMessageBoxStandard("Uninstall MelInEx", "Do you want to back up your BepInEx mods?", 
                            ButtonEnum.YesNo).ShowAsync();

                    if (Directory.Exists(Path.Combine(ManagerSettings.Default.GamePath, "BepInEx")))
                    {
                        if (bakResult == ButtonResult.No)
                            Directory.Delete(Path.Combine(ManagerSettings.Default.GamePath, "BepInEx"), true);
                        else
                        {
                            await _melonLoaderService.ReinstallBepDoorstopAsync(ManagerSettings.Default.GamePath);
                            Directory.Move(Path.Combine(ManagerSettings.Default.GamePath, "BepInEx"), Path.Combine(ManagerSettings.Default.GamePath, "BepInExBackup"));
                        }
                    }
                    File.Delete(Path.Combine(ManagerSettings.Default.GamePath, "Plugins/MelInEx.dll"));
                }
                else
                {
                    bool success = await _melonLoaderService.InstallMelInExAsync(ManagerSettings.Default.GamePath);
                        
                    string message = success 
                        ? "MelInEx has been installed successfully!" 
                        : "MelInEx has failed to install.";
                        
                    await MessageBoxManager
                        .GetMessageBoxStandard("Install MelInEx", message, ButtonEnum.Ok)
                        .ShowAsync();
                }
                    
                HandlePathButtons();
                break;
        }
    }

    public void LaunchGorillaTag(object sender, RoutedEventArgs args)
    {
        if(Directory.GetParent(ManagerSettings.Default.GamePath)?.Name == "common")
        {
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "steam://rungameid/1533390"
            });
        }
        else
        {
            string exeLoc = Path.Combine(ManagerSettings.Default.GamePath, "Gorilla Tag.exe");
            if (File.Exists(exeLoc))
                Process.Start(exeLoc);
        }
    }

    public void GotoGorillaTag(object sender, RoutedEventArgs args)
    {
        if (OperatingSystem.IsLinux())
            Process.Start("xdg-open", "\"" + ManagerSettings.Default.GamePath + "\"");
        else
            Process.Start("explorer", "\"" + ManagerSettings.Default.GamePath + "\"");
    }
}
# Gorilla Melon Manager

![image](https://github.com/GorillaTagModdingHub/GorillaMelonManager/blob/master/GorillaMelonManager/Assets/preview.png?raw=true)

Forked from pl2w/GorillaModManager

# How to make your mod compatible with Gorilla Melon Manager

## Upload it to [GameBanana](https://gamebanana.com/games/9496)

1. Make sure your mod is correctly zipped like this:

   ```
   Mod.Zip
   ├─ Mods
   │  └─ Mod.dll
   └─ Plugins
      └─ Plugin.dll
   ```

2. Make sure the dependencies are correctly listed in GameBanana. \
   It must have a valid zip file link as an url, a GitHub download link or a GameBanana mod link \
   Zip: "https://github.com/legoandmars/Utilla/releases/download/v1.6.13/Utilla.zip" \
   GameBanana: "https://gamebanana.com/mods/507053"

3. Make sure to add the `ModLoader: MelonLoader` tag to your mod under the "Techincal" tab.   

Failure to comply with these three rules will result in your GameBanana mod being unable to be installed from Gorilla Melon Manager.

# How to manually install MelonLoader

1. Backup and delete any BepInEx related folder/files if you have them.
2. [Download MelonLoader](https://github.com/LavaGang/MelonLoader/releases/download/v0.7.2/MelonLoader.x64.zip)
3. Extract the contents of the MelonLoader zip into your game folder
4. Launch the game once to generate mods/plugins folders

Or use the MelonLoader graphical installer:

[Windows](https://github.com/LavaGang/MelonLoader.Installer/releases/latest/download/MelonLoader.Installer.exe)

[Linux](https://github.com/LavaGang/MelonLoader.Installer/releases/latest/download/MelonLoader.Installer.Linux)

# How to manually install mods/plugins

1. Download the Mod/Plugin
2. Put the dll's in their corresponding folders in your game folder

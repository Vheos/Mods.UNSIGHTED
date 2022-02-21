<p align="center">
    <a href="">
        <img src="https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/Logo.png"/>
    </a>
</p>    

<p align="center">
    <a href="">
        <img src="https://img.shields.io/github/v/release/Vheos/Mods.UNSIGHTED?labelColor=808080&color=404040&label=Mod" height=25/>
        <img src="https://img.shields.io/static/v1?labelColor=808080&color=404040&label=Game&message=v1.1.2" height=25/>
        <img src="https://img.shields.io/github/downloads/Vheos/Mods.UNSIGHTED/total?labelColor=808080&color=404040&label=Downloads" height=25/>
    </a>
</p>
    
<p align="center">
    <a href="https://steamcommunity.com/app/1062110/discussions/0/4739473745767880713/">             
        <img src="https://img.shields.io/static/v1?logo=steam&color=0b2961&logoColor=white&label=%20&message=Steam" height=25/>
    </a>
    <a href="https://www.reddit.com/r/UNSIGHTED/comments/rkhnrg/unsighted/">   
        <img src="https://img.shields.io/static/v1?logo=reddit&color=ff4500&logoColor=white&label=%20&message=Reddit" height=25/>
    </a>
    <a href="https://github.com/Vheos/Mods.UNSIGHTED/issues">
        <img src="https://img.shields.io/static/v1?logo=github&color=282828&logoColor=white&label=%20&message=GitHub" height=25/>
    </a>
    <a href="">   
        <img src="https://img.shields.io/static/v1?logo=discord&color=657ac7&logoColor=white&label=%20&message=Vheos%235865" height=25/>
    </a>
    <a href="https://ko-fi.com/Vheos">
        <img src="https://img.shields.io/static/v1?logo=kofi&color=ff5e5b&logoColor=white&label=%20&message=Ko-fi" height="25"/>
    </a>
</p>



# Table of contents
- [UNSIGHTED++?](https://github.com/Vheos/Mods.UNSIGHTED#unsighted)
- [Highlights](https://github.com/Vheos/Mods.UNSIGHTED#highlights)
  - [~~Extra Save Slots~~](https://github.com/Vheos/Mods.UNSIGHTED#extra-save-slots) *(added to core game)*
  - [~~Weapon Loadouts~~](https://github.com/Vheos/Mods.UNSIGHTED#weapon-loadouts) *(added to core game)*
  - [Skin Editor](https://github.com/Vheos/Mods.UNSIGHTED#skin-editor)
  - [Player 2 Equality](https://github.com/Vheos/Mods.UNSIGHTED#player-2-equality)
- [Settings overview](https://github.com/Vheos/Mods.UNSIGHTED#settings-overview-as-of-v100)
- [How to?](https://github.com/Vheos/Mods.UNSIGHTED#how-to)
- [FAQ](https://github.com/Vheos/Mods.UNSIGHTED#faq)
- [Contact](https://github.com/Vheos/Mods.UNSIGHTED#contact)
- [Changelog](https://github.com/Vheos/Mods.UNSIGHTED#changelog)
<br/>



# UNSIGHTED++?
Cool name, huh? Should be called **UNSIGHTED#**, as both the game and this plugin are written in **C#**, but oh well... So, it's my personal collection of mods for the amazing game [UNSIGHTED](https://store.steampowered.com/app/1062110/UNSIGHTED/), which include **co-op improvements**, **difficulty tweaks**, **QoL features** and **audiovisual stuff** - all highly configurable, because who knows better how to enjoy their game than ***YOU!?***

(actually some people might, I wouldn't know)
<br/><br/><br/>



# Highlights

### ~~Extra Save Slots~~ *(added to core game)*
Wow, 8 save slots for the price of 3! You can finally keep mementos of past playthroughs instead of being forced to delete your precious memories ;)

https://user-images.githubusercontent.com/9155825/147118791-82ced823-0b6a-4f56-afe6-80f73d73c33b.mp4

### ~~Weapon Loadouts~~ *(added to core game)*
No more opening menu every minute to switch between shuriken, hookshot, ice-maker and your actual combat weapons. Now you can switch between your favourite loadouts with the press of one button! Comes with in-game **custom controls menu** and **co-op support** :)

https://user-images.githubusercontent.com/9155825/146574388-3daf9b3a-ec14-4690-a4e7-06cfd41fafa9.mp4

### Skin Editor
Let the fashion begin! It's a bit clunky, as the **Configuration Manager** doesn't provide either HSV sliders or numerical RGB input, ***BUT IT WORKS!*** I've even gone the extra mile and written a de/serializer for the color data, so you can copy-paste palettes and share them with others **<3** Take a look at this juice:

https://user-images.githubusercontent.com/9155825/146345939-7f04ae15-d06b-4dfc-9ab0-853e28c80fc1.mp4

### Player 2 Equality
**UNSIGHTED** treats **Player 2** with way more respect than most games (which is  not saying much, as the bar's set pretty low by the gaming industry), but it still suffers from *some* **Player 2** marginalization - namely the archaic and redundant **CAMERA PRIVILEGES!** Take a look at this:

https://user-images.githubusercontent.com/9155825/146352205-7a16275b-ae4b-4003-9c39-272b40fbe07c.mp4

Notice how **Player 1** still has a decent line of sight around them, while **Player 2** is already in the gutter? If that wasn't hurtful enough, they will soon be hurled like a bag of potatoes towards **Player 1**'s location, because god forbid anything *even remotely slows down* the protagonist's adventuring. Now, in case of puzzle games (and metroidvania is partly that) this is somewhat justified, as stretching the screen too wide might spoil the solution. Still, this could be designed around, so as a quick workaround, I implemented `co-op camera stretching`, as well the options to disable `prioritizing player 1` and `teleporting player 2`:

https://user-images.githubusercontent.com/9155825/146353031-51dce301-af60-4b92-8f1f-528a6b5a206e.mp4

<br/>



# Settings overview *(as of [v1.0.0](https://github.com/Vheos/Mods.UNSIGHTED/releases/tag/v1.0.0))*
<details>
    <summary>Time</summary>

- Change the whole engine speed
- Change the in-game timer speed
- Change the cinematic framestop / slowmo
- Override current day, hour and minute

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Time.png)
</details>

<details>
    <summary>Movement</summary>

- Change move/run speed 
- Change run/spin stamina drain 
- Customize `Runner Chip`

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Movement.png)
</details>

<details>
    <summary>Guard</summary>

- Change perfect and normal parry windows 
- Guard longer by holding the button 
- Guard without melee weapons 

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Guard.png)
</details>

<details>
    <summary>Combo</summary>

- Change combo duration and decrease rate 
- Change combo gain values per weapon type 
- Change syringe gained along with combo

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Combo.png)
</details>

<details>
    <summary>Chips & Cogs</summary>

- Change starting chip slots and unlock costs 
- Change number of cog slots 
- Limit number of active cog types

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/ChipsAndCogs.png)
</details>

<details>
    <summary>Camera</summary>

- Change camera zoom to see more 
- Enable co-op screen stretching 
- Put an end to player 2's oppression

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Camera.png)
</details>

<details>
    <summary>UI</summary>

- Hide combat popups 
- Hide current day / time 
- Customize crosshair 
- Customize combo display

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/UI.png)
</details>

<details>
    <summary>Audiovisual</summary>

- Brighten up dark areas 
- Customize Alma's color palette 
- Change volume / pitch of menu SFX

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Audiovisual.png)
</details>

<details>
    <summary>Parry Challenge</summary>
    
- Change spawns and thresholds for each wave
- Change thresholds for getting rewards
- Try out the 5 predefined presets
    
![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/ParryChallenge.png)
</details>

<details>
    <summary>Various</summary>    

- Skip 30sec of intro logos 
- Customize the `Stamina Heal` move
- Scale enemies' and bosses' HP 
- Make enemies in groups attack more often

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Various.png)
</details>
<br/>



# How to?
### • Download [BepInEx](https://github.com/BepInEx/BepInEx/releases/latest/), [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases/latest) and [UNSIGHTED++](https://github.com/Vheos/Mods.UNSIGHTED/releases/latest)

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/HowTo/DownloadFiles.png)

### • For `GamePass` version of the game, follow [this guide](https://www.reddit.com/r/NoMansSkyTheGame/comments/h9z1bd/how_to_mod_xbox_game_pass_nms)
### • For `Steam` and `GOG` versions, extract all files to the game folder

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/HowTo/ExtractFiles.png)

### • Press `F1` in-game to open the `Configuration Manager` window

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/HowTo/ConfigurationManager.png)

### • Choose a mod and click `Apply` to expand its settings

![](https://github.com/Vheos/Mods.UNSIGHTED/blob/master/ReadmeAssets/HowTo/SettingDescription.png)
<br/><br/><br/>



# FAQ
- **How to change the default `Configuration Manager` hotkey?**
    - check out `UNSIGHTED\BepInEx\config\com.bepis.bepinex.configurationmanager.cfg` :)
- **How to unhide a mod?**
    - tick the `Advanced Settings` checkbox at the top of the `Configuration Manager` window and you will see all hidden mods :)
- **Will this mod break my save file?**
    - it shouldn't, but it's a good habit to backup your save files before trying out new stuff :)
- **I found a bug! How to report?**
    - choose one of the contact options below, then describe what's wrong and [pastebin](https://pastebin.com/) output log at `C:\Users\YOUR_USERNAME\AppData\LocalLow\Studio Pixel Punk\UNSIGHTED\output_log.txt`
- **Can I see the source code?**
    - yep, all [my mods](https://github.com/stars/Vheos/lists/mods) are open source! Feel free to study, clone and/or edit the code as you please :)
<br/><br/><br/>



# Contact
- [Steam discussion](https://steamcommunity.com/app/1062110/discussions/0/4739473745767880713/)
- [Reddit post](https://www.reddit.com/r/UNSIGHTED/comments/rkhnrg/unsighted/)
- [GitHub Issue](https://github.com/Vheos/Mods.UNSIGHTED/issues)
- Discord DM: `Vheos#5865`
<br/><br/><br/>



# Changelog
- **1.6.0**
    - updated for game version 1.1.2 (Steam and GOG only)
    - conflicting core game settings and corresponding UNSIGHTED++ changes:
        - `Weapon loadouts`: removed `Menus: Loadouts`
        - `6 Save slots`: didn't remove `Menus: Alternate load menu` as it still grants 2 more slots and displays all the slots on one page
        - `Turn Iris attack on/off`: removed `Various: Iris combat help`
        - `Turn low-health FX on/off`: didn't remove `UI: Hit effect intensity` as it allows for more customization and also works when taking damage and getting frozen
        - `Turn controller vibration on/off`: removed `Various: Gamepad vibrations`
        - `Turn camera shake on/off`: didn't remove `Camera: Shake multiplier` as it allows for more customization
    - `Menus`: renamed `Extra save slots` to `Alternate load menu`    
    - `Audiovisual`: updated Player 2's default color palette
    - `Audiovisual`: added settings to customize loadout-switching text and SFX
- **1.5.0**
    - `Various`: added setting to break crates with guns
    - `Various`: added settings to override current bolts and meteor dusts
    - `Chips & Cogs`: added settings to customize damage taken formula
    - `Chips & Cogs`: added cogs editor (effect, duration, price and color)
    - `Guard`: added settings to customize stun damage multipliers
    - `UI`: added setting to hide total cog uses
    - bugfix: extra saveslots wouldn't work with NG+ popup
    - bugfix: disabling the `Time` mod would reset in-game timer
    - bugfix: placing a meteor weapon on the corrupted pedestal wouldn't remove it from other loadouts, making it possible to have it both placed and equipped
- **1.4.1**
    - `UI`: added setting to reduce the hit / low health effect intensity
    - `Various`: added setting to disable gamepad vibrations
- **1.4.0**
    - added new mod `Fishing` - customize the fishing minigame
    - added new mod `Enemies` - customize the stagger system
    - `Various`: added setting to disable Iris's tutorials and combat help
    - moved some settings from mod `Various` to `Enemies`
- **1.3.0**
    - abstracted extensions for in-game menus
    - renamed mod `Controls` to `Menus`
    - `Menus`: added setting to increase save slots count (experimental)
    - added support for `GamePass` version of the game (experimental)
- **1.2.0**
    - implemented in-game controls extension system
    - added new mod `Controls` - set hotkeys for weapon sets
    - added hidden mod `SFX Player` - test in-game sound effects
    - split mods into 3 sections: `BALANCE`, `QUALITY OF LIFE` and `VARIOUS`
    - `UI`: added setting to override controller icons (PS4 and Xbox)
    - `Parry Challenge`: added settings to customize enemy spawn positions    
- **1.1.0**
    - `Time`: added settings to change current day, hour and minute
    - `UI`: added settings to customize the crosshairs
    - a few last (first?) minute fixes
- **1.0.0**
    - Public release \o/

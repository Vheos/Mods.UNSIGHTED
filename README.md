![](https://dcbadge.vercel.app/api/shield/211827301082464256)



# What is UNSIGHTED++?
Collection of my personal mods for the amazing game [UNSIGHTED](https://store.steampowered.com/app/1062110/UNSIGHTED/)! Those include **co-op improvements**, **difficulty tweaks**, **QoL features** and **audiovisual stuff** - all highly configurable, because who knows better how to enjoy their game than ***YOU!?***

(actually some people might, I wouldn't know)



# How to
- Download [BepInEx](https://github.com/BepInEx/BepInEx/releases/latest/), [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases/latest) and [UNSIGHTED++](https://github.com/Vheos777/Mods.UNSIGHTED/releases/latest)
- Extract all files to the game folder (the one with `UNSIGHTED.exe`)
- Press `F1` in-game to open the `Configuration Manager` window
- Enjoy <3



# Highlights
### Color Palette Editor
One of my proudest features in this mod pack! It's a bit clunky, as the **Configuration Manager** doesn't provide either HSV sliders or numerical RGB input, ***BUT IT WORKS!*** I've even gone the extra mile and written a de/serializer for the color data, so you can copy-paste palettes and share them with others **<3** Take a look at this juicy juice:

https://user-images.githubusercontent.com/9155825/146345939-7f04ae15-d06b-4dfc-9ab0-853e28c80fc1.mp4

### Equal Rights for Player 2
**UNSIGHTED** is an well-designed game, with a lot of modern and concious decisions. But there's a tiny hair in this otherwise perfect meal - the archaic, redundant and deeply rooted in the gaming community (for at least as long as Luigi exists) **MARGINALIZATION OF THE SECOND PLAYER!** Take a look at this:

https://user-images.githubusercontent.com/9155825/146352205-7a16275b-ae4b-4003-9c39-272b40fbe07c.mp4

Notice how **Player 1** still has a decent line of sight around them, while **Player 2** is already in the gutter? If that wasn't hurtful enough, they will soon be forcefully hurled like a bag of potatoes to **Player 1**'s location, because god forbid anything *even remotely slows down* the protagonist's adventuring. Obviously, both issues are addressed in the `Camera` section of the mod pack, and this is how the same scene plays out with `Prioritize player 1` disabled and `Co-op camera stretching` settings disabled:

https://user-images.githubusercontent.com/9155825/146353031-51dce301-af60-4b92-8f1f-528a6b5a206e.mp4



# Settings Overview *(as of [v1.0.0](https://github.com/Vheos777/Mods.UNSIGHTED/releases/tag/v1.0.0))*
<details>
    <summary>Time</summary>

- Change the whole engine speed
- Change the in-game timer speed
- Change the cinematic framestop / slowmo
- Override current day, hour and minute

![](https://github.com/Vheos777/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Time.png)
</details>

<details>
    <summary>Movement</summary>

- Change move/run speed 
- Change run/spin stamina drain 
- Customize `Runner Chip`

![](https://github.com/Vheos777/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Movement.png)
</details>

<details>
    <summary>Guard</summary>

- Change perfect and normal parry windows 
- Guard longer by holding the button 
- Guard without melee weapons 

![](https://github.com/Vheos777/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Guard.png)
</details>

<details>
    <summary>Combo</summary>

- Change combo duration and decrease rate 
- Change combo gain values per weapon type 
- Change syringe gained along with combo

![](https://github.com/Vheos777/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Combo.png)
</details>

<details>
    <summary>Chips & Cogs</summary>

- Change starting chip slots and unlock costs 
- Change number of cog slots 
- Limit number of active cog types

![](https://github.com/Vheos777/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/ChipsAndCogs.png)
</details>

<details>
    <summary>Camera</summary>

- Change camera zoom to see more 
- Enable co-op screen stretching 
- Put an end to player 2's oppression

![](https://github.com/Vheos777/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Camera.png)
</details>

<details>
    <summary>UI</summary>

- Hide combat popups 
- Hide current day / time 
- Customize crosshair 
- Customize combo display

![](https://github.com/Vheos777/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/UI.png)
</details>

<details>
    <summary>Audiovisual</summary>

- Brighten up dark areas 
- Customize Alma's color palette 
- Change volume / pitch of menu SFX

![](https://github.com/Vheos777/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Audiovisual.png)
</details>

<details>
    <summary>Parry Challenge</summary>
    
- Change spawns and thresholds for each wave
- Change thresholds for getting rewards
- Try out the 5 predefined presets
    
![](https://github.com/Vheos777/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/ParryChallenge.png)
</details>

<details>
    <summary>Various</summary>    

- Skip 30sec of intro logos 
- Customize the `Stamina Heal` move
- Scale enemies' and bosses' HP 
- Make enemies in groups attack more often

![](https://github.com/Vheos777/Mods.UNSIGHTED/blob/master/ReadmeAssets/Config%20Screenshots/Various.png)
</details>



# FAQ
- **How to change the default `Configuration Manager` hotkey?**
    - check out `UNSIGHTED\BepInEx\config\com.bepis.bepinex.configurationmanager.cfg` :)
- **How to unhide a mod?**
    - tick the `Advanced Settings` checkbox at the top of the `Configuration Manager` window and you will see all hidden mods :)
- **Will this mod break my save file?**
    - it shouldn't, but it's a good habit to backup your save files before trying out new stuff :)
- **I found a bug! How to report?**
    - choose one of the contact options below, then describe what's wrong and [pastebin](https://pastebin.com/) output log at `C:\Users\Vheos\AppData\LocalLow\Studio Pixel Punk\UNSIGHTED\output_log.txt`
- **Can I see the source code?**
    - yep, all [my mods](https://github.com/stars/Vheos777/lists/mods) are open source! Feel free to study, clone and/or edit the code as you please :)



# Contact Options
- [GitHub Issue](https://github.com/Vheos777/Mods.UNSIGHTED/issues)
- [Reddit post](https://www.reddit.com/r/UNSIGHTED/soon)
- [Steam discussion](https://steamcommunity.com/app/1062110/discussions/0/4739473745767880713/)
- Discord DM: `Vheos#5865`



# Changelog
- **1.1.0**
    - `Time`: added settings to change current day, hour and minute
    - `UI`: added settings to customize the crosshairs
    - a few last (first?) minute fixes
- **1.0.0**
    - Public release \o/

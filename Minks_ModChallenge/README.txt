# Minks_ModChallenge
A 7 Days to Die Mod written in C# for the dedicated server of version A17(.x).


## What does it do
This Mod allows you to challenge another player on a pvp server. The other player gets notified and can accept the challenge. Three minutes later, the challenge starts and both of you get the distance and relative location to each other in a ticker (i.e. "321m north-west") until one of you dies.


## Why?
I made the experience, that on PvP servers people build their homes somewhere hidden. Most likely underground. Understandable, to be not seen is the best way to not get base raided. But you are still for a reason on a PvP server. And have no option for some quick PvP fun.
ModChallenge closes the gap! Equip yourself, get some distance between you and your base, challange someone! Hunt him down or get killed trying... This Mod will get some decent PvP into your servers!


## Server ideas
You can use this Mod just as an additional feature of your PvP server.  
Or you can build a server for the dedicated usage of it: let you players start with level 100 and full equiped and everyone can start to challenge others.


## Requirements
### To use this mod
Allocs Allocs_CommonFunc mod (7dtd-server-fixes.dll) is needed. Ether all 3 of allocs Mods or just the "Allocs_CommonFunc" one is fine.
You can get them [here](https://7dtd.illy.bz/wiki/Server%20fixes "https://7dtd.illy.bz/wiki/Server%20fixes").

## Ingame console commands (F1)
### For users
> Challenge [ <player_name> | giveup]  [ accept | cancel ]  

*Default PermissionLevel 1000*

#### Simple Examples:
    You use:  "Challenge Joe"
    Joe uses: "Challenge YourName accept

    "Challenge" without any parameters shows your current challenge invites.
    To withdraw a challenge invite: "Challenge JoeFarmer cancel" (can only be done before challenge started)
    To deny a challenge request: "Challenge RequesterName cancel"
    To giveup a challenge: "Challenge giveup" (can only be done in a running challange)
    If you want to challenge a user with a space in its name, use "Some Name".

    "withdraw", "deny", "revoke" are aliases for "cancel".

### For admins
> ListAllChallenges [settings]  

*Default PermissionLevel 0*

Lists all challenges in all states (also ended ones)  
If the settings parameter been used, the command prints out the current settings loaded from its xml file.


## Chat commands (t)
Like the console commands, "/Challenge" can be used in the chat window.
The parameters are the same.


## Installation
Download the Minks_ModChallenge.zip file and place the unpacked Folder it in the 7 Days to Die Mods Folder of your dedicated server.
Remember that this Mod depends on allocs server fixes. Make sure you have them in your Mods folder too. See Requirements.
Of course you can also compile your own binary from the source. VS project file is included.


## Configuration
The configuration file is named "Minks_ModChallenge.xml". For an example see [here](https://github.com/Mink80/Minks7daysMods/blob/master/Minks_ModChallenge/Minks_ModChallenge.xml "Minks_ModChallenge/Minks_ModChallenge.xml").
The Mod will look for it in the save game directory of your game. If it will not find one, it creates the file while the first server start with the mod. The default values shown in the example will be used for that.


## Licence
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

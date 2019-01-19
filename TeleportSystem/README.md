# MinksTeleportSystem
A 7 Days to Die Mod written in C#

## What does it do
This mod provides you the possibility to give your current ingame position a name (called "teleport position") and teleport back to it.
A teleport position can be private (only you can see and use it) and global (everyone ono the server can see and use it).
The minimum teleportation delay (default 15 minutes) and the maximum amount of teleport destinations per player (default 5) can be configurated via xml.
Teleport destination names are unique.


## Requirements
### To use this mod
Allocs Allocs_CommonFunc mod (7dtd-server-fixes.dll) is needed. Ether all 3 of allocs Mods or just the "Allocs_CommonFunc" one is fine.
You can get them [here](https://7dtd.illy.bz/wiki/Server%20fixes "https://7dtd.illy.bz/wiki/Server%20fixes").


### To build this mod
See [here](https://github.com/Mink80/Minks7daysMods/blob/master/TeleportSystem/7dtd-binaries/README.txt "TeleportSystem/7dtd-binaries/README.txt").


## Ingame commands
### For users
> Default PermissionLevel 1000
* ListTeleportDestinations (short: ld)
* AddTeleportDestination (short: atd, at)
* TeleportToDestination (short: t)
* ShowTeleportDelay (short d)
* DelTeleportDestination


### For admins
> Default PermissionLevel 0
* ListAllTeleportDestinations - Lists all global and private teleport destinations
* ShowLastTeleports - Shows a table with the last teleport usage of all players


## Installation
Download the Minks_TeleportSystem.zip file [here](http://ge.tt/9mSHY1u2 "Minks_TeleportSystem.zip") and place the unpacked Folder it in the 7 Days to Die Mods Folder of your dedicated server.
Remember that this Mod depends on allocs server fixes. Make sure you have them in your Mods folder too. See Requirements.
Of course you can also compile your own binary from the source. VS project file is included.


## Configuration
The configuration file is named "TeleportSystemConfig.xml". For an example see [here](https://github.com/Mink80/Minks7daysMods/blob/master/TeleportSystem/TeleportSystemConfig.xml "TeleportSystem/TeleportSystemConfig.xml").
The Mod will look for it in the save game directory of your game. If it will not find one, it creates the file while the first server start with the mod. The default values shown in the example will be used for that.


## Licence
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

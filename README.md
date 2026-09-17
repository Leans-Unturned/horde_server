# HordeServer
Transform a survival mode into a horde mode

### Gameplay
- Players can have only two weapons the primary and secondary
- Players cannot manually level up skill
- Players will respawn after every round end

### Recommendations
- [arena_kits](https://github.com/Leans-Unturned/arena_kits) to spawn players with kits

### Preview on Youtube
[![Plugin Preview on Youtube](https://img.youtube.com/vi/QND2QVwtSbo/hqdefault.jpg)](https://youtu.be/QND2QVwtSbo)

## Full Map Template
- [Nuketown](https://steamcommunity.com/sharedfiles/filedetails/?id=3471852710)
- > [Server Configurations](https://github.com/Leans-Unturned/horde_server/tree/master/Templates)

### Get working
The easy way is to get the basic [template](https://github.com/Leans-Unturned/horde_server/tree/master/Templates) from one of the already configurated maps.

Update the configurations from the template or create one, the template must be the same as the map if not the same the plugin will just break.

## Creating a Map compatible

### The Basics
- The player spawn should be the map lobby. (The real spawn will be set in plugin configuration)
- The zombies is recommended to be all stored in one place, (In the game code the zombies cannot be spawned just respawned in a different location, is totally necessary that all available zombies has spawned for the plugin to handle the zombies spawn), recommended to be near the player spawn lobby
- Add only a unique navigation with infinite agro option, so the zombies will automatically hunt the players (The spawned zombies in maps needs to be inside this navigation)
- Setting the real zombie spawn position: to correctly added the zombies spawn you must add a ``Location Node`` with the name ``zombiespawn``, now choose the position you want and place it and the zombies will spawn correctly in that position.
- To set where the player will spawn is the same thing as the zombie spawn but with the name: ``playerspawn``
- Wall buys, to create a wall buy you must use the horde purchase and put the weapon id you want for the player to buy you also need to edit the configuration and add a new entry to the ``AvailableWeaponsToPurchase`` with the weapon id and ammo id, if you forget to add the configuration the buy system will not work.
- Powerups: to add powerups you must add the item id the player will buy on the horde purchase on the map editor and then use the same id in the ``AvailablePowerupsToPurchase`` entry  so the plugin will handle it correctly.
- Pack-A-Punch, to a pack a punch work propertly you must create a new entry for every weapon the player can have in the map. The configuration is: ``AvailablePackAPunch``, you can obtain the ``base64Binary`` by using the command: ``/exportweaponmetadata`` on the currently equipped weapon and the attachments set.
- Every item you put in horde purchase that is not a weapon you must add the id in ``DisabledInventoryIds`` so will be deleted automatically
- Zombies Clothes are selected by the index 1 (the 2 option) on the editor zombies spawn, the index 0 is for the spawn lobby zombies

If everything was done correctly when starting the server, everything should now be working as expected. any questions about the procedures, you can ask in the discussion tab.

# Building

*Windows*: The project uses dotnet 4.8, consider installing into your machine, you need visual studio, simple open the solution file open the Build section and hit the build button (ctrl + shift + b) or you can do into powershell the command dotnet build -c Debug if you have installed dotnet 4.8.

*Linux*: Install dotnet-sdk from your distro package manager, open the root folder of this project and type ``dotnet build -c Debug``.

FTM License.

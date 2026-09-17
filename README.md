# HordeServer
Transform a survival mode into a horde mode

### Gameplay
- Players can have only two weapons the primary and secondary
- Players cannot manually level up skill
- Players will respawn after every round end

### Preview on Youtube
[![Plugin Preview on Youtube](https://img.youtube.com/vi/QND2QVwtSbo/hqdefault.jpg)](https://youtu.be/QND2QVwtSbo)

## Full Map Template
- [Nuketown](https://steamcommunity.com/sharedfiles/filedetails/?id=3471852710)
- > [Server Configurations](https://github.com/Leans-Unturned/horde_server/tree/master/Templates)

### Get working
The easy way is to get the basic [template](https://github.com/Leans-Unturned/horde_server/tree/master/Templates) from one of the already configurated maps.

Update the configurations from the template or create one, the template must be the same as the map if not the same the plugin will just break.

### Creating a Map compatible
- The player spawn should be the map lobby. (The real spawn will be set in plugin configuration)
- The zombies is recommended to be all stored in one place, (In the game code the zombies cannot be spawned just respawned in a different location, is totally necessary that all available zombies has spawned for the plugin to handle the zombies spawn), recommended to be near the player spawn lobby
- Add only a unique navigation with infinite agro option, so the zombies will automatically hunt the players (The spawned zombies in maps needs to be inside this navigation)
- Setting the real zombie spawn position: to correctly added the zombies spawn you must add a ``Location Node`` with the name ``zombiespawn``, now choose the position you want and place it and the zombies will spawn correctly in that position.
- To set where the player will spawn is the same thing as the zombie spawn but with the name: ``playerspawn``

If everything was done correctly when starting the server, everything should now be working as expected. any questions about the procedures, you can ask in the discussion tab.

# Building

*Windows*: The project uses dotnet 4.8, consider installing into your machine, you need visual studio, simple open the solution file open the Build section and hit the build button (ctrl + shift + b) or you can do into powershell the command dotnet build -c Debug if you have installed dotnet 4.8.

*Linux*: Install dotnet-sdk from your distro package manager, open the root folder of this project and type ``dotnet build -c Debug``.

FTM License.

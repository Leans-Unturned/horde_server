# HordeServer
Transform a survival mode into a horde mode

### Preview on Youtube
[![Plugin Preview on Youtube](https://img.youtube.com/vi/8puA4saUrTY/hqdefault.jpg)](https://www.youtube.com/watch?v=8puA4saUrTY)

## TO DO
- Pack a punch
- Weapon reducer damage in config
- Grenade Max Ammo
- Max grenade in inventory in config
- Grenades per round in config

## Fully Map Template
- [Nuketown](https://steamcommunity.com/sharedfiles/filedetails/?id=3471852710)
- [Server Configurations](https://github.com/LeandroTheDev/horde_server/tree/master/Templates)

### Get working
The easy way is to get the basic template from one of the already configurated maps.

First thing you need to change some configuration in dedicated server, go to Configs.json:

```json
"Zombies": {
    "Spawn_Chance": 100.0,
    "Respawn_Day_Time": 999999999.0,
    "Respawn_Night_Time": 999999999.0,
    "Damage_Multiplier": 1.0,
    "Armor_Multiplier": 1.0,
},
"Players": {
    "Food_Use_Ticks": 999999999,
    "Water_Use_Ticks": 999999999,
}
```

Now you need to run the server to generate the configurations, change the ``PlayersFolder`` and ``LevelName`` Configurations so the plugin can handle player disconnections correctly.

Update the configurations, from the template or create one.

### The next step is for configurating a new map for horde server
- The player spawn should be the map lobby. (The real spawn will be set in plugin configuration)
- The zombies is recommended to be all stored in one place, (In the game code the zombies cannot be spawned just respawned in a different location, is totally necessary that all available zombies has spawned for the plugin to handle the zombies spawn), recommended to be near the player spawn lobby
- Add only a unique navigation with infinite agro option, so the zombies will automatically hunt the players (The spawned zombies in maps needs to be inside this navigation)

Open the server, join it, and check the console, you will probably view your position coordinates (if you enabled ``DebugPlayerPosition``), go to a location that you want for the players to spawn when a round start, check the console and get the coordinates, edit the plugin configurations and add it to ``PlayerSpawnPositions``

Now you need to implement the ``ZombiesAvailableNodes`` is basically the same as zombie nodes in map editor, enable ``DebugPlayerPosition`` go to specific locations that you want the zombies to spawn, get the coordinates in console and start adding ``ZombiesAvailableNodes``

If everything was done correctly when starting the server, everything should now be working as expected. any questions about the procedures, you can ask in the discussion tab.

### Considerations
- I strongly recommend that you use ``DebugZombies`` config before asking for help in map configuration

# Building

*Windows*: The project uses dotnet 4.8, consider installing into your machine, you need visual studio, simple open the solution file open the Build section and hit the build button (ctrl + shift + b) or you can do into powershell the command dotnet build -c Debug if you have installed dotnet 4.8.

*Linux*: Install dotnet-sdk from your distro package manager, open the root folder of this project and type ``dotnet build -c Debug``.

FTM License.
#!/bin/sh
dotnet build -c Release

game_dir="/home/bobs/System/Devices/Hard Drive/SteamLibrary/steamapps/common/U3DS/Servers/playtoearn3/Rocket/Plugins/"

cp -r ./bin/Release/net48/HordeServer.dll "$game_dir"
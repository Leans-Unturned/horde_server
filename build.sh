#!/bin/sh
echo "Auto build script"

if [ -z "$UNTURNED_SERVER" ]; then
    read -p "Please enter the path to the Unturned server: " UNTURNED_SERVER
    export UNTURNED_SERVER
fi

if [ -z "$UNTURNED_SERVER_NAME" ]; then
    read -p "Please enter the server name: " UNTURNED_SERVER_NAME
    export UNTURNED_SERVER_NAME
fi


game_dir="$UNTURNED_SERVER/Servers/$UNTURNED_SERVER_NAME/Rocket/Plugins/"
library_dir="$UNTURNED_SERVER/Servers/$UNTURNED_SERVER_NAME/Rocket/Libraries/"

dotnet build -c Release
pwd
cp -r ./bin/Release/net48/HordeServer.dll "$game_dir"
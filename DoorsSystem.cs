extern alias UnityEngineCoreModule;

using HordeServer;
using Rocket.Unturned.Player;
using SDG.Unturned;

class DoorSystem
{
    static public void TryOpenDoor(BarricadeDrop barricade, SteamPlayer instigatorClient, ref bool shouldAllow)
    {
        UnityEngineCoreModule.UnityEngine.Vector3? position = barricade.model?.transform?.position;
        UnturnedPlayer player = UnturnedPlayer.FromSteamPlayer(instigatorClient);

        if (position == null || player == null) return;

        Door? door = null;
        foreach (Door mapDoor in HordeServerPlugin.instance!.Configuration.Instance.AvailableDoorsToPurchase)
        {
            if (mapDoor.pos == position && mapDoor.assetId == barricade.asset.id)
            {
                door = mapDoor;
                break;
            }
        }
        if (door == null) return;

        if (player.Experience < door.cost)
        {
            shouldAllow = false;
            ChatManager.serverSendMessage(
                HordeServerPlugin.instance!.Translate("not_enough_money", door.cost),
                new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                null,
                player.SteamPlayer(),
                EChatMode.SAY,
                HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                true
            );
            return;
        }

        // Paying to open a door is not a vendor purchase, do not let it be mistaken for one
        ItemSystem.SuppressNextCreditSpend(player);
        player.Experience -= (uint)door.cost;

        foreach (UnturnedPlayer onlinePlayer in HordeServerPlugin.onlinePlayers)
        {
            if (onlinePlayer.CSteamID.m_SteamID == player.CSteamID.m_SteamID)
            {
                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("door_open", door.cost),
                    new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                    null,
                    onlinePlayer.SteamPlayer(),
                    EChatMode.SAY,
                    HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                    true
                );
            }
            else
            {
                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("door_opened", player.DisplayName, door.cost),
                    new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                    null,
                    onlinePlayer.SteamPlayer(),
                    EChatMode.SAY,
                    HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                    true
                );
            }
        }
    }

    static public void RespawnDoors()
    {
        BarricadeManager.askClearAllBarricades();

        foreach (var door in HordeServerPlugin.instance!.Configuration.Instance.AvailableDoorsToPurchase)
        {
            if (Assets.find(EAssetType.ITEM, door.assetId) is not ItemBarricadeAsset asset)
            {
                Rocket.Core.Logging.Logger.LogError($"Asset {door.assetId} not found.");
                continue;
            }

            // Doors have no player owner, anyone must be able to salvage them to pay and open the path
            asset.shouldBypassPickupOwnership = true;

            Barricade barricade = new(asset)
            {
                health = ushort.MaxValue
            };
            BarricadeManager.dropNonPlantedBarricade(
                barricade,
                door.pos,
                door.rotation,
                0,
                0
            );
        }
    }
}

public class Door
{
    public UnityEngineCoreModule.UnityEngine.Vector3 pos;
    public UnityEngineCoreModule.UnityEngine.Quaternion rotation;
    public int cost;
    public ushort assetId = 30;
}
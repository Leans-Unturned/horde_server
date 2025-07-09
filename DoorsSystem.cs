extern alias UnityEngineCoreModule;

using HordeServer;
using Rocket.Core.Logging;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;

class DoorSystem
{
    /// <summary>
    /// All doors will change ownership to the most proximity player
    /// THIS IS NECESSARY BECAUSE ONLY THE GOD DAMN OWNER CAN SALVAGE THE F**** BARRICADE
    /// </summary>
    static public void RefreshOwnerships()
    {
        BarricadeRegion[,] regions = BarricadeManager.regions;

        int sizeX = regions.GetLength(0);
        int sizeY = regions.GetLength(1);

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                BarricadeRegion region = regions[x, y];

                foreach (var drop in region.drops)
                {
                    var barricade = drop.asset;
                    var transform = drop.model?.transform;

                    if (transform == null)
                        continue;

                    UnturnedPlayer? nearestPlayer = null;
                    float nearestDistance = float.MaxValue;

                    if (HordeServerPlugin.instance!.Configuration.Instance.DebugBarricadesPosition)
                        Logger.Log($"{transform.position} / {transform.rotation}");

                    foreach (UnturnedPlayer player in HordeServerPlugin.alivePlayers)
                    {
                        float actualDistance = UnityEngineCoreModule.UnityEngine.Vector3.Distance(transform.position, player.Position);

                        if (actualDistance < nearestDistance)
                        {
                            nearestPlayer = player;
                            nearestDistance = actualDistance;
                        }
                    }

                    if (nearestPlayer != null)
                    {
                        TaskDispatcher.QueueOnMainThread(() =>
                        {
                            BarricadeManager.changeOwnerAndGroup(transform, nearestPlayer.CSteamID.m_SteamID, 0);
                        });
                    }
                }
            }
        }
    }

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
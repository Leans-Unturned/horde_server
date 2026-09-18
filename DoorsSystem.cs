extern alias UnityEngineCoreModule;

using System.Collections.Generic;
using HordeServer;
using Rocket.Core.Logging;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;

class DoorSystem
{
    static public void RefreshOwnerships()
    {
        List<Door> doors = HordeServerPlugin.instance!.Configuration.Instance.AvailableDoorsToPurchase;
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
                    var transform = drop.model?.transform;

                    if (transform == null)
                        continue;

                    // Only actual configured doors should ever have their ownership refreshed —
                    // without this check every barricade in the world (e.g. EletricSystem's Barbed
                    // Wire fences) gets an owner assigned to whoever is nearest, letting anyone salvage them
                    bool isDoor = false;
                    foreach (Door mapDoor in doors)
                    {
                        if (mapDoor.pos == transform.position && mapDoor.assetId == drop.asset.id)
                        {
                            isDoor = true;
                            break;
                        }
                    }
                    if (!isDoor)
                        continue;

                    UnturnedPlayer? nearestPlayer = null;
                    float nearestDistance = float.MaxValue;

                    if (HordeServerPlugin.instance!.Configuration.Instance.DebugDoors)
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


    // DoorIndexes unlocked this round (a door's DoorIndex is added here once ANY door sharing that
    // index has been opened), read by HordeUtils to decide which "zombiespawn<index>" location nodes
    // are allowed to spawn zombies. Reset every round in RespawnDoors
    static public readonly HashSet<int> OpenedDoorIndexes = [];

    // DebugDoors config: logs every player-placed salvageable barricade's position and rotation,
    // formatted so it can be pasted directly into an AvailableDoorsToPurchase entry
    static public void LogDebugDoorPlacement(BarricadeRegion region, BarricadeDrop drop)
    {
        if (!HordeServerPlugin.instance!.Configuration.Instance.DebugDoors) return;
        if (drop.asset is not ItemBarricadeAsset asset || !asset.isSalvageable) return;

        BarricadeData data = drop.GetServersideData();
        // Barricades spawned by RespawnDoors have no owner, ignore them to avoid log spam every round
        if (data.owner == 0) return;

        UnityEngineCoreModule.UnityEngine.Vector3 pos = data.point;
        UnityEngineCoreModule.UnityEngine.Quaternion rot = data.rotation;
        // eulerAngles is included because the XML config's <rotation> also needs it: Quaternion.eulerAngles
        // has a setter, and since it's serialized after x/y/z/w it silently overrides them on load if wrong
        UnityEngineCoreModule.UnityEngine.Vector3 euler = rot.eulerAngles;

        string message = $"[DebugDoors] assetId {asset.id} ({asset.name}) placed at pos = new({pos.x:F2}f, {pos.y:F2}f, {pos.z:F2}f), rotation = new({rot.x:F5}f, {rot.y:F5}f, {rot.z:F5}f, {rot.w:F5}f), eulerAngles = ({euler.x:F4}, {euler.y:F4}, {euler.z:F4})";

        Rocket.Core.Logging.Logger.Log(message);

        UnturnedPlayer? player = UnturnedPlayer.FromCSteamID(new CSteamID(data.owner));
        if (player != null)
        {
            ChatManager.serverSendMessage(
                message,
                new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                null,
                player.SteamPlayer(),
                EChatMode.SAY,
                HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                true
            );
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

        // Paying to open a door is not a vendor purchase, do not let it be mistaken for one
        ItemSystem.SuppressNextCreditSpend(player);
        player.Experience -= (uint)door.cost;

        // Unlocks every "zombiespawn<DoorIndex>" node sharing this door's index, DoorIndex 0 means
        // this door does not gate any zombie spawn area
        if (door.DoorIndex != 0 && OpenedDoorIndexes.Add(door.DoorIndex))
        {
            if (HordeServerPlugin.instance!.Configuration.Instance.DebugZombies)
                Rocket.Core.Logging.Logger.Log($"[DoorIndex] Index {door.DoorIndex} opened, matching zombiespawn nodes are now active");
        }

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
        OpenedDoorIndexes.Clear();

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
    // Not unique: multiple doors can share the same DoorIndex. Opening ANY ONE of them unlocks every
    // "zombiespawn<DoorIndex>" location node sharing that index (see HordeUtils.GetZombieSpawnNodePositions).
    // 0 means this door does not gate any zombie spawn area
    public int DoorIndex = 0;
}
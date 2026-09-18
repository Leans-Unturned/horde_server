extern alias UnityEngineCoreModule;

using System.Collections.Generic;
using System.Reflection;
using HordeServer;
using Rocket.Core.Logging;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;

class EletricSystem
{
    // InteractableTrap.zombieDamage is baked per-asset (ItemTrapAsset.zombieDamage) and applied the
    // same way to every instance of the same item, overriding it per spawn is the only way to let
    // each AvailableEletricToPurchase entry deal its own configured damage
    private static readonly FieldInfo? trapZombieDamageField =
        typeof(InteractableTrap).GetField("zombieDamage", BindingFlags.NonPublic | BindingFlags.Instance);

    private class ActiveFence
    {
        public UnityEngineCoreModule.UnityEngine.Transform Transform = null!;
        public System.Timers.Timer RemovalTimer = null!;
        public float PlayerDamage;
    }

    // Fences currently placed in the world, keyed by the EletricFence.id that spawned them, so a
    // purchase on an already-active fence can be refunded instead of double-spawning
    private static readonly Dictionary<ushort, ActiveFence> activeFences = [];

    // How close a player has to be to an active fence to take PlayerDamage on each CheckPlayerDamage tick
    private const float PlayerDamageRadius = 1.5f;

    // The native InteractableTrap only ever damages players when Provider.isPvP is true (see
    // NotifyTrapEntered's Player branch in the SDK), which most PvE-style horde servers never enable,
    // so player damage can't reuse the same reflection override used for zombieDamage. Instead it's
    // applied here via a periodic proximity check, the same style as DoorSystem.RefreshOwnerships,
    // called every second from a timer set up in HordeServerPlugin.
    static public void CheckPlayerDamage()
    {
        if (activeFences.Count == 0) return;

        foreach (ActiveFence fence in activeFences.Values)
        {
            if (fence.PlayerDamage <= 0f) continue;

            UnityEngineCoreModule.UnityEngine.Vector3 fencePosition = fence.Transform.position;

            foreach (UnturnedPlayer player in HordeServerPlugin.alivePlayers)
            {
                if (UnityEngineCoreModule.UnityEngine.Vector3.Distance(fencePosition, player.Position) > PlayerDamageRadius)
                    continue;

                Player? nativePlayer = player.Player;
                if (nativePlayer == null) continue;

                float damage = fence.PlayerDamage;
                TaskDispatcher.QueueOnMainThread(() =>
                {
                    DamageTool.damage(nativePlayer, EDeathCause.SHRED, ELimb.SPINE, CSteamID.Nil, UnityEngineCoreModule.UnityEngine.Vector3.up, damage, 1f, out _, trackKill: false);
                });
            }
        }
    }

    // DebugEletric config: logs every player-placed Barbed Wire's position and rotation,
    // formatted so it can be pasted directly into an AvailableEletricToPurchase entry
    static public void LogDebugEletricPlacement(BarricadeRegion region, BarricadeDrop drop)
    {
        if (!HordeServerPlugin.instance!.Configuration.Instance.DebugEletric) return;
        if (drop.asset is not ItemBarricadeAsset asset || asset.id != 386) return;

        BarricadeData data = drop.GetServersideData();
        // Barricades spawned by EletricSystem itself have no owner, ignore them to avoid log spam
        // every purchase/round
        if (data.owner == 0) return;

        UnityEngineCoreModule.UnityEngine.Vector3 pos = data.point;
        UnityEngineCoreModule.UnityEngine.Quaternion rot = data.rotation;

        string message = $"[DebugEletric] assetId {asset.id} ({asset.name}) placed at pos = new({pos.x:F2}f, {pos.y:F2}f, {pos.z:F2}f), rotation = new({rot.x:F5}f, {rot.y:F5}f, {rot.z:F5}f, {rot.w:F5}f)";

        Logger.Log(message);

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

    static public void TryPurchase(UnturnedPlayer player, EletricFence fence)
    {
        if (activeFences.ContainsKey(fence.id))
        {
            player.Experience += fence.refundValue;
            ChatManager.serverSendMessage(
                HordeServerPlugin.instance!.Translate("refund_eletric_fence", fence.refundValue),
                new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                null,
                player.SteamPlayer(),
                EChatMode.SAY,
                HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                true
            );
            return;
        }

        if (Assets.find(EAssetType.ITEM, fence.assetId) is not ItemBarricadeAsset asset)
        {
            Logger.LogError($"[EletricSystem] Asset {fence.assetId} not found.");
            player.Experience += fence.refundValue;
            return;
        }

        // No owner/group, so RefreshOwnerships-style logic never touches it and players can never
        // gain ownership to salvage it (see Interactable2SalvageBarricade.checkHint)
        Barricade barricade = new(asset)
        {
            health = ushort.MaxValue
        };
        UnityEngineCoreModule.UnityEngine.Transform transform = BarricadeManager.dropNonPlantedBarricade(
            barricade,
            fence.pos,
            fence.rotation,
            0,
            0
        );
        if (transform == null) return;

        if (trapZombieDamageField != null)
        {
            InteractableTrap? trap = transform.GetComponentInChildren<InteractableTrap>();
            if (trap != null) trapZombieDamageField.SetValue(trap, fence.zombieDamage);
        }

        // System.Timers.Timer throws for an interval <= 0, guard against a misconfigured cooldown
        System.Timers.Timer removalTimer = new(fence.cooldown > 0f ? fence.cooldown * 1000 : 1)
        {
            AutoReset = false
        };
        removalTimer.Elapsed += (_, __) => TaskDispatcher.QueueOnMainThread(() => RemoveFence(fence.id));
        removalTimer.Start();

        activeFences[fence.id] = new ActiveFence { Transform = transform, RemovalTimer = removalTimer, PlayerDamage = fence.playerDamage };

        ChatManager.serverSendMessage(
            HordeServerPlugin.instance!.Translate("eletric_fence_placed"),
            new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
            null,
            player.SteamPlayer(),
            EChatMode.SAY,
            HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
            true
        );
    }

    static private void RemoveFence(ushort fenceId)
    {
        if (!activeFences.TryGetValue(fenceId, out ActiveFence? fence)) return;
        activeFences.Remove(fenceId);

        if (BarricadeManager.tryGetRegion(fence.Transform, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
        {
            BarricadeDrop? drop = region.FindBarricadeByRootTransform(fence.Transform);
            if (drop != null) BarricadeManager.destroyBarricade(drop, x, y, plant);
        }
    }

    // Called on round restart: RoundSystem already wipes every barricade in the world via
    // BarricadeManager.askClearAllBarricades before this runs, so fences only need their tracking
    // (and pending removal timers) cleared, not a second destroy
    static public void ResetRound()
    {
        foreach (ActiveFence fence in activeFences.Values)
        {
            fence.RemovalTimer.Stop();
            fence.RemovalTimer.Dispose();
        }
        activeFences.Clear();
    }
}

public class EletricFence
{
    // Matches the HordePurchaseVolume's Item_ID placed in the map, the item it grants on purchase is
    // used purely as a trigger, it is removed immediately by ItemSystem.OnInventoryAdded
    public ushort id;
    public UnityEngineCoreModule.UnityEngine.Vector3 pos;
    public UnityEngineCoreModule.UnityEngine.Quaternion rotation;
    public ushort assetId = 386; // Barbed Wire
    public float zombieDamage;
    public float playerDamage;
    public uint refundValue;
    // Seconds the fence stays in the world after being purchased before it is automatically removed
    public float cooldown = 60f;
}

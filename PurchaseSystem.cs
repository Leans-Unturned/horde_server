using System;
using HarmonyLib;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace HordeServer
{
    static class PurchaseSystem
    {
        // alreadyOwnedGun = true when the player already had the gun and the game gave ammo instead
        public static event Action<UnturnedPlayer, HordePurchaseVolume, bool>? OnPurchase;

        internal static void Raise(UnturnedPlayer player, HordePurchaseVolume volume, bool alreadyOwnedGun)
            => OnPurchase?.Invoke(player, volume, alreadyOwnedGun);
    }

    [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.ReceivePurchaseRequest))]
    static class PurchasePatch
    {
        private static HordePurchaseVolume? _pending;
        private static bool _pendingAlreadyOwned;

        static void Prefix(PlayerSkills __instance, NetId volumeNetId)
        {
            _pending = null;
            HordePurchaseVolume? node = NetIdRegistry.Get<HordePurchaseVolume>(volumeNetId);
            if (node == null || __instance.experience < node.cost) return;

            _pending = node;
            ItemAsset? asset = Assets.find(EAssetType.ITEM, node.id) as ItemAsset;
            _pendingAlreadyOwned = asset is ItemGunAsset && __instance.player.inventory.HasItemByAsset(asset);
        }

        static void Postfix(PlayerSkills __instance)
        {
            if (_pending == null) return;
            HordePurchaseVolume volume = _pending;
            bool alreadyOwned = _pendingAlreadyOwned;
            _pending = null;
            _pendingAlreadyOwned = false;

            UnturnedPlayer? player = UnturnedPlayer.FromPlayer(__instance.player);
            if (player == null) return;

            PurchaseSystem.Raise(player, volume, alreadyOwned);
        }
    }
}

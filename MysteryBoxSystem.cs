using System.Collections.Generic;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;

namespace HordeServer
{
    class MysteryBoxSystem
    {
        private static readonly System.Random random = new();

        // The item must already be removed from the player's inventory by the caller
        // (ItemSystem.OnInventoryAdded), same as the PowerupLoadout detection it sits next to
        static public void Open(UnturnedPlayer player)
        {
            List<ushort> pool = [];
            foreach (WeaponLoadout weaponLoadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
            {
                if (weaponLoadout.canReceiveOnMysteryBox)
                    pool.Add(weaponLoadout.weapondId);
            }

            if (pool.Count == 0)
            {
                Logger.LogWarning("MysteryBox has no AvailableWeaponsToPurchase entry with canReceiveOnMysteryBox = true, nothing to give");
                return;
            }

            ushort chosenWeaponId = pool[random.Next(pool.Count)];

            // Giving the weapon item re-fires OnInventoryAdded, which already recognizes it as a
            // purchase from AvailableWeaponsToPurchase and runs the full pipeline (slot replacement,
            // ammo, pack-a-punch reset, weaponEquipNextTick). No need to duplicate any of that here.
            player.GiveItem(chosenWeaponId, 1);
        }
    }
}

extern alias UnityEngineCoreModule;

using System;
using System.Collections.Generic;
using System.Linq;
using Rocket.Core.Logging;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace HordeServer
{

    class ItemSystem
    {
        static public Dictionary<UnturnedPlayer, WeaponLoadout> primaryWeapon = [];
        static public Dictionary<UnturnedPlayer, WeaponLoadout> secondaryWeapon = [];

        static private List<KeyValuePair<UnturnedPlayer, Item>> itemSwapped = [];
        // Player / tickrate
        public static readonly Dictionary<UnturnedPlayer, uint> ignoredRefunds = [];
        static bool SwapTickReset = false;

        // Marks a player who just legitimately spent credits (e.g. buying from a vendor), Player / tickrate
        // Ammo refunds are only granted while this is set, so obtaining ammo through loot/crafting/trading
        // cannot be used to farm credits
        private static readonly Dictionary<UnturnedPlayer, uint> pendingCreditSpend = [];
        // Players whose next credit spend should NOT be treated as a purchase (e.g. paying to open a door)
        private static readonly HashSet<UnturnedPlayer> suppressNextCreditSpendEvent = [];

        static public List<PendingWeaponEquip> weaponEquipNextTick = [];

        public class PendingWeaponEquip
        {
            public UnturnedPlayer Player;
            public WeaponLoadout Loadout;
            // How many consecutive ticks the weapon has not been found anywhere in the inventory
            public uint MissedTicks;

            public PendingWeaponEquip(UnturnedPlayer player, WeaponLoadout loadout)
            {
                Player = player;
                Loadout = loadout;
            }
        }
        static private List<KeyValuePair<UnturnedPlayer, Item>> weaponReplaceNextTick = [];
        // Variable to ignore next player receive item handling
        private static readonly List<UnturnedPlayer> weaponInventoryIgnoreNextTick = [];
        // Player / tickrate for weaponInventoryIgnoreNextTick be removed, when tickrate is 0 it will be removed from both variables
        private static readonly Dictionary<UnturnedPlayer, uint> weaponInventoryResetIgnoreNextTickOnNextTick = [];
        private static List<UnturnedPlayer> refreshPrimaryWeaponNextTick = [];
        private static List<UnturnedPlayer> refreshSecondaryWeaponNextTick = [];
        private static List<KeyValuePair<UnturnedPlayer, Item>> removeItemNextTick = [];

        // Call this right before manually deducting credits from a player for something that is NOT
        // a vendor purchase (e.g. paying to open a door), so that spend is not mistaken for a purchase
        static public void SuppressNextCreditSpend(UnturnedPlayer player)
        {
            suppressNextCreditSpendEvent.Remove(player);
            suppressNextCreditSpendEvent.Add(player);
        }

        static public void OnPlayerExperienceChanged(PlayerSkills skills, uint oldExperience)
        {
            // Not a spend (award/refund), ignore
            if (skills.experience >= oldExperience) return;

            UnturnedPlayer? player = UnturnedPlayer.FromPlayer(skills.player);
            if (player == null) return;

            if (suppressNextCreditSpendEvent.Remove(player)) return;

            pendingCreditSpend.Remove(player);
            pendingCreditSpend.Add(player, 2);
        }

        // Cleans up whatever weapon+ammo currently occupies (page, 0) so a misplaced weapon can be
        // relocated there safely, instead of just deleting whatever the player already has equipped
        static private void EvictSlotForRelocation(UnturnedPlayer player, byte page)
        {
            ItemJar? occupant = player.Inventory.getItem(page, 0);
            if (occupant?.item == null) return;

            foreach (WeaponLoadout occupantLoadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
            {
                if (occupantLoadout.weapondId == occupant.item.id)
                {
                    RemovePreviouslyAmmo(player, occupantLoadout.ammoId);
                    break;
                }
            }

            player.Inventory.removeItem(page, 0);
        }

        static private void RemovePreviouslyAmmo(UnturnedPlayer player, int ammoId)
        {

            for (byte page = 0; page < PlayerInventory.PAGES; page++)
            {
                try
                {
                    for (byte j = 0; j < player.Inventory.getItemCount(page); j++)
                    {
                        if (player.Inventory.getItem(page, j).item.id == ammoId)
                        {
                            player.Inventory.removeItem(page, j);
                            j--;
                        }
                    }
                }
                catch (Exception) { }
            }
        }

        static public void OnInventoryAdded(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {
            if (IsDisabledInventoryItem(P))
            {
                removeItemNextTick.Add(new(player, new(inventoryGroup, inventoryIndex, P)));
                return;
            }

            if (IsUnauthorizedWeapon(P))
            {
                removeItemNextTick.Add(new(player, new(inventoryGroup, inventoryIndex, P)));
                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("unauthorized_weapon"),
                    new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                    null,
                    player.SteamPlayer(),
                    EChatMode.SAY,
                    HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                    true
                );
                return;
            }

            // Check if the items is swapped
            for (int i = itemSwapped.Count - 1; i >= 0; i--)
            {
                var entry = itemSwapped[i];
                if (entry.Key == player && entry.Value.item.item.id == P.item.id)
                {
                    // Check if the player removed the mains weapon to the inventory
                    foreach (WeaponLoadout weaponLoadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
                    {
                        // Yes the player put the weapon in inventory
                        if (weaponLoadout.weapondId == P.item.id)
                        {
                            weaponReplaceNextTick.Add(new(player, new(inventoryGroup, inventoryIndex, P)));
                            break;
                        }
                    }
                    return;
                }
            }

            // Checking if is some sort of powerup
            foreach (PowerupLoadout powerUpLoadout in HordeServerPlugin.instance!.Configuration.Instance.AvailablePowerupsToPurchase)
            {
                if (powerUpLoadout.itemId == P.item.id)
                {
                    for (byte page = 0; page < PlayerInventory.PAGES; page++)
                    {
                        try
                        {
                            for (byte j = 0; j < player.Inventory.getItemCount(page); j++)
                            {
                                if (player.Inventory.getItem(page, j).item.id == P.item.id)
                                {
                                    PowerupSystem.GivePlayerPowerupByType(player, powerUpLoadout.powerupType);
                                    player.Inventory.removeItem(page, j);
                                    return;
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                }
            }

            // Ignore weapon receive for this event
            if (weaponInventoryIgnoreNextTick.Contains(player))
            {
                weaponInventoryIgnoreNextTick.Remove(player);
                return;
            }

            // In this situation the item is purchased
            foreach (WeaponLoadout weaponLoadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
            {
                // If is the first weapon give it the ammo for that weapon
                if (P.item.id == weaponLoadout.weapondId)
                {
                    // Remove previously equipped weapon
                    {
                        ItemJar? equippedWeapon;
                        if (weaponLoadout.primary)
                            equippedWeapon = player.Inventory.getItem(0, 0);
                        else
                            equippedWeapon = player.Inventory.getItem(1, 0);

                        // Check if exists (in theory is not necessary but...)
                        if (equippedWeapon != null)
                        {
                            // Check if the weapon id is different from the equipped id
                            if (equippedWeapon.item.id != P.item.id)
                            {
                                // Getting the ammo id
                                foreach (WeaponLoadout checkLoadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
                                {
                                    if (checkLoadout.weapondId == equippedWeapon.item.id)
                                    {
                                        // Removing the ammo and the weapon
                                        RemovePreviouslyAmmo(player, checkLoadout.ammoId);
                                        if (weaponLoadout.primary) player.Inventory.removeItem(0, 0);
                                        else player.Inventory.removeItem(1, 0);

                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // Ignore the next: 2 ticks, before detecting ammo refunds
                    // This is necessary on first buy so the system does not refund for the ammo received
                    // in first buy
                    ignoredRefunds.Remove(player);
                    ignoredRefunds.Add(player, 4);

                    // For some reason in this function the item add is not yet in the inventory, we need to equip in the next tick
                    // And for another reason the player receives ammo of the weapon in the horde purchase volume
                    // and we need to remove it for handling the ammo system in the next tick
                    weaponEquipNextTick.Add(new(player, weaponLoadout));

                    if (weaponLoadout.primary)
                    {
                        refreshPrimaryWeaponNextTick.Add(player);
                        PowerupSystem.ResetPlayerPrimaryPackAPunch(player);
                    }
                    else
                    {
                        refreshSecondaryWeaponNextTick.Add(player);
                        PowerupSystem.ResetPlayerSecondaryPackAPunch(player);
                    }
                }

                // If the player receives ammo, is because he already have the weapon lets refresh the inventory
                if (P.item.id == weaponLoadout.ammoId)
                {
                    // Only refund credits if this ammo actually came from a real credit spend
                    // (vendor purchase), never for ammo obtained via loot, crafting or trading
                    bool purchasedNow = pendingCreditSpend.Remove(player);

                    if (weaponLoadout.ammoRefundValue > 0 && purchasedNow && !ignoredRefunds.ContainsKey(player))
                    {
                        player.Experience += weaponLoadout.ammoRefundValue;

                        ChatManager.serverSendMessage(
                            HordeServerPlugin.instance!.Translate("refund_ammo", weaponLoadout.ammoRefundValue),
                            new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                            null,
                            player.SteamPlayer(),
                            EChatMode.SAY,
                            HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                            true
                        );
                    }

                    RemovePreviouslyAmmo(player, weaponLoadout.ammoId);
                    player.GiveItem(weaponLoadout.ammoId, weaponLoadout.ammoRefilQuantity);
                }
            }
        }

        static public void OnInventoryRemoved(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {
            // Future detection for swapped items
            ItemJar? item = player.Inventory.getItem((byte)inventoryGroup, inventoryIndex);
            if (item != null)
            {
                itemSwapped.Add(new KeyValuePair<UnturnedPlayer, Item>(player, new Item(inventoryGroup, inventoryIndex, P)));

                SwapTickReset = true;
            }
        }

        // Deny dropping weapons/ammo that belong to the horde economy, instead of allowing the drop
        // and then wiping every item on the ground for every player to prevent them being stashed
        static public void OnItemDropped(PlayerInventory inventory, SDG.Unturned.Item item, ref bool shouldAllow)
        {
            bool isTrackedEconomyItem = false;
            foreach (WeaponLoadout weaponLoadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
            {
                if (weaponLoadout.weapondId == item.id || weaponLoadout.ammoId == item.id)
                {
                    isTrackedEconomyItem = true;
                    break;
                }
            }

            if (!isTrackedEconomyItem) return;

            shouldAllow = false;

            UnturnedPlayer? player = UnturnedPlayer.FromPlayer(inventory.player);
            if (player == null) return;

            ChatManager.serverSendMessage(
                HordeServerPlugin.instance!.Translate("weapon_drop_denied"),
                new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                null,
                player.SteamPlayer(),
                EChatMode.SAY,
                HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                true
            );
        }

        static public void RefreshPrimaryLoadout(UnturnedPlayer player)
        {
            ItemJar item = player.Player.inventory.getItem(0, 0);
            if (item?.item == null)
            {
                primaryWeapon.Remove(player);
                return;
            }

            foreach (WeaponLoadout loadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
            {
                if (loadout.weapondId == item.item.id && loadout.primary)
                {
                    primaryWeapon[player] = loadout;
                    return;
                }
            }
        }
        static public void RefreshSecondaryLoadout(UnturnedPlayer player)
        {
            ItemJar item = player.Player.inventory.getItem(1, 0);
            if (item?.item == null)
            {
                secondaryWeapon.Remove(player);
                return;
            }

            foreach (WeaponLoadout loadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
            {
                if (loadout.weapondId == item.item.id && !loadout.primary)
                {
                    secondaryWeapon[player] = loadout;
                    return;
                }
            }
        }

        static public bool IsDisabledInventoryItem(ItemJar P)
        {
            if (HordeServerPlugin.instance!.Configuration.Instance.DisabledInventoryIds.Contains(P.item.id))
                return true;
            else
                return false;
        }

        // Any gun that is not part of the configured loadout is not allowed, no matter how it was
        // obtained (map loot, zombie drop, trading, admin give...). Detecting by asset type instead
        // of a second ID blacklist means it stays correct without the admin having to maintain a list
        // of every other gun on the map
        static public bool IsUnauthorizedWeapon(ItemJar P)
        {
            if (Assets.find(EAssetType.ITEM, P.item.id) is not ItemGunAsset)
                return false;

            foreach (WeaponLoadout weaponLoadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
            {
                if (weaponLoadout.weapondId == P.item.id)
                    return false;
            }

            return true;
        }

        static public void Update()
        {
            { // weaponInventoryResetIgnoreNextTickOnNextTick handler
                // If is 0 remove it from both lists
                foreach (KeyValuePair<UnturnedPlayer, uint> keyValuePair in weaponInventoryResetIgnoreNextTickOnNextTick.ToList())
                {
                    if (keyValuePair.Value == 0)
                    {
                        weaponInventoryIgnoreNextTick.Remove(keyValuePair.Key);
                        weaponInventoryResetIgnoreNextTickOnNextTick.Remove(keyValuePair.Key);
                    }
                    else weaponInventoryResetIgnoreNextTickOnNextTick[keyValuePair.Key]--;
                }
            }

            if (ignoredRefunds.Count > 0)
            {
                foreach (var key in ignoredRefunds.Keys.ToList())
                {
                    ignoredRefunds[key]--;

                    if (ignoredRefunds[key] <= 0)
                    {
                        ignoredRefunds.Remove(key);
                    }
                }
            }

            if (pendingCreditSpend.Count > 0)
            {
                foreach (var key in pendingCreditSpend.Keys.ToList())
                {
                    pendingCreditSpend[key]--;

                    if (pendingCreditSpend[key] <= 0)
                    {
                        pendingCreditSpend.Remove(key);
                    }
                }
            }

            if (SwapTickReset)
                itemSwapped = [];

            if (weaponEquipNextTick.Count > 0)
            {
                // Try equip items
                for (int i = weaponEquipNextTick.Count - 1; i >= 0; i--)
                {
                    var entry = weaponEquipNextTick[i];
                    UnturnedPlayer player = entry.Player;

                    bool equipSuccess = false;
                    bool foundItem = false;
                    for (byte page = 0; page < PlayerInventory.PAGES; page++)
                    {
                        try
                        {
                            byte itemCount = player.Inventory.getItemCount(page);

                            for (byte itemIndex = 0; itemIndex < itemCount; itemIndex++)
                            {
                                ItemJar? item = player.Inventory.getItem(page, itemIndex);
                                if (item == null) continue;

                                if (item.item.id == entry.Loadout.weapondId)
                                {
                                    foundItem = true;

                                    // If the weapon is not on primary or secondary slot, add to it
                                    if (page != 0 && page != 1)
                                    {
                                        player.Inventory.removeItem(page, itemIndex);
                                        if (entry.Loadout.primary) player.Inventory.tryAddItem(item.item, 0, 0, 0, 0);
                                        else player.Inventory.tryAddItem(item.item, 0, 0, 1, 0);
                                        equipSuccess = true;
                                    }
                                    // If the weapon is already on the primary or secondary slot equip it
                                    else
                                    {
                                        // If is not a primary weapon and it is equipped on primary, we need to remove it
                                        // and place on secondary. This can happen when the weapon's real
                                        // in-game equip slot disagrees with its "primary" config flag
                                        if (!entry.Loadout.primary && page == 0)
                                        {
                                            EvictSlotForRelocation(player, 1);
                                            player.Inventory.removeItem(0, 0);
                                            if (!player.Inventory.tryAddItem(item.item, 0, 0, 1, 0))
                                                Logger.LogWarning($"Failed to relocate secondary weapon {entry.Loadout.weapondId} into slot 1 for {player.CSteamID}, check if it is configured with the correct primary/secondary slot");
                                            break;
                                        }
                                        // Symmetric case: a primary weapon ended up in the secondary slot
                                        if (entry.Loadout.primary && page == 1)
                                        {
                                            EvictSlotForRelocation(player, 0);
                                            player.Inventory.removeItem(1, 0);
                                            if (!player.Inventory.tryAddItem(item.item, 0, 0, 0, 0))
                                                Logger.LogWarning($"Failed to relocate primary weapon {entry.Loadout.weapondId} into slot 0 for {player.CSteamID}, check if it is configured with the correct primary/secondary slot");
                                            break;
                                        }
                                        player.Inventory.player.equipment.tryEquip(page, item.x, item.y);

                                        // Only give ammo if weaponInventory is not ignored
                                        if (!weaponInventoryIgnoreNextTick.Contains(player))
                                        {
                                            RemovePreviouslyAmmo(player, entry.Loadout.ammoId);
                                            player.GiveItem(entry.Loadout.ammoId, entry.Loadout.ammoRefilQuantity);

                                            weaponEquipNextTick.RemoveAt(i);

                                            // Why you give ammo 2 times in a row?
                                            // Simple the game code is bugged, the first time you give ammo it will multiply by a strange amount
                                            // The second time is normal
                                            RemovePreviouslyAmmo(player, entry.Loadout.ammoId);
                                            player.GiveItem(entry.Loadout.ammoId, entry.Loadout.ammoRefilQuantity);
                                        }
                                        else
                                        {
                                            weaponInventoryIgnoreNextTick.Remove(player);
                                            weaponEquipNextTick.RemoveAt(i);
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        catch (Exception) { }
                        if (equipSuccess) break;
                    }

                    // The weapon this entry is waiting for is nowhere in the player's inventory
                    // anymore (e.g. dropped, traded or destroyed before this tick). Give up on just
                    // this single entry after a few tries instead of letting it sit forever and
                    // eventually tripping the global safety net below, which would affect every
                    // other player's pending purchases too
                    if (!foundItem)
                    {
                        entry.MissedTicks++;
                        if (entry.MissedTicks > 3)
                        {
                            Logger.LogWarning($"Giving up on equipping weapon {entry.Loadout.weapondId} for {player.CSteamID}, item no longer found in inventory");
                            weaponEquipNextTick.RemoveAt(i);
                        }
                    }
                }

                // Safety net for a scenario the per-entry expiry above does not cover. This is
                // deliberately a high threshold and only clears runaway growth, since a handful of
                // players buying weapons at the same time can legitimately have multiple pending
                // entries at once
                if (weaponEquipNextTick.Count > 20)
                {
                    Logger.LogWarning($"Something is strange in weapon equip system, {weaponEquipNextTick.Count}");
                    weaponEquipNextTick = [];
                }
            }

            if (weaponReplaceNextTick.Count > 0)
            {
                for (int i = weaponReplaceNextTick.Count - 1; i >= 0; i--)
                {
                    var entry = weaponReplaceNextTick[i];
                    weaponInventoryIgnoreNextTick.Remove(entry.Key);
                    weaponInventoryIgnoreNextTick.Add(entry.Key);
                    // Remove before adding: this player may already have a pending entry if both
                    // weapons were moved out of their slots in the same tick (e.g. swapping primary
                    // and secondary), and Dictionary.Add throws on a duplicate key
                    weaponInventoryResetIgnoreNextTickOnNextTick.Remove(entry.Key);
                    weaponInventoryResetIgnoreNextTickOnNextTick.Add(entry.Key, 2); // Ignore for 2 ticks
                    foreach (WeaponLoadout weaponLoadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
                    {
                        if (weaponLoadout.weapondId == entry.Value.item.item.id)
                        {
                            byte[] itemMetadata = entry.Value.item.item.metadata;
                            entry.Key.Inventory.removeItem(entry.Value.inventoryPage, entry.Value.inventoryIndex);

                            SDG.Unturned.Item itemToRespawn = new(entry.Value.item.item.id, true)
                            {
                                amount = 1,
                                metadata = itemMetadata
                            };

                            if (weaponLoadout.primary) entry.Key.Inventory.tryAddItem(itemToRespawn, 0, 0, 0, 0);
                            else entry.Key.Inventory.tryAddItem(itemToRespawn, 0, 0, 1, 0);

                            weaponEquipNextTick.Add(new(entry.Key, weaponLoadout));

                            ChatManager.serverSendMessage(
                                HordeServerPlugin.instance!.Translate("main_weapon_moved"),
                                new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                                null,
                                entry.Key.SteamPlayer(),
                                EChatMode.SAY,
                                HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                                true
                            );

                            continue;
                        }
                    }
                }
                weaponReplaceNextTick = [];
            }

            if (refreshPrimaryWeaponNextTick.Count > 0)
            {
                foreach (UnturnedPlayer player in refreshPrimaryWeaponNextTick)
                {
                    RefreshPrimaryLoadout(player);
                }
                refreshPrimaryWeaponNextTick = [];
            }
            if (refreshSecondaryWeaponNextTick.Count > 0)
            {
                foreach (UnturnedPlayer player in refreshSecondaryWeaponNextTick)
                {
                    RefreshSecondaryLoadout(player);
                }
                refreshSecondaryWeaponNextTick = [];
            }

            if (removeItemNextTick.Count > 0)
            {
                foreach (KeyValuePair<UnturnedPlayer, Item> keyValue in removeItemNextTick)
                    keyValue.Key.Inventory.removeItem(keyValue.Value.inventoryPage, keyValue.Value.inventoryIndex);

                removeItemNextTick = [];
            }


            SwapTickReset = false;
        }
    }
}

class Item
{
    public byte inventoryPage;
    public byte inventoryIndex;
    public ItemJar item;

    public Item(InventoryGroup inventoryPage, byte inventoryIndex, ItemJar item)
    {
        switch (inventoryPage)
        {
            case InventoryGroup.Primary: this.inventoryPage = 0; break;
            case InventoryGroup.Secondary: this.inventoryPage = 1; break;
            case InventoryGroup.Hands: this.inventoryPage = 2; break;
            case InventoryGroup.Backpack: this.inventoryPage = 3; break;
            case InventoryGroup.Vest: this.inventoryPage = 4; break;
            case InventoryGroup.Shirt: this.inventoryPage = 5; break;
            case InventoryGroup.Pants: this.inventoryPage = 6; break;
            case InventoryGroup.Storage: this.inventoryPage = 7; break;
        }
        this.inventoryIndex = inventoryIndex;
        this.item = item;
    }
}
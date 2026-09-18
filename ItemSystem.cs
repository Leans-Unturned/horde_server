extern alias UnityEngineCoreModule;

using System;
using System.Collections.Generic;
using System.Linq;
using Rocket.Core.Logging;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.NetTransport;
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

        // Players currently receiving a kit — OnInventoryAdded skips all checks for them
        internal static readonly HashSet<UnturnedPlayer> kitGiveInProgress = [];

        static public List<PendingWeaponEquip> weaponEquipNextTick = [];

        public class PendingWeaponEquip
        {
            public UnturnedPlayer Player;
            public WeaponLoadout Loadout;
            // Slot (0 primary, 1 secondary) this weapon must end up equipped in, decided at purchase/
            // pack-a-punch/relocation time, since weapons no longer have a fixed slot of their own
            public byte TargetSlot;
            // How many consecutive ticks the weapon has not been found anywhere in the inventory
            public uint MissedTicks;

            public PendingWeaponEquip(UnturnedPlayer player, WeaponLoadout loadout)
            {
                Player = player;
                Loadout = loadout;
            }
        }
        public class PendingWeaponReplace
        {
            public UnturnedPlayer Player;
            // Where the weapon currently is (the bag slot it was manually moved into)
            public Item CurrentLocation;
            // The slot (0 or 1) it was equipped in before being moved, it must snap back there
            public byte TargetSlot;

            public PendingWeaponReplace(UnturnedPlayer player, Item currentLocation, byte targetSlot)
            {
                Player = player;
                CurrentLocation = currentLocation;
                TargetSlot = targetSlot;
            }
        }
        static private List<PendingWeaponReplace> weaponReplaceNextTick = [];
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

            if (HordeServerPlugin.instance!.Configuration.Instance.DebugItems)
                Logger.LogWarning($"[DebugItems] EvictSlot page={page} removing item={occupant.item.id} for {player.CSteamID}");

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
            bool debug = HordeServerPlugin.instance!.Configuration.Instance.DebugItems;

            for (byte page = 0; page < PlayerInventory.PAGES; page++)
            {
                try
                {
                    for (byte j = 0; j < player.Inventory.getItemCount(page); j++)
                    {
                        if (player.Inventory.getItem(page, j).item.id == ammoId)
                        {
                            if (debug)
                                Logger.LogWarning($"[DebugItems] RemoveAmmo ammoId={ammoId} page={page} index={j} for {player.CSteamID}");
                            player.Inventory.removeItem(page, j);
                            j--;
                        }
                    }
                }
                catch (Exception) { }
            }
        }

        // Forces the item sitting at (page,x,y) to be consumed as if the player used it themselves,
        // instead of silently deleting it — the real engine consume flow (UseableConsumeable.
        // startPrimary -> performUseOnSelf) applies the item's own stat effects and removes it
        // automatically once done (see ItemConsumeableAsset.shouldDeleteAfterUse). Returns false if
        // the engine refused to equip it (e.g. player mid-animation of something else, dead, etc.),
        // so the caller can fall back to a plain removeItem.
        static private bool ForcePlayerDrinkItem(UnturnedPlayer player, byte page, byte x, byte y)
        {
            PlayerEquipment equipment = player.Player!.equipment;
            equipment.ServerEquip(page, x, y);

            if (equipment.useable is not UseableConsumeable consumeable
                || equipment.equippedPage != page || equipment.equipped_x != x || equipment.equipped_y != y)
                return false;

            consumeable.startPrimary();

            // startPrimary() broadcasts the use animation/sound to every OTHER nearby client, but
            // deliberately excludes the owner's own connection — it assumes the owner already played
            // it locally from their own input. Since this was forced with no client input, replay
            // the same RPC to the owner alone so they also see/hear themselves drink it.
            ITransportConnection ownerConnection = consumeable.channel.GetOwnerTransportConnection();
            if (ownerConnection != null)
            {
                var replayToOwner = ClientInstanceMethod<EConsumeMode>.Get(typeof(UseableConsumeable), "ReceivePlayConsume");
                replayToOwner.Invoke(consumeable.GetNetId(), ENetReliability.Unreliable, ownerConnection, EConsumeMode.USE);
            }

            return true;
        }

        static public void OnInventoryAdded(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {
            if (kitGiveInProgress.Contains(player))
                return;

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
                            weaponReplaceNextTick.Add(new(player, new(inventoryGroup, inventoryIndex, P), entry.Value.inventoryPage));
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

                                    if (powerUpLoadout.forceDrink)
                                    {
                                        ItemJar drinkItem = player.Inventory.getItem(page, j);
                                        if (!ForcePlayerDrinkItem(player, page, drinkItem.x, drinkItem.y))
                                            player.Inventory.removeItem(page, j);
                                    }
                                    else
                                        player.Inventory.removeItem(page, j);

                                    return;
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                }
            }

            // Checking if is an electric fence (Barbed Wire) purchase
            foreach (EletricFence eletricFence in HordeServerPlugin.instance!.Configuration.Instance.AvailableEletricToPurchase)
            {
                if (eletricFence.id == P.item.id)
                {
                    for (byte page = 0; page < PlayerInventory.PAGES; page++)
                    {
                        try
                        {
                            for (byte j = 0; j < player.Inventory.getItemCount(page); j++)
                            {
                                if (player.Inventory.getItem(page, j).item.id == P.item.id)
                                {
                                    EletricSystem.TryPurchase(player, eletricFence);
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
                    byte targetSlot = weaponLoadout.primary ? (byte)0 : (byte)1;

                    if (HordeServerPlugin.instance!.Configuration.Instance.DebugItems)
                        Logger.LogWarning($"[DebugItems] OnInventoryAdded: weapon={P.item.id} detected as purchase, targetSlot={targetSlot}, player={player.CSteamID}");

                    // Remove previously equipped weapon occupying the target slot
                    {
                        ItemJar? equippedWeapon = player.Inventory.getItem(targetSlot, 0);

                        // Check if exists (in theory is not necessary but...)
                        if (equippedWeapon != null)
                        {
                            if (HordeServerPlugin.instance!.Configuration.Instance.DebugItems)
                                Logger.LogWarning($"[DebugItems] OnInventoryAdded: slot {targetSlot} occupied by item={equippedWeapon.item.id}, sameWeapon={equippedWeapon.item.id == P.item.id}");

                            // Check if the weapon id is different from the equipped id
                            if (equippedWeapon.item.id != P.item.id)
                            {
                                // Getting the ammo id
                                foreach (WeaponLoadout checkLoadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
                                {
                                    if (checkLoadout.weapondId == equippedWeapon.item.id)
                                    {
                                        if (HordeServerPlugin.instance!.Configuration.Instance.DebugItems)
                                            Logger.LogWarning($"[DebugItems] OnInventoryAdded: removing old weapon={equippedWeapon.item.id} from slot {targetSlot} for {player.CSteamID}");
                                        // Removing the ammo and the weapon
                                        RemovePreviouslyAmmo(player, checkLoadout.ammoId);
                                        player.Inventory.removeItem(targetSlot, 0);

                                        break;
                                    }
                                }
                            }
                        }
                        else if (HordeServerPlugin.instance!.Configuration.Instance.DebugItems)
                            Logger.LogWarning($"[DebugItems] OnInventoryAdded: slot {targetSlot} empty, no eviction needed");
                    }

                    // Ignore the next: 2 ticks, before detecting ammo refunds
                    // This is necessary on first buy so the system does not refund for the ammo received
                    // in first buy
                    ignoredRefunds.Remove(player);
                    ignoredRefunds.Add(player, 4);

                    // For some reason in this function the item add is not yet in the inventory, we need to equip in the next tick
                    // And for another reason the player receives ammo of the weapon in the horde purchase volume
                    // and we need to remove it for handling the ammo system in the next tick
                    weaponEquipNextTick.Add(new(player, weaponLoadout) { TargetSlot = targetSlot });

                    if (targetSlot == 0)
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
            if (HordeServerPlugin.instance!.Configuration.Instance.DebugItems)
                Logger.LogWarning($"[DebugItems] OnInventoryRemoved: item={P.item.id} group={inventoryGroup} index={inventoryIndex} for {player.CSteamID}");

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

            if (!isTrackedEconomyItem)
                isTrackedEconomyItem = PowerupSystem.GrenadeItemIds.Contains(item.id);

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

                                    bool debugItems = HordeServerPlugin.instance!.Configuration.Instance.DebugItems;
                                    if (debugItems)
                                        Logger.LogWarning($"[DebugItems] EquipTick: found weapon={entry.Loadout.weapondId} on page={page}, targetSlot={entry.TargetSlot} for {player.CSteamID}");

                                    // Weapon landed somewhere other than its target slot (bag, or the
                                    // other weapon slot, since GiveItem/tryAddItem can put it wherever
                                    // there's room), relocate it there. Equip + ammo happen next tick
                                    // once it's found already sitting in the target slot
                                    if (page != entry.TargetSlot)
                                    {
                                        // Save item data — the game may invalidate the Item reference
                                        // after removeItem, so we work with a fresh copy
                                        ushort savedId = item.item.id;
                                        byte[] savedMetadata = item.item.metadata != null ? (byte[])item.item.metadata.Clone() : [];

                                        if (debugItems)
                                            Logger.LogWarning($"[DebugItems] EquipTick: relocating weapon={savedId} from page={page} to slot={entry.TargetSlot} for {player.CSteamID}");

                                        EvictSlotForRelocation(player, entry.TargetSlot);
                                        // Prevent OnInventoryAdded from treating this system relocation
                                        // as a new purchase and adding another weaponEquipNextTick entry
                                        weaponInventoryIgnoreNextTick.Remove(player);
                                        weaponInventoryIgnoreNextTick.Add(player);

                                        // Try to add to the target slot BEFORE removing from source —
                                        // if the engine rejects the slot (e.g. primary gun can't go in
                                        // secondary slot), we fall back to the page the engine chose
                                        // instead of removing the weapon and losing it entirely
                                        SDG.Unturned.Item relocatedItem = new(savedId, true) { amount = 1, metadata = savedMetadata };
                                        bool relocOk = player.Inventory.tryAddItem(relocatedItem, 0, 0, entry.TargetSlot, 0);
                                        if (relocOk)
                                        {
                                            if (debugItems)
                                                Logger.LogWarning($"[DebugItems] EquipTick: relocation OK weapon={savedId} to slot={entry.TargetSlot}, removing source page={page}");
                                            player.Inventory.removeItem(page, itemIndex);
                                            // Clear itemSwapped so OnInventoryAdded won't trigger weaponReplaceNextTick
                                            itemSwapped.RemoveAll(e => e.Key == player && e.Value.item.item.id == entry.Loadout.weapondId);
                                        }
                                        else
                                        {
                                            // Engine rejected the target slot — weapon type likely doesn't
                                            // match (e.g. gun is primary but config says secondary).
                                            // Accept the page the engine chose so the weapon isn't lost
                                            Logger.LogWarning($"Failed to relocate weapon {entry.Loadout.weapondId} into slot {entry.TargetSlot} for {player.CSteamID}, accepting page {page} instead — check 'primary' flag in config");
                                            weaponInventoryIgnoreNextTick.Remove(player);
                                            entry.TargetSlot = page;
                                        }
                                        equipSuccess = true;
                                    }
                                    // Weapon is already in its target slot, equip it
                                    else
                                    {
                                        player.Inventory.player.equipment.ServerEquip(page, item.x, item.y);

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
                    weaponInventoryIgnoreNextTick.Remove(entry.Player);
                    weaponInventoryIgnoreNextTick.Add(entry.Player);
                    // Remove before adding: this player may already have a pending entry if both
                    // weapons were moved out of their slots in the same tick (e.g. swapping primary
                    // and secondary), and Dictionary.Add throws on a duplicate key
                    weaponInventoryResetIgnoreNextTickOnNextTick.Remove(entry.Player);
                    weaponInventoryResetIgnoreNextTickOnNextTick.Add(entry.Player, 2); // Ignore for 2 ticks
                    foreach (WeaponLoadout weaponLoadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
                    {
                        if (weaponLoadout.weapondId == entry.CurrentLocation.item.item.id)
                        {
                            byte[] itemMetadata = entry.CurrentLocation.item.item.metadata;
                            entry.Player.Inventory.removeItem(entry.CurrentLocation.inventoryPage, entry.CurrentLocation.inventoryIndex);

                            SDG.Unturned.Item itemToRespawn = new(entry.CurrentLocation.item.item.id, true)
                            {
                                amount = 1,
                                metadata = itemMetadata
                            };

                            EvictSlotForRelocation(entry.Player, entry.TargetSlot);
                            entry.Player.Inventory.tryAddItem(itemToRespawn, 0, 0, entry.TargetSlot, 0);

                            weaponEquipNextTick.Add(new(entry.Player, weaponLoadout) { TargetSlot = entry.TargetSlot });

                            ChatManager.serverSendMessage(
                                HordeServerPlugin.instance!.Translate("main_weapon_moved"),
                                new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                                null,
                                entry.Player.SteamPlayer(),
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
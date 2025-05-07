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
        static private List<KeyValuePair<UnturnedPlayer, Item>> itemSwapped = [];
        private static readonly Dictionary<UnturnedPlayer, uint> ignoredRefunds = [];
        static bool SwapTickReset = false;
        static bool ClearItemsNextTick = false;

        static private List<KeyValuePair<UnturnedPlayer, WeaponLoadout>> weaponEquipNextTick = [];
        static private List<KeyValuePair<UnturnedPlayer, Item>> weaponReplaceNextTick = [];
        static private List<UnturnedPlayer> weaponInventoryIgnoreNextTick = [];

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
            // Check if thhe items is swapped
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
                    ignoredRefunds.Remove(player);
                    ignoredRefunds.Add(player, 2);

                    // For some reason in this function the item add is not yet in the inventory, we need to equip in the next tick
                    // And for another reason the player receives ammo of the weapon in the horde purchase volume
                    // and we need to remove it for handling the ammo system in the next tick
                    weaponEquipNextTick.Add(new(player, weaponLoadout));
                }

                // If the player receives ammo, is because he already have the weapon lets refresh the inventory
                if (P.item.id == weaponLoadout.ammoId)
                {
                    if (weaponLoadout.ammoRefundValue > 0 && !ignoredRefunds.ContainsKey(player))
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

        static public void OnItemDropped(PlayerInventory _, SDG.Unturned.Item __, ref bool ___)
        {
            ClearItemsNextTick = true;
        }

        static public void Update()
        {
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

            if (SwapTickReset)
                itemSwapped = [];

            if (ClearItemsNextTick)
                ItemManager.askClearAllItems();

            if (weaponEquipNextTick.Count > 0)
            {
                // Try equip items
                for (int i = weaponEquipNextTick.Count - 1; i >= 0; i--)
                {
                    var entry = weaponEquipNextTick[i];
                    UnturnedPlayer player = entry.Key;

                    bool equipSuccess = false;
                    for (byte page = 0; page < PlayerInventory.PAGES; page++)
                    {
                        try
                        {
                            byte itemCount = player.Inventory.getItemCount(page);

                            for (byte itemIndex = 0; itemIndex < itemCount; itemIndex++)
                            {
                                ItemJar? item = player.Inventory.getItem(page, itemIndex);
                                if (item == null) continue;

                                if (item.item.id == entry.Value.weapondId)
                                {
                                    // If the weapon is not on primary or secondary slot, add to it
                                    if (page != 0 && page != 1)
                                    {
                                        player.Inventory.removeItem(page, itemIndex);
                                        if (entry.Value.primary) player.Inventory.tryAddItem(item.item, 0, 0, 0, 0);
                                        else player.Inventory.tryAddItem(item.item, 0, 0, 1, 0);
                                        equipSuccess = true;
                                    }
                                    // If the weapon is already on the primary or secondary slot equip it
                                    else
                                    {
                                        // If is not a primary weapon and it is equipped on primary, we need to remove it
                                        // and place on secondary
                                        if (!entry.Value.primary && page == 0)
                                        {
                                            player.Inventory.removeItem(0, 0);
                                            player.Inventory.removeItem(1, 0);
                                            player.Inventory.tryAddItem(item.item, 0, 0, 1, 0);
                                            break;
                                        }
                                        player.Inventory.player.equipment.tryEquip(page, item.x, item.y);

                                        // Only give ammo if weaponInventory is not ignored
                                        if (!weaponInventoryIgnoreNextTick.Contains(player))
                                        {
                                            weaponInventoryIgnoreNextTick.Remove(player);

                                            RemovePreviouslyAmmo(player, entry.Value.ammoId);
                                            player.GiveItem(entry.Value.ammoId, entry.Value.ammoRefilQuantity);
                                            weaponEquipNextTick.RemoveAt(i);

                                            // Why you give ammo 2 times in a row?
                                            // Simple the game code is bugged, the first time you give ammo it will multiply by a strange amount
                                            // The second time is normal
                                            RemovePreviouslyAmmo(player, entry.Value.ammoId);
                                            player.GiveItem(entry.Value.ammoId, entry.Value.ammoRefilQuantity);
                                        }
                                        else weaponEquipNextTick.RemoveAt(i);
                                    }
                                    break;
                                }
                            }
                        }
                        catch (Exception) { }
                        if (equipSuccess) break;
                    }

                }

                // Insane bug or exploit treatment
                if (weaponEquipNextTick.Count > 2)
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

                            continue;
                        }
                    }
                }
                weaponReplaceNextTick = [];
            }

            SwapTickReset = false;
            ClearItemsNextTick = false;
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
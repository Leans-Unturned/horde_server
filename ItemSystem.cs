extern alias UnityEngineCoreModule;

using System;
using System.Collections.Generic;
using Rocket.Core.Logging;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace HordeServer
{

    class ItemSystem
    {
        static private List<KeyValuePair<UnturnedPlayer, Item>> itemSwapped = [];
        static bool SwapTickReset = false;
        static bool ClearItemsNextTick = false;

        static public void OnInventoryAdded(UnturnedPlayer player, InventoryGroup _, byte __, ItemJar P)
        {
            // Check if thhe items is swapped
            for (int i = itemSwapped.Count - 1; i >= 0; i--)
            {
                var entry = itemSwapped[i];
                if (entry.Key == player && entry.Value.item.item.id == P.item.id)
                {
                    return;
                }
            }

            // In this situation the item is purchased
            foreach (WeaponLoadout loadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
            {
                void removePreviouslyAmmo()
                {
                    for (byte page = 0; page < PlayerInventory.PAGES; page++)
                    {
                        try
                        {
                            for (byte j = 0; j < player.Inventory.getItemCount(page); j++)
                            {
                                if (player.Inventory.getItem(page, j).item.id == loadout.ammoId)
                                {
                                    player.Inventory.removeItem(page, j);
                                    j--;
                                }
                            }
                        }
                        catch (Exception) { }
                    }

                }

                // If is the first weapon give it the ammo for that weapon
                if (P.item.id == loadout.weapondId)
                {
                    removePreviouslyAmmo();
                    player.GiveItem(loadout.ammoId, loadout.ammoRefilQuantity);
                }

                // If the player receives ammo, is because he already have the weapon lets refresh the inventory
                if (P.item.id == loadout.ammoId)
                {
                    removePreviouslyAmmo();
                    player.GiveItem(loadout.ammoId, loadout.ammoRefilQuantity);
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
            if (SwapTickReset)
                itemSwapped = [];

            if (ClearItemsNextTick)
                ItemManager.askClearAllItems();

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
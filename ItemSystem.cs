using System;
using System.Collections.Generic;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace HordeServer
{

    class ItemSystem
    {
        static private List<KeyValuePair<UnturnedPlayer, Item>> itemSwapped = [];
        static bool CheckNextTick = false;

        static public void OnInventoryAdded(UnturnedPlayer player, InventoryGroup _, byte __, ItemJar P)
        {
            for (int i = itemSwapped.Count - 1; i >= 0; i--)
            {
                var entry = itemSwapped[i];
                if (entry.Key == player && entry.Value.item.item.id == P.item.id)
                {
                    for (byte page = 0; page < PlayerInventory.PAGES; page++)
                    {
                        try
                        {
                            for (byte j = 0; j < player.Inventory.getItemCount(page); j++)
                            {
                                if (player.Inventory.getItem(page, j).item.id == P.item.id)
                                {
                                    player.Inventory.removeItem(page, j);
                                    itemSwapped.RemoveAt(i);
                                    return;
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                }
            }
        }

        static public void OnInventoryRemoved(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {
            ItemManager.askClearAllItems();
            ItemJar? item = player.Inventory.getItem((byte)inventoryGroup, inventoryIndex);
            if (item != null)
            {
                itemSwapped.Add(new KeyValuePair<UnturnedPlayer, Item>(player, new Item(inventoryGroup, inventoryIndex, P)));

                CheckNextTick = true;
            }
        }

        static public void Update()
        {
            if (CheckNextTick)
                itemSwapped = [];

            CheckNextTick = false;
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
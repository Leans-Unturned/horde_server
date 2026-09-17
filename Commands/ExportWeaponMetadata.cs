extern alias UnityEngineCoreModule;

using System;
using System.Collections.Generic;
using System.Linq;
using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityCoreModule = UnityEngineCoreModule.UnityEngine;

namespace HordeServer.Commands;

public class ExportWeaponMetadata : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;

    public string Name => "exportweaponmetadata";

    public string Help => "Show the currently equipped weapon's metadata";

    public string Syntax => "/exportweaponmetadata";

    public List<string> Aliases => [];

    public List<string> Permissions => [];

    public void Execute(IRocketPlayer caller, string[] command)
    {
        UnturnedPlayer player = (UnturnedPlayer)caller;
        PlayerEquipment equipment = player.Player.equipment;

        if (equipment.asset is not ItemGunAsset gunAsset)
        {
            SendMessage("You must have a weapon equipped", player);
            return;
        }

        byte index = player.Inventory.getIndex(equipment.equippedPage, equipment.equipped_x, equipment.equipped_y);
        ItemJar itemJar = player.Inventory.getItem(equipment.equippedPage, index);

        Logger.Log("--------------- Weapon Metadata ---------------");
        Logger.Log($"Item Name: {gunAsset.itemName}");
        Logger.Log($"Item ID: {gunAsset.id}");
        Logger.Log($"GUID: {gunAsset.GUID}");
        if (itemJar?.item.state?.Length > 0)
            Logger.Log($"Item State: {Convert.ToBase64String(itemJar.item.state)} / {string.Join(",", itemJar.item.state.Select(b => b.ToString()))}");
        if (itemJar?.item.metadata?.Length > 0)
            Logger.Log($"Item Metadata: {Convert.ToBase64String(itemJar.item.metadata)} / {string.Join(",", itemJar.item.metadata.Select(b => b.ToString()))}");
        Logger.Log("------------------------------------------------");

        SendMessage("Weapon metadata sent to the server console", player);
    }

    private void SendMessage(string message, UnturnedPlayer player)
    {
        ChatManager.serverSendMessage(
            message,
            new UnityCoreModule.Color(0, 255, 0),
            null,
            player.SteamPlayer(),
            EChatMode.SAY,
            HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
            true
        );
    }
}

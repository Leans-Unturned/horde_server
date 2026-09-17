extern alias UnityEngineCoreModule;

using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityCoreModule = UnityEngineCoreModule.UnityEngine;

namespace HordeServer.Commands
{
    public class KitCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "kit";
        public string Help => "Select a starting kit";
        public string Syntax => "/kit <name>";
        public List<string> Aliases => [];
        public List<string> Permissions => [];

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            var config = HordeServerPlugin.instance!.Configuration.Instance;

            if (config.KitCommandOnlyInArea && !KitSystem.PlayersInArea.Contains(player.Id))
            {
                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("kit_unavailable"),
                    new UnityCoreModule.Color(0, 255, 0),
                    null, player.SteamPlayer(), EChatMode.SAY, config.ChatIconURL, true
                );
                return;
            }

            string kitName = string.Join(" ", command);
            Kit? kit = KitSystem.GetKitByName(kitName);

            if (kit == null || !player.HasPermission($"hordekit.{kit.name}"))
            {
                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("kit_nopermission", kitName),
                    new UnityCoreModule.Color(0, 255, 0),
                    null, player.SteamPlayer(), EChatMode.SAY, config.ChatIconURL, true
                );
                return;
            }

            KitSystem.SetPlayerKit(player.Id, kit.name);
            KitSystem.ClearInventory(player);
            KitSystem.SetSkills(player, kit.experience);
            KitSystem.GiveItems(player, kit);

            ChatManager.serverSendMessage(
                HordeServerPlugin.instance!.Translate("kit_received", kit.name),
                new UnityCoreModule.Color(0, 255, 0),
                null, player.SteamPlayer(), EChatMode.SAY, config.ChatIconURL, true
            );
        }
    }
}

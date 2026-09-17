extern alias UnityEngineCoreModule;

using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityCoreModule = UnityEngineCoreModule.UnityEngine;

namespace HordeServer.Commands
{
    public class ListKitCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "listkit";
        public string Help => "Show available kits";
        public string Syntax => "/listkit";
        public List<string> Aliases => [];
        public List<string> Permissions => [];

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            var config = HordeServerPlugin.instance!.Configuration.Instance;

            List<string> available = [];
            foreach (Kit kit in config.Items)
                if (player.HasPermission($"hordekit.{kit.name}"))
                    available.Add(kit.name);

            ChatManager.serverSendMessage(
                HordeServerPlugin.instance!.Translate("kit_available", string.Join(", ", available)),
                new UnityCoreModule.Color(0, 255, 0),
                null, player.SteamPlayer(), EChatMode.SAY, config.ChatIconURL, true
            );
        }
    }
}

extern alias UnityEngineCoreModule;

using System;
using System.Collections.Generic;
using System.Linq;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace HordeServer
{

    class PowerupSystem
    {
        private static Dictionary<UnturnedPlayer, List<string>> playersPowerups = [];

        public static void GivePlayerPowerupByType(UnturnedPlayer player, string powerupType)
        {
            switch (powerupType)
            {
                case "juggernog": GivePlayerJuggernog(player); return;
                case "speedcola": GivePlayerSpeedCola(player); return;
                case "estaminaup": GivePlayerEstaminaup(player); return;
                case "packapunch": GivePackAPunch(player); return;
                case "grenades": GiveMaxGrenadesForPlayer(player, true); return;
            }
        }

        private static void GivePackAPunch(UnturnedPlayer player)
        {
            ChatManager.serverSendMessage(
                HordeServerPlugin.instance!.Translate("HEY, PACK A PUNCH IS NOT DEVELOPTED YET, WHAT ARE YOU DOING HERE?"),
                new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                null,
                player.SteamPlayer(),
                EChatMode.SAY,
                HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                true
            );

            var refundValue = HordeServerPlugin.instance!.Configuration.Instance.AvailablePowerupsToPurchase
                        .FirstOrDefault(powerup => powerup.powerupType == "packapunch")?.refundValue ?? 0;

            player.Experience += refundValue;
        }

        public static void ResetPlayerPowerups(UnturnedPlayer player)
        {
            if (playersPowerups.TryGetValue(player, out List<string> _)) playersPowerups[player] = [];
            else playersPowerups.Add(player, []);
        }

        public static void ResetPlayersPowerups() => playersPowerups = [];

        private static bool GivePlayerJuggernog(UnturnedPlayer player)
        {
            void increaseSkills()
            {
                SkillSystem.UpdatePlayerSkill(player, "Vitality", 5);
                SkillSystem.UpdatePlayerSkill(player, "Strength", 5);
                SkillSystem.RefreshPlayerSkills(player);

                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("receive_powerup", HordeServerPlugin.instance!.Translate("juggernog")),
                    new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                    null,
                    player.SteamPlayer(),
                    EChatMode.SAY,
                    HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                    true
                );
            }

            if (playersPowerups.TryGetValue(player, out List<string> powerups))
            {
                if (powerups.Contains("juggernog"))
                {
                    var refundValue = HordeServerPlugin.instance!.Configuration.Instance.AvailablePowerupsToPurchase
                        .FirstOrDefault(powerup => powerup.powerupType == "estaminaup")?.refundValue ?? 0;

                    if (refundValue > 0)
                    {
                        player.Experience += refundValue;
                        ChatManager.serverSendMessage(
                            HordeServerPlugin.instance!.Translate("refund_powerup", refundValue),
                            new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                            null,
                            player.SteamPlayer(),
                            EChatMode.SAY,
                            HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                            true
                        );
                    }
                    return false;
                }

                playersPowerups[player].Add("juggernog");
                increaseSkills();
                return true;
            }
            else
            {
                playersPowerups.Add(player, ["juggernog"]);
                increaseSkills();
                return true;
            }
        }

        private static bool GivePlayerSpeedCola(UnturnedPlayer player)
        {
            void increaseSkills()
            {
                SkillSystem.UpdatePlayerSkill(player, "Dexerity", 5);
                SkillSystem.RefreshPlayerSkills(player);

                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("receive_powerup", HordeServerPlugin.instance!.Translate("speedcola")),
                    new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                    null,
                    player.SteamPlayer(),
                    EChatMode.SAY,
                    HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                    true
                );
            }

            if (playersPowerups.TryGetValue(player, out List<string> powerups))
            {
                if (powerups.Contains("speedcola"))
                {
                    var refundValue = HordeServerPlugin.instance!.Configuration.Instance.AvailablePowerupsToPurchase
                        .FirstOrDefault(powerup => powerup.powerupType == "speedcola")?.refundValue ?? 0;

                    if (refundValue > 0)
                    {
                        player.Experience += refundValue;
                        ChatManager.serverSendMessage(
                            HordeServerPlugin.instance!.Translate("refund_powerup", refundValue),
                            new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                            null,
                            player.SteamPlayer(),
                            EChatMode.SAY,
                            HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                            true
                        );
                    }
                    return false;
                }

                playersPowerups[player].Add("speedcola");
                increaseSkills();
                return true;
            }
            else
            {
                playersPowerups.Add(player, ["speedcola"]);
                increaseSkills();
                return true;
            }
        }

        private static bool GivePlayerEstaminaup(UnturnedPlayer player)
        {
            void increaseSkills()
            {
                SkillSystem.UpdatePlayerSkill(player, "Cardio", 5);
                SkillSystem.RefreshPlayerSkills(player);

                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("receive_powerup", HordeServerPlugin.instance!.Translate("estaminaup")),
                    new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                    null,
                    player.SteamPlayer(),
                    EChatMode.SAY,
                    HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                    true
                );
            }

            if (playersPowerups.TryGetValue(player, out List<string> powerups))
            {
                if (powerups.Contains("estaminaup"))
                {
                    var refundValue = HordeServerPlugin.instance!.Configuration.Instance.AvailablePowerupsToPurchase
                        .FirstOrDefault(powerup => powerup.powerupType == "estaminaup")?.refundValue ?? 0;

                    if (refundValue > 0)
                    {
                        player.Experience += refundValue;
                        ChatManager.serverSendMessage(
                            HordeServerPlugin.instance!.Translate("refund_powerup", refundValue),
                            new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                            null,
                            player.SteamPlayer(),
                            EChatMode.SAY,
                            HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                            true
                        );
                    }
                    return false;
                }

                playersPowerups[player].Add("estaminaup");
                increaseSkills();
                return true;
            }
            else
            {
                playersPowerups.Add(player, ["estaminaup"]);
                increaseSkills();
                return true;
            }
        }

        private static ushort GetPlayerCurrentGrenade(UnturnedPlayer player)
        {
            if (playersPowerups.TryGetValue(player, out List<string> value))
            {
                if (value.Contains("some_future_powerup_grenade")) return 1100; // Sticky Grenade
                if (value.Contains("some_future_powerup_grenade")) return 1520; // Impact Grenade
                if (value.Contains("some_future_powerup_grenade")) return 1838; // Bounce Grenade
            }

            return 254; // Fragmentation Grenade
        }

        public static void GiveMaxGrenadesForPlayer(UnturnedPlayer player, bool receiveRefund = false)
        {
            ushort playerGrenade = GetPlayerCurrentGrenade(player);
            byte totalGrenades = 0;
            // Get total grenades
            for (byte page = 0; page < PlayerInventory.PAGES; page++)
            {
                try
                {
                    for (byte j = 0; j < player.Inventory.getItemCount(page); j++)
                    {
                        if (player.Inventory.getItem(page, j).item.id == playerGrenade)
                        {
                            totalGrenades++;
                        }
                    }
                }
                catch (Exception) { }
            }

            uint grenadesToReceive = HordeServerPlugin.instance!.Configuration.Instance.MaxGrenades - totalGrenades;

            if (receiveRefund && grenadesToReceive == 0)
            {
                var refundValue = HordeServerPlugin.instance!.Configuration.Instance.AvailablePowerupsToPurchase
                        .FirstOrDefault(powerup => powerup.powerupType == "grenades")?.refundValue ?? 0;

                player.Experience += refundValue;

                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("refund_powerup", refundValue),
                    new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                    null,
                    player.SteamPlayer(),
                    EChatMode.SAY,
                    HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                    true
                );
                return;
            }

            for (int i = 0; i < grenadesToReceive; i++)
                player.Inventory.tryAddItemAuto(new(playerGrenade, true), false, false, false, false);
        }

        public static void GiveRoundGrenadeForPlayer(UnturnedPlayer player)
        {
            ushort playerGrenade = GetPlayerCurrentGrenade(player);
            byte totalGrenades = 0;
            // Get total grenades
            for (byte page = 0; page < PlayerInventory.PAGES; page++)
            {
                try
                {
                    for (byte j = 0; j < player.Inventory.getItemCount(page); j++)
                    {
                        if (player.Inventory.getItem(page, j).item.id == playerGrenade)
                        {
                            totalGrenades++;
                        }
                    }
                }
                catch (System.Exception) { }
            }

            uint grenadesToReceive = HordeServerPlugin.instance!.Configuration.Instance.MaxGrenades - totalGrenades;
            if (grenadesToReceive > 0)
                player.Inventory.tryAddItemAuto(new(playerGrenade, true), false, false, false, false);
        }

        public static void Disconnect(UnturnedPlayer player) => playersPowerups.Remove(player);
    }
}
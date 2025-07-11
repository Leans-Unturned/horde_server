extern alias UnityEngineCoreModule;

using System;
using System.Collections.Generic;
using System.Linq;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace HordeServer
{

    class PowerupSystem
    {
        private static Dictionary<UnturnedPlayer, List<string>> playersPowerups = [];
        private static Dictionary<UnturnedPlayer, PackAPunchEquippment> packAPunchPrimary = [];
        private static Dictionary<UnturnedPlayer, PackAPunchEquippment> packAPunchSecondary = [];

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
            void UpgradeWeapon(WeaponLoadout loadout, PackAPunchWeapon weapon, int nextLevel)
            {
                if (loadout.primary)
                {
                    if (nextLevel < weapon.AvailableLevelsMetada.Count)
                    {
                        player.Inventory.removeItem(0, 0);
                        SDG.Unturned.Item updatedWeapon = new(loadout.weapondId, true)
                        {
                            amount = 1,
                            state = weapon.AvailableLevelsMetada[nextLevel],
                            metadata = weapon.AvailableLevelsMetada[nextLevel]
                        };

                        player.Inventory.tryAddItem(updatedWeapon, 0, 0, 0, 0);

                        ItemSystem.weaponEquipNextTick.Add(new(player, loadout));
                    }

                    if (nextLevel < weapon.AvailableLevelsDamage.Count)
                    {
                        if (!ItemSystem.primaryWeapon.ContainsKey(player))
                        {
                            Logger.LogWarning("Weapon pack a punch requested but no primary weapon is set on ItemSystem class, requesting refresh manually...");
                            ItemSystem.RefreshPrimaryLoadout(player);
                        }
                        ItemSystem.primaryWeapon[player].baseDamage = weapon.AvailableLevelsDamage[nextLevel];
                    }

                    packAPunchPrimary[player] = new()
                    {
                        id = loadout.weapondId,
                        level = nextLevel
                    };
                }
                else
                {
                    if (nextLevel < weapon.AvailableLevelsMetada.Count)
                    {
                        player.Inventory.removeItem(1, 0);
                        SDG.Unturned.Item updatedWeapon = new(loadout.weapondId, true)
                        {
                            amount = 1,
                            state = weapon.AvailableLevelsMetada[nextLevel],
                            metadata = weapon.AvailableLevelsMetada[nextLevel]
                        };

                        player.Inventory.tryAddItem(updatedWeapon, 0, 0, 1, 0);

                        ItemSystem.weaponEquipNextTick.Add(new(player, loadout));
                    }

                    if (nextLevel < weapon.AvailableLevelsDamage.Count)
                    {
                        if (!ItemSystem.secondaryWeapon.ContainsKey(player))
                        {
                            Logger.LogWarning("Weapon pack a punch requested but no secondary weapon is set on ItemSystem class, requesting refresh manually...");
                            ItemSystem.RefreshSecondaryLoadout(player);
                        }
                        ItemSystem.secondaryWeapon[player].baseDamage = weapon.AvailableLevelsDamage[nextLevel];
                    }

                    packAPunchSecondary[player] = new()
                    {
                        id = loadout.weapondId,
                        level = nextLevel
                    };
                }

                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("packapunch"),
                    new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                    null,
                    player.SteamPlayer(),
                    EChatMode.SAY,
                    HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                    true
                );
            }

            PackAPunchWeapon? weapon = null;
            foreach (PackAPunchWeapon packAPunchWeapon in HordeServerPlugin.instance!.Configuration.Instance.AvailablePackAPunch)
            {
                if (packAPunchWeapon.Id == player.Player?.equipment?.itemID)
                {
                    weapon = packAPunchWeapon;
                    break;
                }
            }
            // If cannot find the weapon is unavailable
            if (weapon == null)
            {
                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("packapunch_unavailable"),
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

                return;
            }

            WeaponLoadout? equippedLoadout = null;
            foreach (WeaponLoadout weaponLoadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
            {
                if (weapon.Id == weaponLoadout.weapondId)
                {
                    equippedLoadout = weaponLoadout;
                    break;
                }
            }
            // If cannot find the loadout is unavailable
            if (equippedLoadout == null)
            {
                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("packapunch_unavailable"),
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

                return;
            }

            if (equippedLoadout.primary)
            {
                if (packAPunchPrimary.TryGetValue(player, out PackAPunchEquippment value))
                {
                    // Check if pack a punch is on max level
                    if (weapon.AvailableLevelsMetada.Count < value.level + 1 && weapon.AvailableLevelsDamage.Count < value.level + 1)
                    {
                        ChatManager.serverSendMessage(
                            HordeServerPlugin.instance!.Translate("packapunch_maxlevel"),
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

                        return;
                    }

                    UpgradeWeapon(equippedLoadout, weapon, value.level + 1);
                }
                else if (weapon.AvailableLevelsMetada.Count > 0 || weapon.AvailableLevelsDamage.Count > 0)
                    UpgradeWeapon(equippedLoadout, weapon, 0);
                else
                {
                    ChatManager.serverSendMessage(
                        HordeServerPlugin.instance!.Translate("packapunch_unavailable"),
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

                    return;
                }
            }
            else
            {
                if (packAPunchSecondary.TryGetValue(player, out PackAPunchEquippment value))
                {
                    // Check if pack a punch is on max level
                    if (weapon.AvailableLevelsMetada.Count < value.level + 1)
                    {
                        ChatManager.serverSendMessage(
                            HordeServerPlugin.instance!.Translate("packapunch_maxlevel"),
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

                        return;
                    }

                    UpgradeWeapon(equippedLoadout, weapon, value.level + 1);
                }
                else if (weapon.AvailableLevelsMetada.Count > 0)
                    UpgradeWeapon(equippedLoadout, weapon, 0);
                else
                {
                    ChatManager.serverSendMessage(
                        HordeServerPlugin.instance!.Translate("packapunch_unavailable"),
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

                    return;
                }
            }
        }

        public static void ResetPlayerPowerups(UnturnedPlayer player)
        {
            if (playersPowerups.TryGetValue(player, out List<string> _)) playersPowerups[player] = [];
            else playersPowerups.Add(player, []);

            ResetPlayerPrimaryPackAPunch(player);
            ResetPlayerSecondaryPackAPunch(player);
        }

        public static void ResetPlayerPrimaryPackAPunch(UnturnedPlayer player) => packAPunchPrimary.Remove(player);
        public static void ResetPlayerSecondaryPackAPunch(UnturnedPlayer player) => packAPunchSecondary.Remove(player);

        public static void ResetPlayersPowerups()
        {
            playersPowerups = [];
            packAPunchPrimary = [];
            packAPunchSecondary = [];
        }

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

public class PackAPunchEquippment
{
    public ushort id;
    public int level;
}

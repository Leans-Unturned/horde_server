extern alias UnityEngineCoreModule;

using System.Collections.Generic;
using Rocket.Unturned.Player;

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
            }
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
            }

            if (playersPowerups.TryGetValue(player, out List<string> powerups))
            {
                if (powerups.Contains("juggernog")) return false;

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
                SkillSystem.UpdatePlayerSkill(player, "Dexterity", 5);
                SkillSystem.RefreshPlayerSkills(player);
            }

            if (playersPowerups.TryGetValue(player, out List<string> powerups))
            {
                if (powerups.Contains("speedcola")) return false;

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
                SkillSystem.UpdatePlayerSkill(player, "cardio", 5);
                SkillSystem.RefreshPlayerSkills(player);
            }

            if (playersPowerups.TryGetValue(player, out List<string> powerups))
            {
                if (powerups.Contains("estaminaup")) return false;

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

        public static void Disconnect(UnturnedPlayer player) => playersPowerups.Remove(player);

    }
}
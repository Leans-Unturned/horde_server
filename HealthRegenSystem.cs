extern alias UnityEngineCoreModule;

using System.Collections.Generic;
using Rocket.Unturned.Player;

namespace HordeServer
{
    class HealthRegenSystem
    {
        // Time.time of the last damage instance taken by a player, of any cause
        private static readonly Dictionary<UnturnedPlayer, float> lastHitTime = [];
        // Fractional health carried over between ticks, since askHeal only accepts whole bytes
        private static readonly Dictionary<UnturnedPlayer, float> pendingHeal = [];
        // HP observed at the end of the previous Update tick, to detect damage from sources that
        // bypass DamageTool.damagePlayerRequested (deadzone, bleeding ticks, etc.)
        private static readonly Dictionary<UnturnedPlayer, byte> previousHealth = [];

        public static void RegisterHit(UnturnedPlayer player)
        {
            lastHitTime[player] = UnityEngineCoreModule.UnityEngine.Time.time;
        }

        public static void Disconnect(UnturnedPlayer player)
        {
            lastHitTime.Remove(player);
            pendingHeal.Remove(player);
            previousHealth.Remove(player);
        }

        public static void Update()
        {
            float duration = HordeServerPlugin.instance!.Configuration.Instance.HealthRegenDuration;
            float delay = HordeServerPlugin.instance!.Configuration.Instance.HealthRegenDelay;
            float healthPerSecond = duration > 0 ? 100f / duration : 0f;
            float now = UnityEngineCoreModule.UnityEngine.Time.time;

            foreach (UnturnedPlayer player in HordeServerPlugin.alivePlayers)
            {
                SDG.Unturned.PlayerLife? life = player.Player?.life;
                if (life == null || life.isDead) continue;

                byte currentHealth = life.health;

                // Detect damage from sources that bypass DamageTool (deadzone, etc.)
                if (previousHealth.TryGetValue(player, out byte prevHealth) && currentHealth < prevHealth)
                    RegisterHit(player);

                previousHealth[player] = currentHealth;

                if (duration <= 0 || currentHealth >= 100)
                {
                    pendingHeal.Remove(player);
                    continue;
                }

                if (!lastHitTime.TryGetValue(player, out float hitTime) || now - hitTime < delay)
                    continue;

                pendingHeal.TryGetValue(player, out float pending);
                pending += healthPerSecond * UnityEngineCoreModule.UnityEngine.Time.deltaTime;

                byte wholeAmount = (byte)System.Math.Min(byte.MaxValue, System.Math.Floor(pending));
                if (wholeAmount > 0)
                {
                    life.askHeal(wholeAmount, false, false);
                    pending -= wholeAmount;
                }

                pendingHeal[player] = pending;
            }
        }
    }
}

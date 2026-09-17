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

        public static void RegisterHit(UnturnedPlayer player)
        {
            lastHitTime[player] = UnityEngineCoreModule.UnityEngine.Time.time;
        }

        public static void Disconnect(UnturnedPlayer player)
        {
            lastHitTime.Remove(player);
            pendingHeal.Remove(player);
        }

        public static void Update()
        {
            float duration = HordeServerPlugin.instance!.Configuration.Instance.HealthRegenDuration;
            if (duration <= 0) return;

            float delay = HordeServerPlugin.instance!.Configuration.Instance.HealthRegenDelay;
            float healthPerSecond = 100f / duration;
            float now = UnityEngineCoreModule.UnityEngine.Time.time;

            foreach (UnturnedPlayer player in HordeServerPlugin.alivePlayers)
            {
                SDG.Unturned.PlayerLife? life = player.Player?.life;
                if (life == null || life.isDead) continue;

                if (life.health >= 100)
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

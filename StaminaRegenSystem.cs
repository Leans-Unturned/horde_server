extern alias UnityEngineCoreModule;

using System.Collections.Generic;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace HordeServer
{
    // Replaces vanilla PlayerLife.SimulateStaminaFrame's passive regen, same role HealthRegenSystem
    // plays for health. Unlike health regen there's no Provider.modeConfigData field gating stamina
    // regen at the source, so it's canceled out reactively every tick instead.
    class StaminaRegenSystem
    {
        // Time.time of the last stamina loss (sprinting/boosting) observed for a player
        private static readonly Dictionary<UnturnedPlayer, float> lastTireTime = [];
        // Fractional stamina carried over between ticks, since askRest only accepts whole bytes
        private static readonly Dictionary<UnturnedPlayer, float> pendingRegen = [];
        // Stamina observed at the end of the previous Update tick, used to detect and cancel out
        // vanilla passive regen ticks (they still fire every frame regardless of this system)
        private static readonly Dictionary<UnturnedPlayer, byte> previousStamina = [];

        public static void Disconnect(UnturnedPlayer player)
        {
            lastTireTime.Remove(player);
            pendingRegen.Remove(player);
            previousStamina.Remove(player);
        }

        public static void Update()
        {
            float delay = HordeServerPlugin.instance!.Configuration.Instance.StaminaRegenDelay;
            float duration = HordeServerPlugin.instance!.Configuration.Instance.StaminaRegenDuration;
            float now = UnityEngineCoreModule.UnityEngine.Time.time;

            foreach (UnturnedPlayer player in HordeServerPlugin.alivePlayers)
            {
                SDG.Unturned.PlayerLife? life = player.Player?.life;
                if (life == null || life.isDead) continue;

                byte currentStamina = life.stamina;

                if (previousStamina.TryGetValue(player, out byte prevStamina))
                {
                    if (currentStamina < prevStamina)
                    {
                        lastTireTime[player] = now;
                    }
                    else if (currentStamina > prevStamina)
                    {
                        // Vanilla passive regen ticked this frame; revert it so only the delayed
                        // burst regen below can restore stamina.
                        // serverModifyStamina replicates the delta to the client (askTire does not).
                        life.serverModifyStamina(-(currentStamina - prevStamina));
                        currentStamina = life.stamina;
                    }
                }

                if (duration <= 0 || currentStamina >= 100)
                {
                    pendingRegen.Remove(player);
                    previousStamina[player] = currentStamina;
                    continue;
                }

                if (!lastTireTime.TryGetValue(player, out float tireTime) || now - tireTime < delay)
                {
                    previousStamina[player] = currentStamina;
                    continue;
                }

                // Cardio mastery (maxed out by the "Estamina UP" powerup) speeds up the regen burst,
                // the same role it plays in the vanilla formula this system replaces
                float masteryValue = player.Player!.skills.mastery((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.CARDIO);
                float staminaPerSecond = (100f / duration) * (1f + masteryValue);

                pendingRegen.TryGetValue(player, out float pending);
                pending += staminaPerSecond * UnityEngineCoreModule.UnityEngine.Time.deltaTime;

                byte wholeAmount = (byte)System.Math.Min(byte.MaxValue, System.Math.Floor(pending));
                if (wholeAmount > 0)
                {
                    life.serverModifyStamina(wholeAmount);
                    pending -= wholeAmount;
                }

                pendingRegen[player] = pending;
                previousStamina[player] = life.stamina;
            }
        }
    }
}

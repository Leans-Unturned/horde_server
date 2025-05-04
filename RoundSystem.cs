extern alias UnityEngineCoreModule;

using System.Threading.Tasks;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace HordeServer;
class RoundSystem(uint tickrateBetweenRounds, uint spawnTickrate)
{
    public bool RestartRound = false;
    public int wave = -1;

    private uint actualSpawnTick = spawnTickrate;
    private uint actualRemainingTick = 0;
    private uint actualTickBetweenRounds = tickrateBetweenRounds;

    private bool shouldAlertRoundEnded = false;

    public void Update()
    {
        // Round Restart
        if (RestartRound)
        {
            bool oneIsAlive = false;
            foreach (UnturnedPlayer player in HordeServerPlugin.onlinePlayers)
            {
                if (!player.Dead)
                {
                    oneIsAlive = true;
                    break;
                }
                ;
            }

            if (oneIsAlive)
            {
                actualSpawnTick = HordeServerPlugin.instance!.Configuration.Instance.SpawnTickrate;
                actualTickBetweenRounds = HordeServerPlugin.instance!.Configuration.Instance.TickrateBetweenRounds;
                actualRemainingTick = HordeServerPlugin.instance!.Configuration.Instance.RemainingCheckTickrate;

                HordeUtils.zombiesAlive = [];
                HordeUtils.zombiesToSpawn = 0;
                HordeUtils.wave = null;
                LightingManager.time = 500;
                wave = -1;

                RestartRound = false;
                HordeUtils.KillAllZombies();
                ItemManager.askClearAllItems();

                HordeServerPlugin.alivePlayers = [];
                foreach (UnturnedPlayer player in HordeServerPlugin.onlinePlayers)
                {
                    ChatManager.serverSendMessage(
                        HordeServerPlugin.instance!.Translate("round_started"),
                        new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                        null,
                        player.SteamPlayer(),
                        EChatMode.SAY,
                        HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                        true
                    );

                    PowerupSystem.ResetPlayersPowerups();
                    SkillSystem.ResetPlayersSkills();
                    SkillSystem.RefreshPlayersSkills();
                    player.Experience = HordeServerPlugin.instance.Configuration.Instance.StartingCredits;

                    if (!player.Dead)
                    {
                        ConfigPosition position = HordeServerPlugin.instance.Configuration.Instance.PlayerSpawnPositions[
                            Random.Range(0, HordeServerPlugin.instance.Configuration.Instance.PlayerSpawnPositions.Count)];

                        player.Teleport(new(position.X, position.Y, position.Z), position.Angle);
                        HordeServerPlugin.alivePlayers.Add(player);
                    }
                }
            }
            else
            {
                if (HordeServerPlugin.onlinePlayers.Count == 0)
                {
                    Logger.LogWarning("No online players, round restart was cancelled");
                    RestartRound = false;
                }
                else
                {
                    Logger.LogWarning($"No alive players, round restart was cancelled, will retry in {HordeServerPlugin.instance!.Configuration.Instance.SecondsAfterRoundFail} seconds...");
                    RestartRound = false;

                    Task.Delay(HordeServerPlugin.instance!.Configuration.Instance.SecondsAfterRoundFail * 1000).ContinueWith((_) =>
                    {
                        if (HordeServerPlugin.alivePlayers.Count > 0)
                        {
                            Logger.LogWarning($"Restart round was cancelled because there is alive players");
                            return;
                        }
                        Logger.LogWarning($"Restarting round after no alive players detected...");
                        RestartRound = true;
                    });
                }
            }
        }

        // Round Start
        if (HordeServerPlugin.alivePlayers.Count > 0)
        {
            if (HordeUtils.zombiesToSpawn <= 0 && HordeUtils.zombiesAlive.Count <= 0 && actualTickBetweenRounds == 0)
            {
                actualTickBetweenRounds = HordeServerPlugin.instance!.Configuration.Instance.TickrateBetweenRounds;
                HordeUtils.AlertsUsed = [];

                wave++;

                // Game Ended all waves has been reach
                if (HordeServerPlugin.instance!.Configuration.Instance.Waves.Count <= wave)
                {
                    HordeUtils.wave = HordeServerPlugin.instance!.Configuration.Instance.Waves[wave].Clone();
                    foreach (UnturnedPlayer player in HordeServerPlugin.onlinePlayers)
                    {
                        ChatManager.serverSendMessage(
                            HordeServerPlugin.instance!.Translate("round_finish"),
                            new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                            null,
                            player.SteamPlayer(),
                            EChatMode.SAY,
                            HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                            true
                        );
                    }

                    RestartRound = true;
                    return;
                }

                HordeUtils.wave = HordeServerPlugin.instance!.Configuration.Instance.Waves[wave].Clone();
                HordeUtils.CalculateZombiesToSpawn();

                foreach (UnturnedPlayer player in HordeServerPlugin.onlinePlayers)
                {
                    ChatManager.serverSendMessage(
                        HordeServerPlugin.instance!.Translate("wave_started", wave + 1, HordeUtils.zombiesToSpawn),
                        new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                        null,
                        player.SteamPlayer(),
                        EChatMode.SAY,
                        HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                        true
                    );

                    // Teleport dead players to the area again
                    if (!HordeServerPlugin.alivePlayers.Contains(player))
                    {
                        ConfigPosition position = HordeServerPlugin.instance.Configuration.Instance.PlayerSpawnPositions[
                                Random.Range(0, HordeServerPlugin.instance.Configuration.Instance.PlayerSpawnPositions.Count)];

                        player.Teleport(new(position.X, position.Y, position.Z), position.Angle);
                        HordeServerPlugin.alivePlayers.Add(player);
                    }
                }

                shouldAlertRoundEnded = true;
            }
            else if (HordeUtils.zombiesToSpawn <= 0 && HordeUtils.zombiesAlive.Count <= 0)
            {
                if (shouldAlertRoundEnded)
                {
                    foreach (UnturnedPlayer player in HordeServerPlugin.onlinePlayers)
                    {
                        ChatManager.serverSendMessage(
                            HordeServerPlugin.instance!.Translate("round_end"),
                            new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                            null,
                            player.SteamPlayer(),
                            EChatMode.SAY,
                            HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                            true
                        );
                    }
                    shouldAlertRoundEnded = false;
                }
                actualTickBetweenRounds--;
            };
        }

        // Spawn Tickrate
        if (actualSpawnTick <= 0)
        {
            actualSpawnTick = HordeServerPlugin.instance!.Configuration.Instance.SpawnTickrate;

            HordeUtils.SpawnZombiesInNodes();
        }
        actualSpawnTick--;

        // Zombie check tick
        if (actualRemainingTick <= 0)
        {
            actualRemainingTick = HordeServerPlugin.instance!.Configuration.Instance.RemainingCheckTickrate;

            HordeUtils.CheckRemainingZombies();
        }
        actualRemainingTick--;
    }
}

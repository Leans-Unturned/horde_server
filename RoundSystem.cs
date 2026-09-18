extern alias UnityEngineCoreModule;

using System.Threading.Tasks;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Random = UnityEngineCoreModule.UnityEngine.Random;

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
                shouldAlertRoundEnded = false;

                actualSpawnTick = HordeServerPlugin.instance!.Configuration.Instance.SpawnTickrate;
                actualTickBetweenRounds = HordeServerPlugin.instance!.Configuration.Instance.TickrateBetweenRounds;
                actualRemainingTick = HordeServerPlugin.instance!.Configuration.Instance.RemainingCheckTickrate;

                HordeUtils.zombiesAlive = [];
                HordeUtils.zombieSpeedMultipliers.Clear();
                HordeUtils.zombiesToSpawn = 0;
                HordeUtils.wave = null;
                LightingManager.time = 500;
                wave = -1;

                DoorSystem.RespawnDoors();

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
                    ItemSystem.RefreshPrimaryLoadout(player);
                    ItemSystem.RefreshSecondaryLoadout(player);
                    player.Experience = HordeServerPlugin.instance.Configuration.Instance.StartingCredits;

                    if (!player.Dead)
                    {
                        var playerSpawnPositions = HordeUtils.GetPlayerSpawnNodePositions();
                        if (playerSpawnPositions.Count > 0)
                        {
                            var spawn = playerSpawnPositions[Random.Range(0, playerSpawnPositions.Count)];
                            player.Teleport(spawn.position, spawn.angle);
                            HordeServerPlugin.alivePlayers.Add(player);
                        }
                        else
                            Logger.LogError("Cannot find any location node named \"playerspawn\" in the map, add some in the map editor");
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
                    HordeUtils.ScaleWaveForPlayerCount(HordeUtils.wave, HordeServerPlugin.onlinePlayers.Count);
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
                HordeUtils.ScaleWaveForPlayerCount(HordeUtils.wave, HordeServerPlugin.onlinePlayers.Count);
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
                    if (!HordeServerPlugin.alivePlayers.Contains(player) && !player.Dead)
                    {
                        ItemSystem.RefreshPrimaryLoadout(player);
                        ItemSystem.RefreshSecondaryLoadout(player);

                        var playerSpawnPositions = HordeUtils.GetPlayerSpawnNodePositions();
                        if (playerSpawnPositions.Count > 0)
                        {
                            var spawn = playerSpawnPositions[Random.Range(0, playerSpawnPositions.Count)];
                            player.Teleport(spawn.position, spawn.angle);
                            HordeServerPlugin.alivePlayers.Add(player);
                        }
                        else
                            Logger.LogError("Cannot find any location node named \"playerspawn\" in the map, add some in the map editor");
                    }

                    PowerupSystem.GiveRoundGrenadeForPlayer(player);
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
            }
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

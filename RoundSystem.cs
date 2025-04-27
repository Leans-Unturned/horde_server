extern alias UnityEngineCoreModule;

using System.Threading.Tasks;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace HordeServer;
class RoundSystem(uint tickrateBetweenRounds, uint spawnTickrate)
{
    public bool RestartRound = false;
    public int wave = 0;

    private uint actualSpawnTick = spawnTickrate;
    private uint actualRemainingTick = 0;
    private uint actualTickBetweenRounds = tickrateBetweenRounds;

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
                };
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
                wave = 0;
                System.Random random = new();

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

                    player.Experience = HordeServerPlugin.instance.Configuration.Instance.StartingCredits;

                    if (!player.Dead)
                    {
                        ConfigPosition position = HordeServerPlugin.instance.Configuration.Instance.PlayerSpawnPositions[
                            random.Next(HordeServerPlugin.instance.Configuration.Instance.PlayerSpawnPositions.Count)];

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
        if (HordeUtils.zombiesToSpawn <= 0 && HordeUtils.zombiesAlive.Count <= 0 && actualTickBetweenRounds == 0)
        {
            actualTickBetweenRounds = HordeServerPlugin.instance!.Configuration.Instance.TickrateBetweenRounds;
            HordeUtils.AlertsUsed = [];

            wave++;

            // Game Ended all waves has been reach
            if (HordeServerPlugin.instance!.Configuration.Instance.Waves.Count <= wave)
            {
                HordeUtils.wave = HordeServerPlugin.instance!.Configuration.Instance.Waves[wave];
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

                RestartRound = true;
                return;
            }

            HordeUtils.wave = HordeServerPlugin.instance!.Configuration.Instance.Waves[wave];
            HordeUtils.CalculateZombiesToSpawn();

            System.Random random = new();
            foreach (UnturnedPlayer player in HordeServerPlugin.onlinePlayers)
            {
                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("wave_started", wave, HordeUtils.zombiesToSpawn),
                    new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                    null,
                    player.SteamPlayer(),
                    EChatMode.SAY,
                    HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                    true
                );

                if (!HordeServerPlugin.alivePlayers.Contains(player))
                {
                    ConfigPosition position = HordeServerPlugin.instance.Configuration.Instance.PlayerSpawnPositions[
                            random.Next(HordeServerPlugin.instance.Configuration.Instance.PlayerSpawnPositions.Count)];

                    player.Teleport(new(position.X, position.Y, position.Z), position.Angle);
                    HordeServerPlugin.alivePlayers.Add(player);
                }
            }
        }
        else if (HordeUtils.zombiesToSpawn <= 0 && HordeUtils.zombiesAlive.Count <= 0) actualTickBetweenRounds--;

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

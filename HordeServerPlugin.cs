extern alias UnityEngineCoreModule;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace HordeServer
{
    public class HordeServerPlugin : RocketPlugin<HordeServerConfiguration>
    {
        public static List<UnturnedPlayer> onlinePlayers = [];
        public static List<UnturnedPlayer> alivePlayers = [];

        public static HordeServerPlugin? instance;

        private HordeSpawnTickrate? hordeInstance;
        public override void LoadPlugin()
        {
            base.LoadPlugin();
            instance = this;

            hordeInstance = gameObject.AddComponent<HordeSpawnTickrate>();

            Rocket.Unturned.U.Events.OnPlayerConnected += OnPlayerConnected;
            Rocket.Unturned.U.Events.OnPlayerDisconnected += OnPlayerDisconnected;

            string[] playersDirectory = System.IO.Directory.GetDirectories(Configuration.Instance.PlayersFolder);
            foreach (string playerFolder in playersDirectory)
            {
                string clothingPath = Path.Combine(Configuration.Instance.PlayersFolder, playerFolder, Configuration.Instance.LevelName, "Player", "Clothing.dat");
                if (File.Exists(clothingPath)) File.Delete(clothingPath);

                string inventoryPath = Path.Combine(Configuration.Instance.PlayersFolder, playerFolder, Configuration.Instance.LevelName, "Player", "Inventory.dat");
                if (File.Exists(inventoryPath)) File.Delete(inventoryPath);

                string position = Path.Combine(Configuration.Instance.PlayersFolder, playerFolder, Configuration.Instance.LevelName, "Player", "Player.dat");
                if (File.Exists(position)) File.Delete(position);

                string life = Path.Combine(Configuration.Instance.PlayersFolder, playerFolder, Configuration.Instance.LevelName, "Player", "Life.dat");
                if (File.Exists(life)) File.Delete(life);

                string skills = Path.Combine(Configuration.Instance.PlayersFolder, playerFolder, Configuration.Instance.LevelName, "Player", "Skills.dat");
                if (File.Exists(skills)) File.Delete(skills);
            }

            Logger.Log("HordeServer instanciated, by LeandroTheDev");
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            player.Events.OnDeath -= OnPlayerDead;

            onlinePlayers.Remove(player);
            alivePlayers.Remove(player);

            UnturnedPlayerEvents.OnPlayerUpdateStat += OnPlayerStatsUpdate;

            string clothingPath = Path.Combine(Configuration.Instance.PlayersFolder, $"{player.Id}_0", Configuration.Instance.LevelName, "Player", "Clothing.dat");
            if (File.Exists(clothingPath)) File.Delete(clothingPath);

            string inventoryPath = Path.Combine(Configuration.Instance.PlayersFolder, $"{player.Id}_0", Configuration.Instance.LevelName, "Player", "Inventory.dat");
            if (File.Exists(inventoryPath)) File.Delete(inventoryPath);

            string position = Path.Combine(Configuration.Instance.PlayersFolder, $"{player.Id}_0", Configuration.Instance.LevelName, "Player", "Player.dat");
            if (File.Exists(position)) File.Delete(position);

            string life = Path.Combine(Configuration.Instance.PlayersFolder, $"{player.Id}_0", Configuration.Instance.LevelName, "Player", "Life.dat");
            if (File.Exists(life)) File.Delete(life);

            string skills = Path.Combine(Configuration.Instance.PlayersFolder, $"{player.Id}_0", Configuration.Instance.LevelName, "Player", "Skills.dat");
            if (File.Exists(skills)) File.Delete(skills);
        }

        private void OnPlayerStatsUpdate(UnturnedPlayer player, EPlayerStat stat)
        {
            switch (stat)
            {
                case EPlayerStat.KILLS_ZOMBIES_NORMAL:
                    HordeUtils.ReceiveZombieDeathUpdate();
                    break;
                case EPlayerStat.KILLS_ZOMBIES_MEGA:
                    HordeUtils.ReceiveZombieDeathUpdate();
                    break;
            }
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            player.Events.OnDeath += OnPlayerDead;

            if (onlinePlayers.Count == 0)
            {
                Task.Delay(5000).ContinueWith((_) =>
                {
                    hordeInstance!.RestartRound = true;
                });
            }

            if (Configuration.Instance.DebugPlayerPosition)
            {
                var debugTimer = new System.Timers.Timer(1000);

                debugTimer.Elapsed += (sender, e) =>
                {
                    if (player.Position != null)
                        Logger.Log($"[{player.SteamName}]: {player.Position.x},{player.Position.y},{player.Position.z}");
                    else debugTimer.Dispose();
                };

                debugTimer.AutoReset = true;
                debugTimer.Enabled = true;
            }

            onlinePlayers.Add(player);
        }

        private void OnPlayerDead(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            alivePlayers.Remove(player);

            if (alivePlayers.Count <= 0)
            {
                foreach (UnturnedPlayer chatPlayer in onlinePlayers)
                {
                    ChatManager.serverSendMessage(
                        Translate("round_fail"),
                        new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                        null,
                        chatPlayer.SteamPlayer(),
                        EChatMode.SAY,
                        Configuration.Instance.ChatIconURL,
                        true
                    );

                    Task.Delay(Configuration.Instance.SecondsAfterRoundFail * 1000).ContinueWith((_) => hordeInstance!.RestartRound = true);
                }
            }
        }

        public override TranslationList DefaultTranslations => new()
        {
            {"round_started", "Horde Starting soon..."},
            {"round_end", "Congratulations, you survived the Horde!!!"},
            {"round_fail", "All survivors died, restarting round..."},
            {"wave_started", "Wave {0}, Zombies: {1}"},
            {"wave_remaining", "Remaining Zombies: {0}" }
        };
    }

    class HordeSpawnTickrate : MonoBehaviour
    {
        public bool RestartRound = false;
        public int wave = 0;

        private uint actualSpawnTick = 0;
        private uint actualRemainingTick = 0;
        private uint actualTickBetweenRounds = 0;

        public void Start()
        {
            actualTickBetweenRounds = HordeServerPlugin.instance!.Configuration.Instance.TickrateBetweenRounds;
            actualSpawnTick = HordeServerPlugin.instance!.Configuration.Instance.SpawnTickrate;
        }

        public void Update()
        {
            // Round Restart
            if (RestartRound)
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

                    if (!player.Dead)
                    {
                        ConfigPosition position = HordeServerPlugin.instance.Configuration.Instance.PlayerSpawnPositions[
                            random.Next(HordeServerPlugin.instance.Configuration.Instance.PlayerSpawnPositions.Count)];

                        player.Teleport(new(position.X, position.Y, position.Z), position.Angle);
                        HordeServerPlugin.alivePlayers.Add(player);
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

    class HordeUtils
    {
        private static readonly System.Random random = new();
        public static List<uint> AlertsUsed = [];
        public static long zombiesToSpawn = 0;
        public static List<Zombie> zombiesAlive = [];
        public static ConfigWave? wave = null;

        public static void SpawnZombiesInNodes()
        {
            if (HordeServerPlugin.alivePlayers.Count == 0
            || zombiesToSpawn <= 0
            || wave == null
            || HordeServerPlugin.instance == null) return;

            // Getting all zombie instances from the map
            List<Zombie> zombieNodes = [];
            ZombieManager.getZombiesInRadius(new(
                HordeServerPlugin.instance.Configuration.Instance.MapCenterPosition.X,
                HordeServerPlugin.instance.Configuration.Instance.MapCenterPosition.Y,
                HordeServerPlugin.instance.Configuration.Instance.MapCenterPosition.Z),
                HordeServerPlugin.instance.Configuration.Instance.MapCenterRadius, zombieNodes);

            List<ZombieNodePosition> zombiesNodesToSpawn = new(
                HordeServerPlugin.instance.Configuration.Instance.ZombiesAvailableNodes
            );
            foreach (ZombieNodePosition _ in HordeServerPlugin.instance.Configuration.Instance.ZombiesAvailableNodes)
            {
                // Randomly get a zombie node to spawn
                ZombieNodePosition zombieNodePosition;
                if (zombiesNodesToSpawn.Count > 0)
                {
                    int index = random.Next(zombiesNodesToSpawn.Count);
                    zombieNodePosition = zombiesNodesToSpawn[index];
                    zombiesNodesToSpawn.RemoveAt(index);
                }
                else return;

                // Create the zombie instance
                EZombieSpeciality speciality = GetRandomZombieFromWave();
                byte type = 1;
                byte shirt = 1;
                byte pants = 1;
                byte hat = 1;
                byte gear = 1;
                UnityEngineCoreModule.UnityEngine.Vector3 point = new(zombieNodePosition.X, zombieNodePosition.Y, zombieNodePosition.Z);

                bool zombieSpawned = false;

                // Try to spawn a zombie with one of the available zombie nodes
                foreach (Zombie zombie in zombieNodes)
                {
                    if (zombiesToSpawn <= 0) return;

                    if (zombie.isDead)
                    {
                        zombie.sendRevive(type, (byte)speciality, shirt, pants, hat, gear, point, Random.Range(0f, 360f));
                        zombiesAlive.Add(zombie);
                        zombiesToSpawn--;
                        if (zombiesToSpawn <= 0) return;
                        zombieSpawned = true;
                        break;
                    }
                }

                // All zombies is alive
                if (!zombieSpawned) break;
            }
        }

        public static void KillAllZombies()
        {
            List<Zombie> zombiesAlive = UnityEngineCoreModule.UnityEngine.Object.FindObjectsOfType<Zombie>()?.ToList() ?? [];
            foreach (Zombie zombie in zombiesAlive)
            {
                ZombieManager.sendZombieDead(zombie, UnityEngineCoreModule.UnityEngine.Vector3.zero);
            }
        }

        public static void CalculateZombiesToSpawn()
        {
            if (wave == null)
            {
                Logger.LogError("CalculateZombiesToSpawn called without any ConfigWave");
                return;
            }

            zombiesToSpawn = 0;
            zombiesToSpawn += wave.Acid;
            zombiesToSpawn += wave.BossEletric;
            zombiesToSpawn += wave.BossElverStomper;
            zombiesToSpawn += wave.BossFire;
            zombiesToSpawn += wave.BossMagma;
            zombiesToSpawn += wave.BossNuclear;
            zombiesToSpawn += wave.BossSprit;
            zombiesToSpawn += wave.BossWind;
            zombiesToSpawn += wave.Burner;
            zombiesToSpawn += wave.Crawler;
            zombiesToSpawn += wave.DLBlueVolatile;
            zombiesToSpawn += wave.DLRedVolatile;
            zombiesToSpawn += wave.FlankerFriendly;
            zombiesToSpawn += wave.FlankerStalk;
            zombiesToSpawn += wave.Mega;
            zombiesToSpawn += wave.Normal;
            zombiesToSpawn += wave.Spirit;
            zombiesToSpawn += wave.Sprinter;
        }

        public static void ReceiveZombieDeathUpdate()
        {
            if (zombiesToSpawn - 1 <= 0)
            {
                uint? nextAlert = HordeServerPlugin.instance!.Configuration.Instance.RemainingZombiesAlert
                    .Where(n => n >= zombiesAlive.Count)
                    .OrderBy(n => n)
                    .FirstOrDefault();

                if (nextAlert != null && !AlertsUsed.Contains((uint)nextAlert))
                {
                    AlertsUsed.Add((uint)nextAlert);

                    foreach (UnturnedPlayer player in HordeServerPlugin.onlinePlayers)
                    {
                        ChatManager.serverSendMessage(
                            HordeServerPlugin.instance!.Translate("wave_remaining", zombiesAlive.Count),
                            new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                            null,
                            player.SteamPlayer(),
                            EChatMode.SAY,
                            HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                            true
                        );
                    }
                }
            }
        }

        public static void CheckRemainingZombies()
        {
            List<Zombie> toRemove = [];

            foreach (Zombie zombie in zombiesAlive)
            {
                if (zombie.isDead)
                    toRemove.Add(zombie);
            }

            if (zombiesToSpawn <= 0 && toRemove.Count > 0)
            {
                uint? nextAlert = HordeServerPlugin.instance!.Configuration.Instance.RemainingZombiesAlert
                    .Where(n => n >= zombiesAlive.Count)
                    .OrderBy(n => n)
                    .FirstOrDefault();

                if (nextAlert != null && !AlertsUsed.Contains((uint)nextAlert))
                {
                    AlertsUsed.Add((uint)nextAlert);

                    foreach (UnturnedPlayer player in HordeServerPlugin.onlinePlayers)
                    {
                        ChatManager.serverSendMessage(
                            HordeServerPlugin.instance!.Translate("wave_remaining", zombiesAlive.Count),
                            new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                            null,
                            player.SteamPlayer(),
                            EChatMode.SAY,
                            HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                            true
                        );
                    }
                }
            }

            foreach (Zombie zombie in toRemove)
            {
                zombiesAlive.Remove(zombie);
            }
        }

        private static EZombieSpeciality GetRandomZombieFromWave()
        {
            if (wave == null)
            {
                Logger.LogError("GetRandomZombieFromWave called without any ConfigWave");
                return EZombieSpeciality.NONE;
            }

            List<string> zombies = [];
            if (wave.Acid > 0) zombies.Add("Acid");
            if (wave.BossEletric > 0) zombies.Add("BossEletric");
            if (wave.BossElverStomper > 0) zombies.Add("BossElverStomper");
            if (wave.BossFire > 0) zombies.Add("BossFire");
            if (wave.BossMagma > 0) zombies.Add("BossMagma");
            if (wave.BossNuclear > 0) zombies.Add("BossNuclear");
            if (wave.BossSprit > 0) zombies.Add("BossSprit");
            if (wave.BossWind > 0) zombies.Add("BossWind");
            if (wave.Burner > 0) zombies.Add("Burner");
            if (wave.Crawler > 0) zombies.Add("Crawler");
            if (wave.DLBlueVolatile > 0) zombies.Add("DLBlueVolatile");
            if (wave.DLRedVolatile > 0) zombies.Add("DLRedVolatile");
            if (wave.FlankerFriendly > 0) zombies.Add("FlankerFriendly");
            if (wave.FlankerStalk > 0) zombies.Add("FlankerStalk");
            if (wave.Mega > 0) zombies.Add("Mega");
            if (wave.Normal > 0) zombies.Add("Normal");
            if (wave.Spirit > 0) zombies.Add("Spirit");
            if (wave.Sprinter > 0) zombies.Add("Sprinter");

            zombiesToSpawn--;

            if (zombies.Count > 0)
            {
                int index = random.Next(zombies.Count);
                var zombieType = zombies[index];

                switch (zombieType)
                {
                    case "Acid":
                        wave.Acid--;
                        return EZombieSpeciality.ACID;
                    case "BossEletric":
                        wave.BossEletric--;
                        return EZombieSpeciality.BOSS_ELECTRIC;
                    case "BossElverStomper":
                        wave.BossElverStomper--;
                        return EZombieSpeciality.BOSS_ELVER_STOMPER;
                    case "BossFire":
                        wave.BossFire--;
                        return EZombieSpeciality.BOSS_FIRE;
                    case "BossMagma":
                        wave.BossMagma--;
                        return EZombieSpeciality.BOSS_MAGMA;
                    case "BossNuclear":
                        wave.BossNuclear--;
                        return EZombieSpeciality.BOSS_NUCLEAR;
                    case "BossSprit":
                        wave.BossSprit--;
                        return EZombieSpeciality.BOSS_SPIRIT;
                    case "BossWind":
                        wave.BossWind--;
                        return EZombieSpeciality.BOSS_WIND;
                    case "Burner":
                        wave.Burner--;
                        return EZombieSpeciality.BURNER;
                    case "Crawler":
                        wave.Crawler--;
                        return EZombieSpeciality.CRAWLER;
                    case "DLBlueVolatile":
                        wave.DLBlueVolatile--;
                        return EZombieSpeciality.DL_BLUE_VOLATILE;
                    case "DLRedVolatile":
                        wave.DLRedVolatile--;
                        return EZombieSpeciality.DL_RED_VOLATILE;
                    case "FlankerFriendly":
                        wave.FlankerFriendly--;
                        return EZombieSpeciality.FLANKER_FRIENDLY;
                    case "FlankerStalk":
                        wave.FlankerStalk--;
                        return EZombieSpeciality.FLANKER_STALK;
                    case "Mega":
                        wave.Mega--;
                        return EZombieSpeciality.MEGA;
                    case "Normal":
                        wave.Normal--;
                        return EZombieSpeciality.NORMAL;
                    case "Spirit":
                        wave.Spirit--;
                        return EZombieSpeciality.SPIRIT;
                    case "Sprinter":
                        wave.Sprinter--;
                        return EZombieSpeciality.SPRINTER;
                }
            }
            return EZombieSpeciality.NONE;
        }
    }
}
extern alias UnityEngineCoreModule;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Enumerations;
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

        private UnityTickrate? unityTickrate;
        public override void LoadPlugin()
        {
            base.LoadPlugin();
            instance = this;

            unityTickrate = gameObject.AddComponent<UnityTickrate>();

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

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            player.Events.OnDeath += OnPlayerDead;
            player.Events.OnInventoryAdded += ItemSystem.OnInventoryAdded;
            player.Events.OnInventoryRemoved += ItemSystem.OnInventoryRemoved;
            UnturnedPlayerEvents.OnPlayerUpdateStat += OnPlayerStatsUpdate;

            if (onlinePlayers.Count == 0)
            {
                Task.Delay(Configuration.Instance.SecondsAfterRoundFail * 1000).ContinueWith((_) =>
                {
                    unityTickrate!.RoundSystemInstance!.RestartRound = true;
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

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            player.Events.OnDeath -= OnPlayerDead;
            player.Events.OnInventoryAdded -= ItemSystem.OnInventoryAdded;
            player.Events.OnInventoryRemoved -= ItemSystem.OnInventoryRemoved;
            UnturnedPlayerEvents.OnPlayerUpdateStat -= OnPlayerStatsUpdate;

            onlinePlayers.Remove(player);
            alivePlayers.Remove(player);

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
                }

                void tryToRestartRound()
                {
                    if (onlinePlayers.Count == 0) return;

                    Task.Delay(Configuration.Instance.SecondsAfterRoundFail * 1000).ContinueWith((_) =>
                    {
                        if (onlinePlayers.Count == 0) return;

                        bool oneIsAlive = false;
                        foreach (UnturnedPlayer player in onlinePlayers)
                        {
                            if (!player.Dead)
                            {
                                oneIsAlive = true;
                                break;
                            };
                        }

                        if (oneIsAlive)
                            unityTickrate!.RoundSystemInstance!.RestartRound = true;
                        else tryToRestartRound();
                    });
                }

                tryToRestartRound();
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

    class UnityTickrate : MonoBehaviour
    {
        public RoundSystem? RoundSystemInstance;

        public void Start()
        {
            RoundSystemInstance = new(HordeServerPlugin.instance!.Configuration.Instance.TickrateBetweenRounds,
                HordeServerPlugin.instance!.Configuration.Instance.SpawnTickrate);
        }

        public void Update()
        {
            RoundSystemInstance?.Update();
            ItemSystem.Update();
        }
    }
}
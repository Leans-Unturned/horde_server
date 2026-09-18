extern alias UnityEngineCoreModule;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

        // Matches the exact path the engine itself saves player data to (ServerSavedata.cs /
        // PlayerSavedata.cs), derived from the running server's own identity instead of requiring
        // the admin to duplicate/retype it in our config
        public static string PlayersFolder => Path.Combine(ReadWrite.PATH, "Servers", Provider.serverID, "Players");

        private UnityTickrate? unityTickrate;
        public override void LoadPlugin()
        {
            base.LoadPlugin();
            instance = this;

            unityTickrate = gameObject.AddComponent<UnityTickrate>();

            if (Configuration.Instance.ForceRemoveZombieRadiation)
                HordeUtils.RemoveZombiesRadiation();
            if (Configuration.Instance.ForceRemovePlayerRadiation)
                PlayerLife.OnTellVirus_Global += HordeUtils.ForcePlayerFullImmunity;

            // Vanilla passive health regen (PlayerLife, +1hp tick when food/water > 90) would add
            // stray HP between zombie hits, breaking the deterministic hits-to-kill math and fighting
            // the delay/duration timing of HealthRegenSystem. Disable it, HealthRegenSystem is now
            // the only thing allowed to heal players passively.
            Provider.modeConfigData.Players.Health_Regen_Ticks = uint.MaxValue;

            // Horde mode balancing baked into the plugin instead of relying on every server's
            // Configs.json being edited by hand: max zombie spawn/respawn, vanilla damage/armor
            // multipliers untouched (horde difficulty is scaled elsewhere), food/water never deplete.
            Provider.modeConfigData.Zombies.Spawn_Chance = 100.0f;
            Provider.modeConfigData.Zombies.Respawn_Day_Time = 999999999.0f;
            Provider.modeConfigData.Zombies.Respawn_Night_Time = 999999999.0f;
            Provider.modeConfigData.Zombies.Damage_Multiplier = 1.0f;
            Provider.modeConfigData.Zombies.Armor_Multiplier = 1.0f;
            Provider.modeConfigData.Players.Food_Use_Ticks = uint.MaxValue;
            Provider.modeConfigData.Players.Water_Use_Ticks = uint.MaxValue;

            Rocket.Unturned.U.Events.OnPlayerConnected += OnPlayerConnected;
            Rocket.Unturned.U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            DamageTool.damageZombieRequested += HordeUtils.CalculateZombieArmor;
            DamageTool.damageZombieRequested += HordeUtils.HitPoints;
            DamageTool.damagePlayerRequested += HordeUtils.CalculatePlayerLifeFromZombieHit;
            UseableGun.onChangeSightRequested += HordeUtils.BlockPackAPunchAttachmentChange;
            UseableGun.onChangeTacticalRequested += HordeUtils.BlockPackAPunchAttachmentChange;
            UseableGun.onChangeGripRequested += HordeUtils.BlockPackAPunchAttachmentChange;
            UseableGun.onChangeBarrelRequested += HordeUtils.BlockPackAPunchAttachmentChange;
            UseableGun.onChangeMagazineRequested += HordeUtils.BlockPackAPunchAttachmentChange;
            UnturnedPlayerEvents.OnPlayerUpdateStat += OnPlayerStatsUpdate;
            PlayerSkills.OnExperienceChanged_Global += ItemSystem.OnPlayerExperienceChanged;

            try
            {
                // Nothing to clean up yet (fresh server, no player has ever joined), and Level.info
                // is not guaranteed to be populated this early in plugin load, avoid relying on
                // GetDirectories/Level.info throwing to detect either case
                if (System.IO.Directory.Exists(PlayersFolder) && Level.info != null)
                {
                    string[] playersDirectory = System.IO.Directory.GetDirectories(PlayersFolder);
                    foreach (string playerFolder in playersDirectory)
                    {
                        string clothingPath = Path.Combine(PlayersFolder, playerFolder, Level.info.name, "Player", "Clothing.dat");
                        if (File.Exists(clothingPath)) File.Delete(clothingPath);

                        string inventoryPath = Path.Combine(PlayersFolder, playerFolder, Level.info.name, "Player", "Inventory.dat");
                        if (File.Exists(inventoryPath)) File.Delete(inventoryPath);

                        string position = Path.Combine(PlayersFolder, playerFolder, Level.info.name, "Player", "Player.dat");
                        if (File.Exists(position)) File.Delete(position);

                        string life = Path.Combine(PlayersFolder, playerFolder, Level.info.name, "Player", "Life.dat");
                        if (File.Exists(life)) File.Delete(life);

                        string skills = Path.Combine(PlayersFolder, playerFolder, Level.info.name, "Player", "Skills.dat");
                        if (File.Exists(skills)) File.Delete(skills);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Invalid players directory ({PlayersFolder}), exception: {ex.Message}");
            }

            BarricadeDrop.OnSalvageRequested_Global += DoorSystem.TryOpenDoor;
            BarricadeManager.onBarricadeSpawned += DoorSystem.LogDebugDoorPlacement;

            if (Configuration.Instance.KitCommandOnlyInArea)
                UnturnedPlayerEvents.OnPlayerUpdatePosition += KitSystem.PositionUpdate;

            Logger.Log("HordeServer instanciated, by LeandroTheDev");
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            player.Events.OnDeath += OnPlayerDead;
            player.Events.OnRevive += OnPlayerRevive;
            player.Events.OnInventoryAdded += ItemSystem.OnInventoryAdded;
            player.Events.OnInventoryRemoved += ItemSystem.OnInventoryRemoved;
            player.Inventory.onDropItemRequested += ItemSystem.OnItemDropped;
            player.Player.skills.onSkillsUpdated += OnSkillsUpdated;

            KitSystem.GiveDefaultKit(player);

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
            PowerupSystem.Disconnect(player);
            SkillSystem.Disconnect(player);
            HealthRegenSystem.Disconnect(player);
            KitSystem.Disconnect(player.Id);
            player.Events.OnDeath -= OnPlayerDead;
            player.Events.OnRevive -= OnPlayerRevive;
            player.Events.OnInventoryAdded -= ItemSystem.OnInventoryAdded;
            player.Events.OnInventoryRemoved -= ItemSystem.OnInventoryRemoved;
            player.Inventory.onDropItemRequested -= ItemSystem.OnItemDropped;
            player.Player.skills.onSkillsUpdated -= OnSkillsUpdated;

            onlinePlayers.Remove(player);
            alivePlayers.Remove(player);

            string clothingPath = Path.Combine(PlayersFolder, $"{player.Id}_0", Level.info.name, "Player", "Clothing.dat");
            if (File.Exists(clothingPath)) File.Delete(clothingPath);

            string inventoryPath = Path.Combine(PlayersFolder, $"{player.Id}_0", Level.info.name, "Player", "Inventory.dat");
            if (File.Exists(inventoryPath)) File.Delete(inventoryPath);

            string position = Path.Combine(PlayersFolder, $"{player.Id}_0", Level.info.name, "Player", "Player.dat");
            if (File.Exists(position)) File.Delete(position);

            string life = Path.Combine(PlayersFolder, $"{player.Id}_0", Level.info.name, "Player", "Life.dat");
            if (File.Exists(life)) File.Delete(life);

            string skills = Path.Combine(PlayersFolder, $"{player.Id}_0", Level.info.name, "Player", "Skills.dat");
            if (File.Exists(skills)) File.Delete(skills);
        }

        private void OnPlayerRevive(UnturnedPlayer player, UnityEngineCoreModule.UnityEngine.Vector3 position, byte angle)
        {
            KitSystem.GiveDefaultKit(player);
        }

        private void OnSkillsUpdated()
        {
            SkillSystem.RefreshPlayersSkills();
        }

        private void OnPlayerStatsUpdate(UnturnedPlayer player, EPlayerStat stat)
        {
            switch (stat)
            {
                case EPlayerStat.KILLS_ZOMBIES_NORMAL:
                    HordeUtils.ReceiveZombieDeathUpdate(player, "normal");
                    break;
                case EPlayerStat.KILLS_ZOMBIES_MEGA:
                    HordeUtils.ReceiveZombieDeathUpdate(player, "mega");
                    break;
            }
        }

        private void OnPlayerDead(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            UnityTickrate.PendingDeathItemClears.Add(player.Position);

            PowerupSystem.ResetPlayerPowerups(player);
            SkillSystem.ResetPlayerSkills(player);
            SkillSystem.RefreshPlayerSkills(player);
            ItemSystem.primaryWeapon.Remove(player);
            ItemSystem.secondaryWeapon.Remove(player);
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
                            }
                            ;
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
            {"round_end", "Wave complete..."},
            {"round_finish", "Congratulations, you survived the Horde!!!"},
            {"round_fail", "All survivors died, restarting round..."},
            {"wave_started", "Wave {0}, Zombies: {1}"},
            {"wave_remaining", "Remaining Zombies: {0}" },
            {"max_ammo", "Max Ammo, finded by: {0}" },
            {"refund_ammo", "Ammo purchased, refunded: {0} credits" },
            {"refund_powerup", "Powerup already equipped, refunded: {0} credits" },
            {"refund_powerup_grenade", "Grenades already full, refunded: {0} credits" },
            {"receive_powerup", "{0} received"},
            {"juggernog", "Juggernog"},
            {"estaminaup", "Estamina UP"},
            {"speedcola", "Speed Cola"},
            {"sharpshooter", "Sharpshooter"},
            {"main_weapon_moved", "Your weapon has been removed because you moved out of your equipment!" },
            {"unauthorized_weapon", "This weapon is not authorized on this server, it has been removed" },
            {"weapon_drop_denied", "You cannot drop this weapon or ammo" },
            {"not_enough_money", "Not enough money, necessary: {0}"},
            {"door_open", "Door opened, you lose: {0} money"},
            {"door_opened", "Door opened by {0}, with: {1} money"},
            {"packapunch_unavailable", "Unavailable Pack a Punch"},
            {"packapunch_maxlevel", "Pack a Punch is on Max Level"},
            {"packapunch", "Pack a Punch Received"},
            {"kit_received", "Kit received: {0}"},
            {"kit_nopermission", "Kit not found or no permission: {0}"},
            {"kit_available", "Available kits: {0}"},
            {"kit_unavailable", "Kit selection is only available in the designated area"},
        };
    }

    class UnityTickrate : UnityEngineCoreModule.UnityEngine.MonoBehaviour
    {
        public RoundSystem? RoundSystemInstance;
        // Positions where a player died and their loot should be cleared next tick, instead of
        // wiping every item on the map
        public static readonly List<UnityEngineCoreModule.UnityEngine.Vector3> PendingDeathItemClears = [];

        public void Start()
        {
            RoundSystemInstance = new(HordeServerPlugin.instance!.Configuration.Instance.TickrateBetweenRounds,
                HordeServerPlugin.instance!.Configuration.Instance.SpawnTickrate);
        }

        public void Update()
        {
            RoundSystemInstance?.Update();
            ItemSystem.Update();
            HealthRegenSystem.Update();

            if (PendingDeathItemClears.Count > 0)
            {
                float radius = HordeServerPlugin.instance!.Configuration.Instance.DeathItemsClearRadius;
                foreach (var position in PendingDeathItemClears)
                    ItemManager.ServerClearItemsInSphere(position, radius);

                PendingDeathItemClears.Clear();
            }
        }
    }
}
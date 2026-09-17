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

        private UnityTickrate? unityTickrate;
        public override void LoadPlugin()
        {
            base.LoadPlugin();
            instance = this;

            unityTickrate = gameObject.AddComponent<UnityTickrate>();

            // Vanilla passive health regen (PlayerLife, +1hp tick when food/water > 90) would add
            // stray HP between zombie hits, breaking the deterministic hits-to-kill math and fighting
            // the delay/duration timing of HealthRegenSystem. Disable it, HealthRegenSystem is now
            // the only thing allowed to heal players passively.
            Provider.modeConfigData.Players.Health_Regen_Ticks = uint.MaxValue;

            // Weapons no longer have a fixed primary/secondary slot in the config (WeaponLoadout.primary
            // was removed), placement is now decided dynamically at purchase time. But the asset itself
            // still carries its own baked-in Slot (PRIMARY/SECONDARY/etc from the .dat), which would still
            // block tryAddItem/tryEquip from placing it in whichever slot is free. Force every purchasable
            // weapon's asset to accept either slot 0 or 1, this must be reapplied on every asset load
            foreach (WeaponLoadout weaponLoadout in Configuration.Instance.AvailableWeaponsToPurchase)
            {
                if (Assets.find(EAssetType.ITEM, weaponLoadout.weapondId) is ItemAsset asset)
                {
                    asset.slot = ESlotType.SECONDARY;
                    if (Configuration.Instance.DebugWeaponSlots)
                        Logger.Log($"[WeaponSlots] Overrode asset.slot for weapondId {weaponLoadout.weapondId} ({asset.name}) to SECONDARY (accepts slot 0 or 1)");
                }
                else
                    Logger.LogWarning($"[WeaponSlots] Could not find ItemAsset for weapondId {weaponLoadout.weapondId}, slot override skipped, this weapon may still be restricted to its baked-in primary/secondary slot");
            }

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
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Invalid directory for players, please check the PlayerFolder configuration for HordeServer, exception: {ex.Message}");
            }

            BarricadeDrop.OnSalvageRequested_Global += DoorSystem.TryOpenDoor;
            System.Timers.Timer doorRefreshTimer = new();
            doorRefreshTimer.AutoReset = true;
            doorRefreshTimer.Interval = 1000;
            doorRefreshTimer.Elapsed += (_, __) => DoorSystem.RefreshOwnerships();
            doorRefreshTimer.Enabled = true;

            Logger.Log("HordeServer instanciated, by LeandroTheDev");
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            player.Events.OnDeath += OnPlayerDead;
            player.Events.OnInventoryAdded += ItemSystem.OnInventoryAdded;
            player.Events.OnInventoryRemoved += ItemSystem.OnInventoryRemoved;
            player.Inventory.onDropItemRequested += ItemSystem.OnItemDropped;
            player.Player.skills.onSkillsUpdated += OnSkillsUpdated;

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
            player.Events.OnDeath -= OnPlayerDead;
            player.Events.OnInventoryAdded -= ItemSystem.OnInventoryAdded;
            player.Events.OnInventoryRemoved -= ItemSystem.OnInventoryRemoved;
            player.Inventory.onDropItemRequested -= ItemSystem.OnItemDropped;
            player.Player.skills.onSkillsUpdated -= OnSkillsUpdated;

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
        };
    }

    class UnityTickrate : MonoBehaviour
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

            if (HordeServerPlugin.instance!.Configuration.Instance.ForceRemoveZombieRadiation)
                HordeUtils.RemoveZombiesRadiation();
            if (HordeServerPlugin.instance!.Configuration.Instance.ForceRemovePlayerRadiation)
                HordeUtils.RemovePlayersRadiation();

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
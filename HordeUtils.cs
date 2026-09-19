extern alias UnityEngineCoreModule;

using System.Collections.Generic;
using System.Linq;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Random = UnityEngineCoreModule.UnityEngine.Random;

namespace HordeServer;

class HordeUtils
{
    // Zombie/player spawn positions come from LocationNode(s) placed in the map editor with these
    // exact names (case-insensitive, any amount of them), instead of a manually-typed config list
    private const string ZombieSpawnNodeName = "zombiespawn";
    private const string PlayerSpawnNodeName = "playerspawn";

    public static List<uint> AlertsUsed = [];
    public static long zombiesToSpawn = 0;
    public static long zombiesAliveCountReference = 0;
    public static List<Zombie> zombiesAlive = [];
    public static ConfigWave? wave = null;
    // Per-zombie speed multiplier rolled once at spawn (ZombieSpeedMultiplier +/- ZombieSpeedVariance),
    // read every frame by HordeServerPlugin's speed override instead of the flat global multiplier
    public static Dictionary<Zombie, float> zombieSpeedMultipliers = [];

    private static List<LocationDevkitNode> GetLocationNodesByName(string name)
    {
        List<LocationDevkitNode> nodes = [];
        foreach (LocationDevkitNode node in LocationDevkitNodeSystem.Get().GetAllNodes())
        {
            if (string.Equals(node.locationName, name, System.StringComparison.InvariantCultureIgnoreCase))
                nodes.Add(node);
        }
        return nodes;
    }

    // A node named exactly "zombiespawn" always spawns zombies. A node named "zombiespawn" followed
    // by a number (e.g. "zombiespawn1") is gated behind a Door sharing that same DoorIndex: it only
    // spawns once at least one such door has been opened (DoorSystem.OpenedDoorIndexes), letting a
    // map block zombies out of an area until players pay to open a path into it. DoorIndex is not
    // unique, so opening any one of multiple doors sharing an index unlocks all of them at once.
    private static List<UnityEngineCoreModule.UnityEngine.Vector3> GetZombieSpawnNodePositions()
    {
        List<UnityEngineCoreModule.UnityEngine.Vector3> positions = [];

        foreach (LocationDevkitNode node in LocationDevkitNodeSystem.Get().GetAllNodes())
        {
            string name = node.locationName;
            if (string.IsNullOrEmpty(name) || !name.StartsWith(ZombieSpawnNodeName, System.StringComparison.InvariantCultureIgnoreCase)) continue;

            string suffix = name.Substring(ZombieSpawnNodeName.Length);
            if (suffix.Length == 0)
            {
                positions.Add(node.transform.position);
                continue;
            }

            if (!int.TryParse(suffix, out int doorIndex))
            {
                if (HordeServerPlugin.instance!.Configuration.Instance.DebugZombies)
                    Logger.LogWarning($"Location node \"{name}\" starts with \"{ZombieSpawnNodeName}\" but its suffix (\"{suffix}\") is not a valid DoorIndex number, ignoring it");
                continue;
            }

            // DoorIndex 0 means no gate (same convention as DoorSystem), always active
            if (doorIndex == 0 || DoorSystem.OpenedDoorIndexes.Contains(doorIndex))
                positions.Add(node.transform.position);
        }

        return positions;
    }

    // Angle is the node's own Y rotation in the map editor, so the map author controls which way
    // players face on spawn/respawn
    public static List<(UnityEngineCoreModule.UnityEngine.Vector3 position, float angle)> GetPlayerSpawnNodePositions()
    {
        return GetLocationNodesByName(PlayerSpawnNodeName)
            .Select(node => (node.transform.position, node.transform.eulerAngles.y))
            .ToList();
    }

    public static void SpawnZombiesInNodes()
    {
        if (HordeServerPlugin.alivePlayers.Count == 0
        || zombiesToSpawn <= 0
        || wave == null
        || HordeServerPlugin.instance == null) return;

        bool debug = HordeServerPlugin.instance.Configuration.Instance.DebugZombies;

        List<UnityEngineCoreModule.UnityEngine.Vector3> zombieSpawnPositions = GetZombieSpawnNodePositions();

        if (zombieSpawnPositions.Count == 0)
        {
            if (debug)
                Logger.LogError($"Cannot find any location node named \"{ZombieSpawnNodeName}\" in the map, add some in the map editor");
            return;
        }

        uint zombiesSpawnedThisTick = 0;
        uint maxZombiesPerTick = GetScaledMaxZombiesSpawnedPerTick(HordeServerPlugin.onlinePlayers.Count);

        List<UnityEngineCoreModule.UnityEngine.Vector3> zombiesNodesToSpawn = new(zombieSpawnPositions);
        foreach (UnityEngineCoreModule.UnityEngine.Vector3 _ in zombieSpawnPositions)
        {
            if (zombiesNodesToSpawn.Count == 0) return;
            if (maxZombiesPerTick > 0 && zombiesSpawnedThisTick >= maxZombiesPerTick) return;

            int nodeIndex = Random.Range(0, zombiesNodesToSpawn.Count);
            UnityEngineCoreModule.UnityEngine.Vector3 point = zombiesNodesToSpawn[nodeIndex];
            zombiesNodesToSpawn.RemoveAt(nodeIndex);

            // Skip nodes that are not close to at least one alive player
            bool nodeNearAnyPlayer = false;
            foreach (UnturnedPlayer alivePlayer in HordeServerPlugin.alivePlayers)
            {
                float distance = UnityEngineCoreModule.UnityEngine.Vector3.Distance(alivePlayer.Position, point);
                if (HordeServerPlugin.instance.Configuration.Instance.DebugPlayerPosition)
                    Logger.Log($"{alivePlayer.SteamName} node: {point.x},{point.y},{point.z} distance: {distance}");

                if (distance <= HordeServerPlugin.instance.Configuration.Instance.MaximumZombieNodeDistanceToSpawn)
                {
                    nodeNearAnyPlayer = true;
                    break;
                }
            }
            if (!nodeNearAnyPlayer)
            {
                if (HordeServerPlugin.instance.Configuration.Instance.DebugPlayerPosition)
                    Logger.Log($"node: {point.x},{point.y},{point.z} too far");
                continue;
            }

            // Each zombie belongs to a navmesh bound region. Reviving a zombie at a position
            // outside its own bound causes the zombie to glitch (fly, spawn dead). Always look up
            // the bound that contains the spawn point and pick a dead zombie from that bound's list
            if (!LevelNavigation.tryGetBounds(point, out byte bound))
            {
                if (debug)
                    Logger.LogWarning($"[ZombieSpawn] No navmesh bound at {point.x:F1},{point.y:F1},{point.z:F1} — node is outside nav areas, skipping");
                continue;
            }

            if (ZombieManager.regions == null || bound >= ZombieManager.regions.Length)
            {
                if (debug)
                    Logger.LogWarning($"[ZombieSpawn] bound {bound} out of range (regions.Length={ZombieManager.regions?.Length ?? -1}), skipping");
                continue;
            }

            Zombie? deadZombie = null;
            foreach (Zombie zombie in ZombieManager.regions[bound].zombies)
            {
                if (zombie.isDead)
                {
                    deadZombie = zombie;
                    break;
                }
            }

            if (deadZombie == null)
            {
                if (debug)
                    Logger.LogWarning($"[ZombieSpawn] All zombies in bound {bound} are alive, skipping position {point.x:F1},{point.y:F1},{point.z:F1}");
                continue;
            }

            EZombieSpeciality speciality = GetRandomZombieFromWave();
            // Use the zombie's own type (set by the map), not a hardcoded value — passing a type
            // that doesn't exist in the map's zombie tables causes the zombie to spawn dead
            byte type = deadZombie.type;
            GetRandomZombieClothing(type, out byte shirt, out byte pants, out byte hat, out byte gear);

            // Spawn slightly above the node so the zombie doesn't clip into terrain geometry
            var spawnPoint = point + new UnityEngineCoreModule.UnityEngine.Vector3(0f, 0.5f, 0f);
            deadZombie.sendRevive(type, (byte)speciality, shirt, pants, hat, gear, spawnPoint, Random.Range(0f, 360f));
            if (HordeServerPlugin.instance.Configuration.Instance.ForceRemoveZombieRadiation)
                RemoveZombiesRadiation();
            zombiesAlive.Add(deadZombie);
            zombiesToSpawn--;
            zombiesSpawnedThisTick++;

            float baseMultiplier = HordeServerPlugin.instance.Configuration.Instance.ZombieSpeedMultiplier;
            float variance = HordeServerPlugin.instance.Configuration.Instance.ZombieSpeedVariance;
            zombieSpeedMultipliers[deadZombie] = variance <= 0f
                ? baseMultiplier
                : baseMultiplier * (1f + Random.Range(-variance, variance));

            if (debug)
                Logger.Log($"[ZombieSpawn] Spawned: bound={bound} type={type} speciality={speciality} pos={point.x:F1},{point.y:F1},{point.z:F1} isDead={deadZombie.isDead} hp={deadZombie.GetHealth()}");

            if (zombiesToSpawn <= 0) return;
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

    // Scales a freshly cloned wave up based on how many players are in the match, so the server
    // doesn't spawn the same amount/toughness of zombies for 1 player as for a full squad. Must be
    // called on the clone (HordeUtils.wave), never on the original HordeServerConfiguration.Waves entry
    public static void ScaleWaveForPlayerCount(ConfigWave wave, int playerCount)
    {
        int extraPlayers = System.Math.Max(0, playerCount - 1);
        if (extraPlayers <= 0) return;

        float countScale = 1f + extraPlayers * HordeServerPlugin.instance!.Configuration.Instance.ZombieCountIncreasePerPlayer;
        float healthScale = 1f + extraPlayers * HordeServerPlugin.instance!.Configuration.Instance.ZombieHealthIncreasePerPlayer;

        wave.Acid = (long)System.Math.Ceiling(wave.Acid * countScale);
        wave.BossEletric = (long)System.Math.Ceiling(wave.BossEletric * countScale);
        wave.BossElverStomper = (long)System.Math.Ceiling(wave.BossElverStomper * countScale);
        wave.BossFire = (long)System.Math.Ceiling(wave.BossFire * countScale);
        wave.BossMagma = (long)System.Math.Ceiling(wave.BossMagma * countScale);
        wave.BossNuclear = (long)System.Math.Ceiling(wave.BossNuclear * countScale);
        wave.BossSprit = (long)System.Math.Ceiling(wave.BossSprit * countScale);
        wave.BossWind = (long)System.Math.Ceiling(wave.BossWind * countScale);
        wave.Burner = (long)System.Math.Ceiling(wave.Burner * countScale);
        wave.Crawler = (long)System.Math.Ceiling(wave.Crawler * countScale);
        wave.DLBlueVolatile = (long)System.Math.Ceiling(wave.DLBlueVolatile * countScale);
        wave.DLRedVolatile = (long)System.Math.Ceiling(wave.DLRedVolatile * countScale);
        wave.FlankerFriendly = (long)System.Math.Ceiling(wave.FlankerFriendly * countScale);
        wave.FlankerStalk = (long)System.Math.Ceiling(wave.FlankerStalk * countScale);
        wave.Mega = (long)System.Math.Ceiling(wave.Mega * countScale);
        wave.Normal = (long)System.Math.Ceiling(wave.Normal * countScale);
        wave.Spirit = (long)System.Math.Ceiling(wave.Spirit * countScale);
        wave.Sprinter = (long)System.Math.Ceiling(wave.Sprinter * countScale);

        wave.HealthMultiplier *= healthScale;

        if (HordeServerPlugin.instance!.Configuration.Instance.DebugZombies)
            Logger.Log($"[Difficulty] Scaled wave for {playerCount} players (+{extraPlayers} extra): countScale={countScale}, healthScale={healthScale}, HealthMultiplier={wave.HealthMultiplier}");
    }

    // Same scaling formula as ScaleWaveForPlayerCount: more players means more nodes near them are
    // eligible each tick, so the per-tick spawn cap needs to grow too or the trickle stays sized for
    // solo play. 0 (no cap) is left untouched
    public static uint GetScaledMaxZombiesSpawnedPerTick(int playerCount)
    {
        uint baseMax = HordeServerPlugin.instance!.Configuration.Instance.MaxZombiesSpawnedPerTick;
        if (baseMax == 0) return 0;

        int extraPlayers = System.Math.Max(0, playerCount - 1);
        if (extraPlayers <= 0) return baseMax;

        float scale = 1f + extraPlayers * HordeServerPlugin.instance!.Configuration.Instance.MaxZombiesSpawnedPerTickIncreasePerPlayer;
        return (uint)System.Math.Ceiling(baseMax * scale);
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
        zombiesAliveCountReference = zombiesToSpawn;
    }

    public static void ReceiveZombieDeathUpdate(UnturnedPlayer fromPlayer, string zombieType)
    {
        if (wave == null) return;

        fromPlayer.Experience += HordeServerPlugin.instance!.Configuration.Instance.KillCredits;

        // Special drop calculation
        float chance = Random.Range(0f, 100f);
        if (chance <= wave!.MaxAmmoChance)
        {
            foreach (UnturnedPlayer player in HordeServerPlugin.onlinePlayers)
            {
                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("max_ammo", fromPlayer.DisplayName),
                    new UnityEngineCoreModule.UnityEngine.Color(0, 255, 0),
                    null,
                    player.SteamPlayer(),
                    EChatMode.SAY,
                    HordeServerPlugin.instance!.Configuration.Instance.ChatIconURL,
                    true
                );
            }

            GiveMaxAmmo();
        }

        zombiesAliveCountReference--;

        uint? nextAlert = HordeServerPlugin.instance!.Configuration.Instance.RemainingZombiesAlert
            .Where(n => n >= zombiesAliveCountReference)
            .OrderBy(n => n)
            .FirstOrDefault();

        if (nextAlert != null && !AlertsUsed.Contains((uint)nextAlert))
        {
            AlertsUsed.Add((uint)nextAlert);

            foreach (UnturnedPlayer player in HordeServerPlugin.onlinePlayers)
            {
                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("wave_remaining", zombiesAliveCountReference),
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

    public static void CheckRemainingZombies()
    {
        List<Zombie> toRemove = [];

        foreach (Zombie zombie in zombiesAlive)
        {
            if (zombie.isDead)
                toRemove.Add(zombie);
        }

        foreach (Zombie zombie in toRemove)
        {
            zombiesAlive.Remove(zombie);
            zombieSpeedMultipliers.Remove(zombie);
        }

        if (HordeServerPlugin.instance!.Configuration.Instance.DebugZombies)
        {
            Logger.Log("---- Alive Zombies ----");
            foreach (Zombie zombie in zombiesAlive)
            {
                Logger.Log($"{zombie.transform.position.x},{zombie.transform.position.y},{zombie.transform.position.z}");
            }
        }
    }

    public static void CalculateZombieArmor(ref DamageZombieParameters parameters, ref bool _)
    {
        if (wave == null)
        {
            Logger.LogError("CalculateZombiesToSpawn called without any ConfigWave");
            return;
        }

        if (parameters.instigator is Player abstractPlayer)
        {
            float healthMult = wave.HealthMultiplier;
            switch (parameters.zombie.speciality)
            {
                case EZombieSpeciality.CRAWLER:
                    healthMult *= wave.CrawlerHealthMultiplier;
                    break;
                case EZombieSpeciality.MEGA:
                    healthMult *= wave.MegaHealthMultiplier;
                    break;
                case EZombieSpeciality.BOSS_ELECTRIC:
                case EZombieSpeciality.BOSS_ELVER_STOMPER:
                case EZombieSpeciality.BOSS_FIRE:
                case EZombieSpeciality.BOSS_MAGMA:
                case EZombieSpeciality.BOSS_NUCLEAR:
                case EZombieSpeciality.BOSS_SPIRIT:
                case EZombieSpeciality.BOSS_WIND:
                    healthMult *= wave.BossHealthMultiplier;
                    break;
            }
            parameters.damage *= 1f / healthMult;

            UnturnedPlayer player = UnturnedPlayer.FromPlayer(abstractPlayer);
            if (player.Player?.equipment?.itemID == null) return;

            // Weapon damage modifier
            if (ItemSystem.primaryWeapon.ContainsKey(player))
                if (player.Player.equipment.itemID == ItemSystem.primaryWeapon[player].weapondId)
                {
                    parameters.damage *= ItemSystem.primaryWeapon[player].baseDamage;
                    return;
                }
            if (ItemSystem.secondaryWeapon.ContainsKey(player))
                if (player.Player.equipment.itemID == ItemSystem.secondaryWeapon[player].weapondId)
                {
                    parameters.damage *= ItemSystem.secondaryWeapon[player].baseDamage;
                    return;
                }
        }
    }

    // Applied on top of the config/"grenadier" multipliers, matching the flat 5x the powerup grants
    private const float GrenadierDamageMultiplier = 5f;

    /// <summary>
    /// Explosive throwables carry their own damage fields (Grenade.zombieDamage) set at spawn time,
    /// and their explosion damage never reaches DamageTool.damageZombieRequested with a Player
    /// instigator, so CalculateZombieArmor never sees them. Scale the grenade's own field instead.
    /// </summary>
    public static void ApplyGrenadeDamageMultiplier(UseableThrowable useable, UnityEngineCoreModule.UnityEngine.GameObject throwable)
    {
        Grenade? grenade = throwable.GetComponent(typeof(Grenade)) as Grenade;
        if (grenade == null) return;

        float multiplier = HordeServerPlugin.instance!.Configuration.Instance.GrenadeDamageMultiplier;

        UnturnedPlayer player = UnturnedPlayer.FromPlayer(useable.player);
        if (PowerupSystem.PlayerHasPowerup(player, "grenadier")) multiplier *= GrenadierDamageMultiplier;

        grenade.zombieDamage *= multiplier;
    }

    /// <summary>
    /// Turns a NORMAL zombie into a Crawler when a grenade blast leaves it alive under the
    /// configured health fraction. Reuses the same "instigator is null" explosion signal as
    /// ApplyGrenadeDamageMultiplier's doc comment - guns/melee always assign a Player instigator
    /// before calling damageZombie, explosions never do.
    ///
    /// The final damage isn't applied yet at this point in DamageTool.damageZombie, so the amount
    /// that will land is predicted here: explosion hits always target ELimb.SPINE with
    /// respectArmor/allowBackstab both false, leaving only the pending global armor multiplier
    /// (applyGlobalArmorMultiplier) to account for, matching damageZombie's own math.
    /// </summary>
    public static void ConvertLowHealthGrenadeVictimToCrawler(ref DamageZombieParameters parameters, ref bool _)
    {
        float threshold = HordeServerPlugin.instance!.Configuration.Instance.GrenadeCrawlerHealthThreshold;
        if (threshold <= 0f) return;

        if (parameters.instigator != null) return;

        Zombie zombie = parameters.zombie;
        if (zombie == null || zombie.isDead || zombie.speciality != EZombieSpeciality.NORMAL) return;

        float globalMultiplier = parameters.applyGlobalArmorMultiplier
            ? Provider.modeConfigData.Zombies.NonHeadshot_Armor_Multiplier
            : 1f;
        int roundedDamage = (int)System.Math.Floor(parameters.damage * parameters.times * globalMultiplier);
        if (roundedDamage <= 0) return;

        float healthAfter = zombie.GetHealth() - roundedDamage;
        if (healthAfter <= 0f || healthAfter > zombie.GetMaxHealth() * threshold) return;

        ZombieManager.sendZombieSpeciality(zombie, EZombieSpeciality.CRAWLER);
    }

    public static void HitPoints(ref DamageZombieParameters parameters, ref bool _)
    {
        object instigator = parameters.instigator;
        if (instigator is Player abstractPlayer)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(abstractPlayer);
            player.Experience += HordeServerPlugin.instance!.Configuration.Instance.HitCredits;
        }
    }

    /// <summary>
    /// Forces zombie hits to always take a fixed number of hits to kill, regardless of
    /// zombie damage, clothing armor or the game mode's armor multiplier, by overriding the
    /// final damage/times used in DamageTool.damagePlayer. Only affects EDeathCause.ZOMBIE,
    /// every other death cause (fall, drowning, suicide, etc.) is left untouched.
    /// </summary>
    public static void CalculatePlayerLifeFromZombieHit(ref DamagePlayerParameters parameters, ref bool _)
    {
        UnturnedPlayer player = UnturnedPlayer.FromPlayer(parameters.player);
        // Regen delay resets on any damage cause, not just zombie hits
        HealthRegenSystem.RegisterHit(player);

        if (parameters.cause != EDeathCause.ZOMBIE) return;

        uint hitsToKill = PowerupSystem.PlayerHasPowerup(player, "juggernog")
            ? HordeServerPlugin.instance!.Configuration.Instance.HitsToKillPlayerWithJuggernog
            : HordeServerPlugin.instance!.Configuration.Instance.HitsToKillPlayer;

        if (hitsToKill <= 0) return;

        parameters.damage = (float)System.Math.Min(byte.MaxValue, System.Math.Ceiling(100f / hitsToKill));
        parameters.times = 1f;
        // Prevent clothing armor and Players.Armor_Multiplier from changing the fixed hit count
        parameters.respectArmor = false;
        parameters.applyGlobalArmorMultiplier = false;
    }

    // Pack-a-Punch forces a gun's state bytes (sight/tactical/grip/barrel/magazine) directly from
    // config, without ever giving the player a real attachment item. The vanilla attach/detach RPCs
    // mint a genuine attachment item out of whatever is encoded in state when the player removes or
    // replaces one, so if left alone Pack-a-Punch would be a free-loot exploit. Block any attachment
    // change on a currently pack-a-punched weapon, this same handler is shared by all 5 attachment
    // slot events since they all use the same signature
    public static void BlockPackAPunchAttachmentChange(PlayerEquipment equipment, UseableGun gun, SDG.Unturned.Item oldItem, ItemJar newItem, ref bool shouldAllow)
    {
        UnturnedPlayer? player = UnturnedPlayer.FromPlayer(equipment.player);
        if (player == null) return;

        byte page = equipment.equippedPage;
        if (page != 0 && page != 1) return;

        if (PowerupSystem.IsPackAPunched(player, page))
            shouldAllow = false;
    }

    public static void GiveMaxAmmo(UnturnedPlayer? uniquePlayer = null)
    {
        void GiveAmmo(UnturnedPlayer player)
        {
            // We are giving ammo, the item system doens't know if is purchased
            // We are preventing purchase refunds for 4 ticks
            if (!ItemSystem.ignoredRefunds.ContainsKey(player))
                ItemSystem.ignoredRefunds.Add(player, 4);

            ItemJar primaryWeapon = player.Inventory.getItem(0, 0);
            ItemJar secondaryWeapon = player.Inventory.getItem(1, 0);

            WeaponLoadout? primaryWeaponLoadout = null;
            WeaponLoadout? secondaryWeaponLoadout = null;

            // Getting available loadout for equipped weapons
            foreach (WeaponLoadout loadout in HordeServerPlugin.instance!.Configuration.Instance.AvailableWeaponsToPurchase)
            {
                if (primaryWeapon?.item != null)
                    if (loadout.weapondId == primaryWeapon.item.id)
                        primaryWeaponLoadout = loadout;

                if (secondaryWeapon?.item != null)
                    if (loadout.weapondId == secondaryWeapon.item.id)
                        secondaryWeaponLoadout = loadout;
            }

            void removePreviouslyAmmo(WeaponLoadout loadout)
            {
                for (byte page = 0; page < PlayerInventory.PAGES; page++)
                {
                    try
                    {
                        for (byte j = 0; j < player.Inventory.getItemCount(page); j++)
                        {
                            if (player.Inventory.getItem(page, j).item.id == loadout.ammoId)
                            {
                                player.Inventory.removeItem(page, j);
                                j--;
                            }
                        }
                    }
                    catch (System.Exception) { }
                }
            }

            if (primaryWeaponLoadout != null)
            {
                removePreviouslyAmmo(primaryWeaponLoadout);
                player.GiveItem(primaryWeaponLoadout.ammoId, primaryWeaponLoadout.ammoRefilQuantity);
            }
            if (secondaryWeaponLoadout != null)
            {
                removePreviouslyAmmo(secondaryWeaponLoadout);
                player.GiveItem(secondaryWeaponLoadout.ammoId, secondaryWeaponLoadout.ammoRefilQuantity);
            }

            PowerupSystem.GiveMaxGrenadesForPlayer(player);
        }

        if (uniquePlayer != null)
        {
            GiveAmmo(uniquePlayer);
            return;
        }

        foreach (UnturnedPlayer player in HordeServerPlugin.alivePlayers)
        {
            GiveAmmo(player);
        }
    }

    // Randomizes clothing the same way the engine's own (internal, so not callable from a plugin)
    // ZombieTable.GetSpawnClothingParameters does: roll each slot (Shirt/Pants/Hat/Gear) against its
    // "chance" from the map's Zombies.dat/Cloth editor, picking a random item within that slot's table
    // or 255 (no item) on a miss. Falls back to the old hardcoded look if the type has no table.
    private static void GetRandomZombieClothing(byte type, out byte shirt, out byte pants, out byte hat, out byte gear)
    {
        if (LevelZombies.tables == null || type >= LevelZombies.tables.Count)
        {
            shirt = pants = hat = gear = 1;
            return;
        }

        ZombieSlot[] slots = LevelZombies.tables[type].slots;
        shirt = RandomClothingIndex(slots[0]);
        pants = RandomClothingIndex(slots[1]);
        hat = RandomClothingIndex(slots[2]);
        gear = RandomClothingIndex(slots[3]);
    }

    private static byte RandomClothingIndex(ZombieSlot slot)
    {
        if (slot.table.Count > 0 && Random.value < slot.chance)
            return (byte)Random.Range(0, slot.table.Count);
        return 255;
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

        if (zombies.Count > 0)
        {
            int index = Random.Range(0, zombies.Count);
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

    // ZombieRegion.isRadioactive is computed once when the level loads (from the map's Deadzone/Zone
    // volumes) and never changes on its own, so this only needs to run once at plugin load, over every
    // region, instead of every tick over whichever zombies happen to be alive
    public static void RemoveZombiesRadiation()
    {
        if (ZombieManager.regions == null) return;

        foreach (ZombieRegion region in ZombieManager.regions)
            region.isRadioactive = false;
    }

    // PlayerLife.virus only ever changes through askInfect/askRadiate/askDisinfect, and all three
    // fire OnTellVirus_Global right after applying their change (SDG.Unturned.PlayerLife), so hooking
    // it lets us correct back to full immunity exactly when needed instead of every tick. The
    // life.virus >= 100 guard both skips players who are already fully immune and stops this handler
    // from re-triggering itself forever, since askDisinfect below also fires OnTellVirus_Global
    public static void ForcePlayerFullImmunity(PlayerLife life)
    {
        if (life.virus >= 100) return;

        UnturnedPlayer? player = UnturnedPlayer.FromPlayer(life.player);
        if (player == null || !HordeServerPlugin.alivePlayers.Contains(player)) return;

        life.askDisinfect((byte)(100 - life.virus));
    }
}

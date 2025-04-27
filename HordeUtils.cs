extern alias UnityEngineCoreModule;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rocket.Core.Logging;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace HordeServer;

class HordeUtils
{
    private static readonly System.Random random = new();
    public static List<uint> AlertsUsed = [];
    public static long zombiesToSpawn = 0;
    public static long zombiesAliveCountReference = 0;
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

        if (zombieNodes.Count == 0)
        {
            if (HordeServerPlugin.instance.Configuration.Instance.DebugZombies)
                Logger.LogError("Cannot find any zombie node in map center, try increasing the radius and checking the position");
            return;
        }

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
                if (zombie.isDead)
                {
                    bool nodeeToFar = false;
                    foreach (UnturnedPlayer alivePlayer in HordeServerPlugin.alivePlayers)
                    {
                        float distance = UnityEngineCoreModule.UnityEngine.Vector3.Distance(alivePlayer.Position, point);
                        if (HordeServerPlugin.instance.Configuration.Instance.DebugPlayerPosition)
                            Logger.Log($"{alivePlayer.SteamName} node: {point.x},{point.y},{point.z} distance: {distance}");

                        if (distance > HordeServerPlugin.instance.Configuration.Instance.MaximumZombieNodeDistanceToSpawn)
                        {
                            nodeeToFar = true;
                            break;
                        }
                    }
                    if (nodeeToFar)
                    {
                        if (HordeServerPlugin.instance.Configuration.Instance.DebugPlayerPosition)
                            Logger.Log($"node: {point.x},{point.y},{point.z} too far");
                        continue;
                    };

                    if (HordeServerPlugin.instance.Configuration.Instance.DebugZombies)
                        Logger.Log($"Zombie Spawned in: {point.x},{point.y}{point.z}");

                    zombie.sendRevive(type, (byte)speciality, shirt, pants, hat, gear, point, Random.Range(0f, 360f));
                    zombiesAlive.Add(zombie);
                    zombiesToSpawn--;

                    if (zombiesToSpawn <= 0) return;
                    zombieSpawned = true;
                    break;
                }
            }

            // All zombies is alive
            if (!zombieSpawned)
            {
                if (HordeServerPlugin.instance.Configuration.Instance.DebugZombies)
                    Logger.LogWarning("No zombies spawned in the tick, because all available zombies nodes is alive");
                break;
            }
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
        zombiesAliveCountReference = zombiesToSpawn;
    }

    public static void ReceiveZombieDeathUpdate(UnturnedPlayer fromPlayer, string zombieType)
    {
        if (Random.Range(0f, 100f) <= wave!.MaxAmmoChance)
        {
            foreach (UnturnedPlayer player in HordeServerPlugin.onlinePlayers)
            {
                ChatManager.serverSendMessage(
                    HordeServerPlugin.instance!.Translate("max_ammo", player.DisplayName),
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

        parameters.damage *= 1f / wave.HealthMultiplier;
    }

    public static void GiveMaxAmmo()
    {
        
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

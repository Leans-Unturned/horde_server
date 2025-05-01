using System.Collections.Generic;
using Rocket.API;

namespace HordeServer
{
    public class HordeServerConfiguration : IRocketPluginConfiguration
    {
        public bool ForceRemoveZombieRadiation = true;
        public uint SpawnTickrate = 600;
        public uint RemainingCheckTickrate = 600;
        public uint TickrateBetweenRounds = 1000;
        public int SecondsAfterRoundFail = 10;
        public ConfigPosition MapCenterPosition = new();
        public int MapCenterRadius = 10000;
        public int MaximumZombieNodeDistanceToSpawn = 150;
        public bool DebugPlayerPosition = false;
        public bool DebugZombies = false;
        public string PlayersFolder = "SteamLibrary/steamapps/common/U3DS/Servers/myserver/Players/";
        public string LevelName = "PEI";

        public string ChatIconURL = "https://add-image-url.com";

        public List<ZombieNodePosition> ZombiesAvailableNodes = [];
        public List<ConfigPosition> PlayerSpawnPositions = [];
        public List<ConfigWave> Waves = [];
        public List<uint> RemainingZombiesAlert = [];
        public List<WeaponLoadout> AvailableWeaponsToPurchase = [];
        public List<PowerupLoadout> AvailablePowerupsToPurchase = [];

        public uint StartingCredits = 500;
        public uint HitCredits = 10;

        public void LoadDefaults()
        {
            ZombiesAvailableNodes = [
                new () {
                    NodeName = "civilian",
                    X = 10,
                    Y = 10,
                    Z = 10,
                    Angle = 0,
                },
                new () {
                    NodeName = "military",
                    X = 20,
                    Y = 10,
                    Z = 20,
                    Angle = 0,
                }
            ];

            PlayerSpawnPositions = [
                new() {
                    X = 10,
                    Y = 10,
                    Z = 10
                },
                new() {
                    X = 15,
                    Y = 10,
                    Z = 15
                }
            ];

            RemainingZombiesAlert = [1000, 500, 250, 100, 50, 20, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1];

            Waves = [
                new() {
                    Normal = 10,
                    HealthMultiplier = 0.1f,
                },
                new() {
                    Normal = 10,
                    Crawler = 3,
                    HealthMultiplier = 0.2f,
                },
                new() {
                    Normal = 20,
                    Crawler = 5,
                    Acid = 1,
                    HealthMultiplier = 0.3f,
                },
                new() {
                    Normal = 30,
                    Crawler = 10,
                    Acid = 1,
                    HealthMultiplier = 0.4f,
                },
                new() {
                    Normal = 40,
                    Crawler = 15,
                    Acid = 2,
                    Burner = 1,
                    HealthMultiplier = 0.5f,
                },
                new() {
                    Normal = 60,
                    Crawler = 20,
                    Acid = 3,
                    Burner = 2,
                    HealthMultiplier = 0.6f,
                },
                new() {
                    Normal = 90,
                    Crawler = 30,
                    Acid = 4,
                    Burner = 3,
                    HealthMultiplier = 0.7f,
                },
                new() {
                    Normal = 100,
                    Crawler = 30,
                    Acid = 4,
                    Burner = 3,
                    Mega = 1,
                    HealthMultiplier = 0.8f,
                },
                new() {
                    Normal = 150,
                    Crawler = 40,
                    Acid = 6,
                    Burner = 5,
                    Mega = 2,
                    HealthMultiplier = 0.9f,
                },
                new() {
                    BossWind = 1,
                    HealthMultiplier = 1.0f,
                },
                new() {
                    Normal = 180,
                    Crawler = 45,
                    Acid = 8,
                    Burner = 15,
                    Mega = 2,
                    HealthMultiplier = 1.1f,
                },
                new() {
                    Normal = 160,
                    Crawler = 45,
                    Acid = 28,
                    Burner = 15,
                    Mega = 2,
                    HealthMultiplier = 1.2f,
                },
                new() {
                    Normal = 140,
                    Crawler = 45,
                    Acid = 28,
                    Burner = 35,
                    Mega = 2,
                    HealthMultiplier = 1.3f,
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 28,
                    Burner = 35,
                    Mega = 4,
                    HealthMultiplier = 1.4f,
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 28,
                    Burner = 35,
                    Mega = 4,
                    HealthMultiplier = 1.5f,
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 28,
                    Burner = 35,
                    Mega = 4,
                    HealthMultiplier = 1.6f,
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 28,
                    Burner = 35,
                    Mega = 4,
                    HealthMultiplier = 1.7f,
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 28,
                    Burner = 35,
                    Mega = 4,
                    HealthMultiplier = 1.8f,
                },
                new() {
                    Normal = 80,
                    Crawler = 15,
                    Acid = 28,
                    Burner = 85,
                    Mega = 4,
                    HealthMultiplier = 1.9f,
                },
                new() {
                    BossFire = 2,
                    HealthMultiplier = 2.0f,
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 48,
                    Burner = 35,
                    Mega = 4,
                    HealthMultiplier = 2.1f,
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 88,
                    Burner = 35,
                    Mega = 4,
                    HealthMultiplier = 2.2f,
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4,
                    HealthMultiplier = 2.3f,
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4,
                    HealthMultiplier = 2.4f,
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4,
                    HealthMultiplier = 2.5f,
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4,
                    HealthMultiplier = 2.6f,
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4,
                    HealthMultiplier = 2.7f,
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4,
                    HealthMultiplier = 2.8f,
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4,
                    HealthMultiplier = 2.9f,
                },
                new() {
                    BossNuclear = 4,
                    HealthMultiplier = 3.0f,
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 20,
                    Mega = 6,
                    HealthMultiplier = 3.1f,
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 20,
                    Mega = 6,
                    HealthMultiplier = 3.2f,
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 20,
                    Mega = 6,
                    HealthMultiplier = 3.3f,
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 20,
                    Mega = 6,
                    HealthMultiplier = 3.4f,
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 20,
                    Mega = 6,
                    HealthMultiplier = 3.5f,
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 40,
                    Mega = 6,
                    HealthMultiplier = 3.6f,
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 60,
                    Mega = 6,
                    HealthMultiplier = 3.7f,
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 80,
                    Mega = 6,
                    HealthMultiplier = 3.8f,
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 100,
                    Mega = 6,
                    HealthMultiplier = 3.9f,
                },
                new() {
                    BossMagma = 8,
                    HealthMultiplier = 4.0f,
                },
            ];

            AvailableWeaponsToPurchase = [
                new() {
                    weapondId = 101,
                    ammoId = 103,
                    ammoRefilQuantity = 5,
                },
                new() {
                    weapondId = 380,
                    ammoId = 381,
                    ammoRefilQuantity = 10
                }
            ];

            AvailablePowerupsToPurchase = [
                new() {
                    itemId = 1051,
                    powerupType = "juggernog"
                },
                new() {
                    itemId = 1052,
                    powerupType = "speedcola"
                },
                new() {
                    itemId = 1053,
                    powerupType = "estaminaup"
                },
            ];
        }
    }

    public class ZombieNodePosition
    {
        public string NodeName = "default";
        public float X = 0;
        public float Y = 0;
        public float Z = 0;
        public float Angle = 0.0f;
    }

    public class ConfigPosition
    {
        public float X = 0;
        public float Y = 0;
        public float Z = 0;
        public float Angle = 0.0f;
    }

    public class ConfigWave
    {
        public long Acid = 0;
        public long BossEletric = 0;
        public long BossElverStomper = 0;
        public long BossFire = 0;
        public long BossMagma = 0;
        public long BossNuclear = 0;
        public long BossSprit = 0;
        public long BossWind = 0;
        public long Burner = 0;
        public long Crawler = 0;
        public long DLBlueVolatile = 0;
        public long DLRedVolatile = 0;
        public long FlankerFriendly = 0;
        public long FlankerStalk = 0;
        public long Mega = 0;
        public long Normal = 0;
        public long Spirit = 0;
        public long Sprinter = 0;
        public float HealthMultiplier = 1.0f;
        public float MaxAmmoChance = 0.05f;

        public ConfigWave Clone()
        {
            return new ConfigWave
            {
                Acid = Acid,
                BossEletric = BossEletric,
                BossElverStomper = BossElverStomper,
                BossFire = BossFire,
                BossMagma = BossMagma,
                BossNuclear = BossNuclear,
                BossSprit = BossSprit,
                BossWind = BossWind,
                Burner = Burner,
                Crawler = Crawler,
                DLBlueVolatile = DLBlueVolatile,
                DLRedVolatile = DLRedVolatile,
                FlankerFriendly = FlankerFriendly,
                FlankerStalk = FlankerStalk,
                Mega = Mega,
                Normal = Normal,
                Spirit = Spirit,
                Sprinter = Sprinter,
                HealthMultiplier = HealthMultiplier,
                MaxAmmoChance = MaxAmmoChance,
            };
        }
    }

    public class Skill
    {
        public byte Agriculture = 0;
        public byte Cardio = 0;
        public byte Cooking = 0;
        public byte Crafting = 0;
        public byte Dexerity = 0;
        public byte Diving = 0;
        public byte Engineer = 0;
        public byte Exercise = 0;
        public byte Fishing = 0;
        public byte Healing = 0;
        public byte Immunity = 0;
        public byte Mechanic = 0;
        public byte Outdoors = 0;
        public byte Overkill = 0;
        public byte Parkour = 0;
        public byte Sharpshooter = 0;
        public byte Sneakybeaky = 0;
        public byte Strength = 0;
        public byte Survival = 0;
        public byte Toughness = 0;
        public byte Vitality = 0;
        public byte Warmblooded = 0;
    }

    public class WeaponLoadout
    {
        public ushort weapondId = 0;
        public ushort ammoId = 0;
        public byte ammoRefilQuantity = 0;
        public bool primary = true;
    }

    public class PowerupLoadout
    {
        public ushort itemId = 0;
        public string powerupType = "juggernog";
    }
}
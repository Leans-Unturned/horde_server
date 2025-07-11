using System.Collections.Generic;
using Rocket.API;

namespace HordeServer
{
    public class HordeServerConfiguration : IRocketPluginConfiguration
    {
        public bool ForceRemoveZombieRadiation = true;
        public bool ForceRemovePlayerRadiation = true;
        public uint SpawnTickrate = 600;
        public uint RemainingCheckTickrate = 600;
        public uint TickrateBetweenRounds = 1000;
        public int SecondsAfterRoundFail = 10;
        public ConfigPosition MapCenterPosition = new();
        public int MapCenterRadius = 10000;
        public int MaximumZombieNodeDistanceToSpawn = 150;
        public bool DebugPlayerPosition = false;
        public bool DebugBarricadesPosition = false;
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
        public List<Door> AvailableDoorsToPurchase = [];
        public List<PackAPunchWeapon> AvailablePackAPunch = [];

        public uint StartingCredits = 500;
        public uint HitCredits = 10;
        public uint MaxGrenades = 4;

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
                    primary = true,
                    refundValue = 500,
                    ammoRefundValue = 200,
                    baseDamage = 2.0f
                },
                new() {
                    weapondId = 380,
                    ammoId = 381,
                    ammoRefilQuantity = 10,
                    primary = true,
                    refundValue = 500,
                    ammoRefundValue = 200,
                    baseDamage = 1.0f
                }
            ];

            AvailablePowerupsToPurchase = [
                new() {
                    itemId = 1051,
                    powerupType = "juggernog",
                    refundValue = 2500
                },
                new() {
                    itemId = 1052,
                    powerupType = "speedcola",
                    refundValue = 3000
                },
                new() {
                    itemId = 1053,
                    powerupType = "estaminaup",
                    refundValue = 2000
                },
                new() {
                    itemId = 1054,
                    powerupType = "packapunch",
                    refundValue = 5000
                },
                new() {
                    itemId = 1055,
                    powerupType = "grenades",
                    refundValue = 750
                },
            ];

            AvailableDoorsToPurchase = [
                // Laundry White House
                new() {
                pos = new(51.65f, 54.04f, -78.41f),
                    rotation = new(-0.50111f, -0.49888f, -0.49888f, 0.50111f),
                    cost = 750,
                    assetId = 30,
                },

                // To upstars White House
                new() {
                    pos = new(47.99f, 54.04f, -72.36f),
                    rotation = new(-0.50360f, -0.49637f, -0.49637f, 0.50360f),
                    cost = 800,
                    assetId = 30,
                },

                // 1 Upstairs White House
                new() {
                    pos = new(58.70f, 58.79f, -82.03f),
                    rotation = new(-0.70705f, 0.00897f, 0.00897f, 0.70705f),
                    cost = 1000,
                    assetId = 30,
                },

                // Speed Cola White House
                new() {
                    pos = new(58.62f, 58.79f, -89.52f),
                    rotation = new(-0.70703f, 0.01051f, 0.01051f, 0.70703f),
                    cost = 1500,
                    assetId = 30,
                },

                // 2 Upstairs Back Window White House 
                new() {
                    pos = new(61.08f, 59.79f, -84.72f),
                    rotation = new(-0.49629f, 0.50368f, 0.50368f, 0.49629f),
                    cost = 1000,
                    assetId = 30,
                },

                // Back House White House 
                new() {
                    pos = new(73.68f, 54.00f, -65.68f),
                    rotation = new(0.00140f, -0.70711f, -0.70711f, -0.00140f),
                    cost = 1650,
                    assetId = 30,
                },

                // Laundry Brown House
                new() {
                    pos = new(4.13f, 54.04f, -83.54f),
                    rotation = new(-0.49209f, 0.50779f, 0.50779f, 0.49209f),
                    cost = 750,
                    assetId = 30,
                },

                // To upstars Brown House
                new() {
                    pos = new(7.56f, 54.04f, -89.56f),
                    rotation = new(0.48605f, -0.51357f, -0.51357f, -0.48605f),
                    cost = 800,
                    assetId = 30,
                },

                // 1 Upstairs Brown House
                new() {
                    pos = new(-2.78f, 58.79f, -79.66f),
                    rotation = new(0.01159f, 0.70701f, 0.70701f, -0.01159f),
                    cost = 1000,
                    assetId = 30,
                },

                // 2 Upstairs Back Window White House 
                new() {
                    pos = new(-4.98f, 59.79f, -76.98f),
                    rotation = new(0.50276f, 0.49722f, 0.49722f, -0.50276f),
                    cost = 1000,
                    assetId = 30,
                },

                // Pack a Punch brown House
                new() {
                    pos = new(-27.63f, 53.82f, -78.26f),
                    rotation = new(-0.48098f, 0.51832f, 0.51832f, 0.48098f),
                    cost = 1000,
                    assetId = 30,
                },
            ];

            AvailablePackAPunch = [
                // 1911
                new() {
                    Id = 97,
                    AvailableLevelsMetada = [
                        [0,0,0,0,0,0,221,1,98,0,7,1,1,100,100,100,100,100],
                        [0,0,0,0,0,0,221,1,98,0,7,1,1,100,100,100,100,100],
                        [0,0,151,0,0,0,221,1,98,0,7,1,1,100,100,100,100,100],
                    ],
                    AvailableLevelsDamage = [
                        0.5f,
                        1.0f,
                        1.5f,
                        2.0f,
                        2.5f,
                        3.0f,
                        4.5f,
                        5.0f,
                        5.5f,
                        6.0f,
                    ]
                },
                // Masterkey
                new() {
                    Id = 380,
                    AvailableLevelsMetada = [],
                    AvailableLevelsDamage = [
                        1.0f,
                        2.0f,
                        3.0f,
                        4.0f,
                        5.0f,
                        6.0f,
                        7.0f,
                        8.0f,
                        9.0f,
                        10.0f,
                    ]
                },
                // Schofield
                new() {
                    Id = 101,
                    AvailableLevelsMetada = [
                        [102,0,0,0,0,0,221,1,103,0,5,1,1,100,100,100,100,100],
                        [102,0,151,0,0,0,221,1,103,0,5,1,1,100,100,100,100,100],
                        [102,0,151,0,8,0,221,1,103,0,5,1,1,100,100,100,100,100],
                        [146,0,151,0,145,0,221,1,103,0,5,1,1,100,100,100,65,100],
                    ],
                    AvailableLevelsDamage = [
                        2.0f,
                        4.0f,
                        6.0f,
                        8.0f,
                        12.0f,
                        14.0f,
                        16.0f,
                        18.0f,
                        20.0f,
                        22.0f,
                    ]
                },
                // Avenger
                new() {
                    Id = 1021,
                    AvailableLevelsMetada = [
                        [0,0,0,0,0,0,150,0,254,3,13,1,1,100,100,100,100,100],
                        [0,0,0,0,0,0,149,0,254,3,13,1,1,100,100,100,100,100],
                        [0,0,151,0,0,0,149,0,254,3,13,1,1,100,100,100,100,100],
                    ],
                    AvailableLevelsDamage = [
                        1.0f,
                        1.5f,
                        2.0f,
                        2.5f,
                        3.0f,
                        3.5f,
                        4.0f,
                        4.5f,
                        5.0f,
                        5.5f,
                    ]
                },
                // Bluntforce
                new() {
                    Id = 112,
                    AvailableLevelsMetada = [
                        [114,0,0,0,8,0,0,0,113,0,8,1,1,100,100,100,100,100],
                        [146,0,0,0,8,0,0,0,113,0,8,1,1,100,100,100,100,100],
                        [0,0,151,0,0,0,149,0,254,3,13,1,1,100,100,100,100,100],
                    ],
                    AvailableLevelsDamage = [
                        1.0f,
                        2.0f,
                        3.0f,
                        4.0f,
                        5.0f,
                        6.0f,
                        7.0f,
                        8.0f,
                        9.0f,
                        10.0f,
                    ]
                },
                // Bulldog
                new() {
                    Id = 1369,
                    AvailableLevelsMetada = [
                        [90,5,0,0,0,0,166,4,91,5,45,2,1,100,100,100,100,100],
                        [147,0,0,0,0,0,166,4,91,5,45,2,1,100,100,100,100,100],
                        [147,0,0,0,0,0,167,4,91,5,45,2,1,100,100,100,100,100],
                        [147,0,0,0,8,0,167,4,91,5,45,2,1,100,100,100,100,100],
                        [147,0,0,0,145,0,167,4,91,5,45,2,1,100,100,100,100,100]
                    ],
                    AvailableLevelsDamage = [
                        0.5f,
                        1.0f,
                        1.5f,
                        2.0f,
                        2.5f,
                        3.0f,
                        4.5f,
                        5.0f,
                        5.5f,
                        6.0f,
                    ]
                },
                // Mapplestrike
                new() {
                    Id = 363,
                    AvailableLevelsMetada = [
                        [108,1,0,0,0,0,150,0,6,0,30,2,1,100,100,100,100,100],
                        [108,1,0,0,8,0,150,0,6,0,30,2,1,100,100,100,100,100],
                        [146,0,0,0,8,0,150,0,6,0,30,2,1,100,100,100,100,100],
                        [146,0,151,0,8,0,150,0,6,0,30,2,1,100,100,100,100,100],
                        [146,0,151,0,8,0,149,0,6,0,30,2,1,100,100,100,100,100],
                        [146,0,151,0,145,0,149,0,6,0,30,2,1,100,100,100,100,100],
                        [148,0,151,0,145,0,149,0,6,0,30,2,1,100,100,100,100,100]
                    ],
                    AvailableLevelsDamage = [
                        0.5f,
                        1.0f,
                        1.5f,
                        2.0f,
                        2.5f,
                        3.0f,
                        4.5f,
                        5.0f,
                        5.5f,
                        6.0f,
                    ]
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
        public uint refundValue = 0;
        public uint ammoRefundValue = 0;
        public float baseDamage = 1.0f;
    }

    public class PowerupLoadout
    {
        public ushort itemId = 0;
        public string powerupType = "juggernog";
        public uint refundValue = 0;
    }

    public class PackAPunchWeapon
    {
        public ushort Id = 1;
        public List<byte[]> AvailableLevelsMetada = [];
        public List<float> AvailableLevelsDamage = [];
    }
}
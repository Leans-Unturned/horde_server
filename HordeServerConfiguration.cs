using System.Collections.Generic;
using Rocket.API;

namespace HordeServer
{
    public class HordeServerConfiguration : IRocketPluginConfiguration
    {
        public bool ForceRemoveZombieRadiation = true;
        public bool ForceRemovePlayerRadiation = true;
        public uint SpawnTickrate = 100;
        public uint RemainingCheckTickrate = 300;
        public uint TickrateBetweenRounds = 1000;
        public int SecondsAfterRoundFail = 10;
        public int MaximumZombieNodeDistanceToSpawn = 150;
        // Caps how many zombies can be revived in a single SpawnZombiesInNodes tick, so a map with
        // many eligible nodes near the players doesn't dump them all in at once. 0 = no cap
        public uint MaxZombiesSpawnedPerTick = 3;
        // Scales MaxZombiesSpawnedPerTick up per player beyond the first, same formula as
        // ZombieCountIncreasePerPlayer, so more players (more nodes covered at once) still get a
        // steady stream instead of the same trickle sized for solo play. 0 disables the scaling
        public float MaxZombiesSpawnedPerTickIncreasePerPlayer = 0.5f;
        public float DeathItemsClearRadius = 10f;
        public uint HitsToKillPlayer = 2;
        public uint HitsToKillPlayerWithJuggernog = 4;
        public float HealthRegenDelay = 2f;
        public float HealthRegenDuration = 1f;
        public float StaminaRegenDelay = 2f;
        public float StaminaRegenDuration = 3f;
        // Scales each wave's zombie counts/health up per player beyond the first, so a full server
        // is not the same difficulty as someone playing solo. 0 disables the respective scaling
        public float ZombieCountIncreasePerPlayer = 0.5f;
        public float ZombieHealthIncreasePerPlayer = 0.25f;
        // Multiplier applied to every zombie's movement speed each frame (1.0 = no change, 0.75 = 75% of original)
        public float ZombieSpeedMultiplier = 0.75f;
        // Random +/- variance applied per zombie on top of ZombieSpeedMultiplier, rolled once when it
        // spawns and kept for its lifetime, so not every zombie moves at the exact same speed.
        // 0.3 = each zombie's final speed multiplier is ZombieSpeedMultiplier +/- up to 30%. 0 disables it
        public float ZombieSpeedVariance = 0f;
        public float SalvageDuration = 2f;
        public bool DebugPlayerPosition = false;
        public bool DebugZombies = false;
        // Logs (chat + server log) the position and rotation of every player-placed salvageable
        // barricade, so door coordinates for AvailableDoorsToPurchase can be copied straight from it
        public bool DebugDoors = false;
        // Logs (chat + server log) the position and rotation of every player-placed Barbed Wire,
        // so coordinates for AvailableEletricToPurchase can be copied straight from it
        public bool DebugEletric = false;
        // Logs every item removal and weapon equip step to track disappearing weapons
        public bool DebugItems = false;

        public string ChatIconURL = "https://add-image-url.com";

        public bool KitCommandOnlyInArea = false;
        public List<KitAreas> KitCommandAreas = [];
        public List<Kit> Items = [];

        public List<ConfigWave> Waves = [];
        public List<uint> RemainingZombiesAlert = [];
        public List<WeaponLoadout> AvailableWeaponsToPurchase = [];
        public List<PowerupLoadout> AvailablePowerupsToPurchase = [];
        public List<Door> AvailableDoorsToPurchase = [];
        public List<EletricFence> AvailableEletricToPurchase = [];
        public List<PackAPunchWeapon> AvailablePackAPunch = [];
        public List<ushort> DisabledInventoryIds = [];
        public bool InvulnerableLevelObjects = true;

        public uint StartingCredits = 500;
        public uint HitCredits = 10;
        public uint KillCredits = 50;
        public uint MaxGrenades = 4;
        public float GrenadeDamageMultiplier = 1.0f;
        // If a grenade explosion leaves a NORMAL zombie alive with its health at or below this
        // fraction of its max health, it converts into a Crawler. 0 disables the conversion
        public float GrenadeCrawlerHealthThreshold = 0.5f;

        public void LoadDefaults()
        {
            KitCommandAreas = [
                new() { X1 = 15.5, Y1 = 50, Z1 = -241.5, X2 = 33.1, Y2 = 60, Z2 = -250.05 }
            ];

            Items = [
                new() {
                    name = "Dmitri",
                    items = [
                        new() { Id = 1182, Amount = 1 },
                        new() { Id = 254,  Amount = 1 },
                        new() { Id = 254,  Amount = 1 },
                        new() { Id = 1385, Amount = 1 },
                        new() { Id = 1386, Amount = 1 },
                        new() { Id = 1387, Amount = 1 },
                        new() { Id = 97,   Amount = 1 },
                        new() { Id = 98,   Amount = 7 },
                        new() { Id = 98,   Amount = 7 },
                        new() { Id = 98,   Amount = 7 },
                        new() { Id = 98,   Amount = 7 },
                        new() { Id = 98,   Amount = 7 },
                    ]
                },
                new() {
                    name = "Nikolai",
                    items = [
                        new() { Id = 1182, Amount = 1 },
                        new() { Id = 254,  Amount = 1 },
                        new() { Id = 254,  Amount = 1 },
                        new() { Id = 305,  Amount = 1 },
                        new() { Id = 306,  Amount = 1 },
                        new() { Id = 313,  Amount = 1 },
                        new() { Id = 97,   Amount = 1 },
                        new() { Id = 98,   Amount = 7 },
                        new() { Id = 98,   Amount = 7 },
                        new() { Id = 98,   Amount = 7 },
                        new() { Id = 98,   Amount = 7 },
                        new() { Id = 98,   Amount = 7 },
                    ]
                },
                new() {
                    name = "Aleksandr",
                    items = [
                        new() { Id = 1182, Amount = 1 },
                        new() { Id = 254,  Amount = 1 },
                        new() { Id = 254,  Amount = 1 },
                        new() { Id = 1048, Amount = 1 },
                        new() { Id = 407,  Amount = 1 },
                        new() { Id = 408,  Amount = 1 },
                        new() { Id = 97,   Amount = 1 },
                        new() { Id = 98,   Amount = 7 },
                        new() { Id = 98,   Amount = 7 },
                        new() { Id = 98,   Amount = 7 },
                        new() { Id = 98,   Amount = 7 },
                        new() { Id = 98,   Amount = 7 },
                    ]
                },
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
                    primary = false,
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
                new() {
                    itemId = 1056,
                    powerupType = "sharpshooter",
                    refundValue = 2500
                },
                new() {
                    itemId = 1057,
                    powerupType = "mysterybox",
                    refundValue = 0
                },
                new() {
                    itemId = 1058,
                    powerupType = "grenadier",
                    refundValue = 4000
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

            // Example entry, just to document which fields exist, replace placements with real
            // coordinates from DebugEletric before relying on this in a map. Add more than one
            // placement when a single Barbed Wire doesn't fully block the gap
            AvailableEletricToPurchase = [
                new() {
                    id = 1059,
                    placements = [
                        new() {
                            pos = new(0f, 0f, 0f),
                            rotation = new(0f, 0f, 0f, 1f),
                        },
                    ],
                    assetId = 386, // Barbed Wire
                    zombieDamage = 25f,
                    playerDamage = 50f,
                    refundValue = 500,
                    cooldown = 60f,
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

            DisabledInventoryIds = [
                // Custom Sights
                21,
                22,
                146,
                147,
                148,
                153,
                296,
                302,
                476,
                1004,
                1201,
                1363,
                1442,
                // Default Sights
                5,
                19,
                102,
                110,
                114,
                118,
                124,
                128,
                131,
                134,
                299,
                349,
                358,
                364,
                475,
                486,
                521,
                1001,
                1019,
                1025,
                1028,
                1038,
                1043,
                1367,
                1370,
                1376,
                1378,
                1380,
                1383,
                1448,
                1478,
                1482,
                1486,
                1489,
                // Grips
                8,
                143,
                145,
                // Barrels
                7,
                144,
                149,
                150,
                477,
                1190,
                1191,
                117,
                350,
                354,
                1002,
                1167,
                1338,
                1444,
                // Tacticals
                151,
                152,
                1007,
                1008,
                1438,
                // Barricade
                30
            ];
        }
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
        public float CrawlerHealthMultiplier = 0.5f;
        // Mega/Boss speciality zombies otherwise take damage exactly like a Normal zombie of the
        // same wave, making them die just as fast despite being the "tougher" enemy type
        public float MegaHealthMultiplier = 2.0f;
        public float BossHealthMultiplier = 4.0f;
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
                CrawlerHealthMultiplier = CrawlerHealthMultiplier,
                MegaHealthMultiplier = MegaHealthMultiplier,
                BossHealthMultiplier = BossHealthMultiplier,
                MaxAmmoChance = MaxAmmoChance,
            };
        }
    }

    public class Kit
    {
        public string name = "";
        public string? permissionId = null;
        public List<KitItem> items = [];
        public Skill experience = new();
    }

    public class KitItem
    {
        public ushort Id = 1;
        public byte Amount = 1;
        public byte[]? Metadata = [];
    }

    public class KitAreas
    {
        public double X1;
        public double Y1;
        public double Z1;
        public double X2;
        public double Y2;
        public double Z2;
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
        // Opt-in flag for this weapon to be part of the MysteryBoxSystem random pool, kept right
        // next to the weapon's own definition so the pool never has to duplicate ids in a separate
        // list that could drift out of sync
        public bool canReceiveOnMysteryBox = false;
    }

    public class PowerupLoadout
    {
        public ushort itemId = 0;
        public string powerupType = "juggernog";
        public uint refundValue = 0;
        // Opt-in flag for powerups whose trigger item is itself a real consumable (e.g. a soda
        // bottle) — instead of silently deleting the item, the player is forced to "drink" it
        // through the real engine consume flow, which removes the item on its own once done
        public bool forceDrink = false;
    }

    public class PackAPunchWeapon
    {
        public ushort Id = 1;
        public List<byte[]> AvailableLevelsMetada = [];
        public List<float> AvailableLevelsDamage = [];
    }
}
using System.Collections.Generic;
using Rocket.API;

namespace HordeServer
{
    public class HordeServerConfiguration : IRocketPluginConfiguration
    {
        public uint SpawnTickrate = 600;
        public uint RemainingCheckTickrate = 600;
        public uint TickrateBetweenRounds = 1000;
        public int SecondsAfterRoundFail = 10;
        public ConfigPosition MapCenterPosition = new();
        public int MapCenterRadius = 10000;
        public bool DebugPlayerPosition = false;
        public string PlayersFolder = "SteamLibrary/steamapps/common/U3DS/Servers/myserver/Players/";
        public string LevelName = "PEI";

        public string ChatIconURL = "https://add-image-url.com";

        public List<ZombieNodePosition> ZombiesAvailableNodes = [];
        public List<ConfigPosition> PlayerSpawnPositions = [];
        public List<ConfigWave> Waves = [];
        public List<uint> RemainingZombiesAlert = [];

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
                },
                new() {
                    Normal = 15,
                    Crawler = 3
                },
                new() {
                    Normal = 20,
                    Crawler = 5,
                    Acid = 1
                },
                new() {
                    Normal = 30,
                    Crawler = 10,
                    Acid = 1
                },
                new() {
                    Normal = 40,
                    Crawler = 15,
                    Acid = 2,
                    Burner = 1
                },
                new() {
                    Normal = 60,
                    Crawler = 20,
                    Acid = 3,
                    Burner = 2
                },
                new() {
                    Normal = 90,
                    Crawler = 30,
                    Acid = 4,
                    Burner = 3
                },
                new() {
                    Normal = 100,
                    Crawler = 30,
                    Acid = 4,
                    Burner = 3,
                    Mega = 1
                },
                new() {
                    Normal = 150,
                    Crawler = 40,
                    Acid = 6,
                    Burner = 5,
                    Mega = 2
                },
                new() {
                    BossWind = 1
                },
                new() {
                    Normal = 180,
                    Crawler = 45,
                    Acid = 8,
                    Burner = 15,
                    Mega = 2
                },
                new() {
                    Normal = 160,
                    Crawler = 45,
                    Acid = 28,
                    Burner = 15,
                    Mega = 2
                },
                new() {
                    Normal = 140,
                    Crawler = 45,
                    Acid = 28,
                    Burner = 35,
                    Mega = 2
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 28,
                    Burner = 35,
                    Mega = 4
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 28,
                    Burner = 35,
                    Mega = 4
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 28,
                    Burner = 35,
                    Mega = 4
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 28,
                    Burner = 35,
                    Mega = 4
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 28,
                    Burner = 35,
                    Mega = 4
                },
                new() {
                    Normal = 80,
                    Crawler = 15,
                    Acid = 28,
                    Burner = 85,
                    Mega = 4,
                },
                new() {
                    BossFire = 2,
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 48,
                    Burner = 35,
                    Mega = 4
                },
                new() {
                    Normal = 140,
                    Crawler = 25,
                    Acid = 88,
                    Burner = 35,
                    Mega = 4
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4
                },
                new() {
                    Normal = 100,
                    Crawler = 25,
                    Acid = 100,
                    Burner = 15,
                    Mega = 4
                },
                new() {
                    BossNuclear = 4,
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 20,
                    Mega = 6
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 20,
                    Mega = 6
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 20,
                    Mega = 6
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 20,
                    Mega = 6
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 20,
                    Mega = 6
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 40,
                    Mega = 6
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 60,
                    Mega = 6
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 80,
                    Mega = 6
                },
                new() {
                    Normal = 200,
                    Crawler = 25,
                    Acid = 20,
                    Burner = 100,
                    Mega = 6
                },
                new() {
                    BossMagma = 8
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
    }
}
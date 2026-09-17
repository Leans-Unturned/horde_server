extern alias UnityEngineCoreModule;

using System;
using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;
using Rocket.Unturned.Skills;
using SDG.Unturned;
using UnityCoreModule = UnityEngineCoreModule.UnityEngine;
using SdgItem = SDG.Unturned.Item;

namespace HordeServer
{
    static class KitSystem
    {
        private static readonly Dictionary<string, string> _playerKits = [];
        public static readonly List<string> PlayersInArea = [];
        private static readonly Random _random = new();

        public static void GiveDefaultKit(UnturnedPlayer player)
        {
            Kit kit;
            if (_playerKits.TryGetValue(player.Id, out string? kitName))
                kit = GetKitByName(kitName) ?? GetRandomKit(player);
            else
                kit = GetRandomKit(player);

            ClearInventory(player);
            SetSkills(player, kit.experience);
            GiveItems(player, kit);
        }

        public static void GiveItems(UnturnedPlayer player, Kit kit)
        {
            ItemSystem.kitGiveInProgress.Add(player);
            try
            {
                foreach (KitItem item in kit.items)
                {
                    SdgItem spawnItem = new(item.Id, true) { amount = item.Amount };
                    if (item.Metadata != null && item.Metadata.Length > 0)
                    {
                        spawnItem.state = item.Metadata;
                        spawnItem.metadata = item.Metadata;
                    }
                    player.Inventory.tryAddItemAuto(spawnItem, true, false, true, false);
                }
            }
            finally
            {
                ItemSystem.kitGiveInProgress.Remove(player);
            }
        }

        public static void ClearInventory(UnturnedPlayer player)
        {
            for (byte page = 0; page < PlayerInventory.PAGES; page++)
            {
                try
                {
                    while (player.Inventory.getItemCount(page) > 0)
                        player.Inventory.removeItem(page, 0);
                }
                catch (Exception) { }
            }
            player.Inventory.player.clothing.updateClothes(0, 0, [], 0, 0, [], 0, 0, [], 0, 0, [], 0, 0, [], 0, 0, [], 0, 0, []);
        }

        public static void SetSkills(UnturnedPlayer player, Skill experience)
        {
            player.Experience = 0;
            player.SetSkillLevel(UnturnedSkill.Agriculture, experience.Agriculture);
            player.SetSkillLevel(UnturnedSkill.Cardio, experience.Cardio);
            player.SetSkillLevel(UnturnedSkill.Cooking, experience.Cooking);
            player.SetSkillLevel(UnturnedSkill.Crafting, experience.Crafting);
            player.SetSkillLevel(UnturnedSkill.Dexerity, experience.Dexerity);
            player.SetSkillLevel(UnturnedSkill.Diving, experience.Diving);
            player.SetSkillLevel(UnturnedSkill.Engineer, experience.Engineer);
            player.SetSkillLevel(UnturnedSkill.Exercise, experience.Exercise);
            player.SetSkillLevel(UnturnedSkill.Fishing, experience.Fishing);
            player.SetSkillLevel(UnturnedSkill.Healing, experience.Healing);
            player.SetSkillLevel(UnturnedSkill.Immunity, experience.Immunity);
            player.SetSkillLevel(UnturnedSkill.Mechanic, experience.Mechanic);
            player.SetSkillLevel(UnturnedSkill.Outdoors, experience.Outdoors);
            player.SetSkillLevel(UnturnedSkill.Overkill, experience.Overkill);
            player.SetSkillLevel(UnturnedSkill.Parkour, experience.Parkour);
            player.SetSkillLevel(UnturnedSkill.Sharpshooter, experience.Sharpshooter);
            player.SetSkillLevel(UnturnedSkill.Sneakybeaky, experience.Sneakybeaky);
            player.SetSkillLevel(UnturnedSkill.Strength, experience.Strength);
            player.SetSkillLevel(UnturnedSkill.Survival, experience.Survival);
            player.SetSkillLevel(UnturnedSkill.Toughness, experience.Toughness);
            player.SetSkillLevel(UnturnedSkill.Vitality, experience.Vitality);
            player.SetSkillLevel(UnturnedSkill.Warmblooded, experience.Warmblooded);
        }

        public static Kit GetRandomKit(UnturnedPlayer player)
        {
            var kits = HordeServerPlugin.instance!.Configuration.Instance.Items;
            if (kits.Count == 0) return new Kit();
            for (int i = 0; i < 10; i++)
            {
                int idx = _random.Next(0, kits.Count);
                if (player.HasPermission($"hordekit.{kits[idx].name}"))
                    return kits[idx];
            }
            return kits[0];
        }

        public static Kit? GetKitByName(string name)
        {
            foreach (Kit kit in HordeServerPlugin.instance!.Configuration.Instance.Items)
                if (kit.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return kit;
            return null;
        }

        public static void SetPlayerKit(string playerId, string kitName) => _playerKits[playerId] = kitName;

        public static void Disconnect(string playerId)
        {
            _playerKits.Remove(playerId);
            PlayersInArea.Remove(playerId);
        }

        public static void PositionUpdate(UnturnedPlayer player, UnityCoreModule.Vector3 position)
        {
            var areas = HordeServerPlugin.instance!.Configuration.Instance.KitCommandAreas;
            foreach (KitAreas area in areas)
            {
                double minX = Math.Min(area.X1, area.X2), maxX = Math.Max(area.X1, area.X2);
                double minY = Math.Min(area.Y1, area.Y2), maxY = Math.Max(area.Y1, area.Y2);
                double minZ = Math.Min(area.Z1, area.Z2), maxZ = Math.Max(area.Z1, area.Z2);

                if (position.x < minX || position.x > maxX ||
                    position.y < minY || position.y > maxY ||
                    position.z < minZ || position.z > maxZ)
                {
                    PlayersInArea.Remove(player.Id);
                    return;
                }
            }
            if (!PlayersInArea.Contains(player.Id))
                PlayersInArea.Add(player.Id);
        }
    }
}

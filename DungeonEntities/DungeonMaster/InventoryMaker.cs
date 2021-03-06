﻿using DungeonEntities.Dungeon;

namespace DungeonEntities.DungeonMaster
{
    public class InventoryMaker
    {
        private static readonly string[] Weapons = new string[]
        {
            "Sword",
            "Broadsword",
            "Longsword",
            "Shortsword",
            "Axe",
            "Spear",
            "Club",
            "Mace",
            "Morning star",
            "Pike",
            "Scythe"
        };

        private static readonly string[] Types = new string[]
        {
            "Magic",
            "Sparkling",
            "Glowing",
            "Stout",
            "Hefty",
            "Cracked",
            "Worn",
            "Bloodied",
            "Immaculate"
        };

        private static readonly string[] Treasures = new string[]
        {
            "Diamond",
            "Commodore 64",
            "Fortune Cookie wrapper",
            "Ultima Series boxed set",
            "Zork trilogy set",
            "Nokia Lumia 1020",
            "300 baud modem",
            "Dot-matrix printer"
        };

        public Inventory[] MakeNewInventory()
        {
            var weapon = new Inventory
            {
                IsTreasure = false,
                Name = $"{Types.PickRandom()} {Weapons.PickRandom()}"
            };
            var treasure = new Inventory
            {
                IsTreasure = true,
                Name = $"{Types.PickRandom()} {Treasures.PickRandom()}"
            };
            return new[] { weapon, treasure };
        }
    }
}

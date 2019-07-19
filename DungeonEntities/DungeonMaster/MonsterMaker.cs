using DungeonEntities.Dungeon;

namespace DungeonEntities.DungeonMaster
{
    public class MonsterMaker
    {
        private readonly string[] Colors = new string[] {
            "Red", "Orange", "Yellow", "Green", "Blue", "Indigo", "Violet" };

        private readonly string[] Adjectives = new string[]
        {
            "Gibbering",
            "Twitching",
            "Slobbering",
            "Growling",
            "Shuffling",
            "Moaning",
            "Slavoring",
            "Lumbering",
            "Gigantic",
            "Colossal"
        };

        private readonly string[] Beasts = new string[]
        {
            "Basilisk",
            "Centaur",
            "Cyclops",
            "Dragon",
            "Gorgon",
            "Griffon",
            "Harpy",
            "Manticore",
            "Minotaur",
            "Troll",
            "Zombie"
        };

        public Monster GetNewMonster()
        {
            var monster = new Monster
            {
                IsAlive = true,
                Name = $"{Adjectives.PickRandom()} {Colors.PickRandom()} {Beasts.PickRandom()}"
            };
            return monster;
        }
    }
}

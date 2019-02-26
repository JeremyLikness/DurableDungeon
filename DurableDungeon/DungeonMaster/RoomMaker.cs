using DurableDungeon.Dungeon;

namespace DurableDungeon.DungeonMaster
{
    public class RoomMaker
    {
        private static readonly string[] RoomTypes = new string[]
        {
            "rusty",
            "dusty",
            "moldy",
            "damp",
            "dark",
            "well-lit",
            "small",
            "large",
            "cramped",
            "spacious",
            "clean",
            "comfortable",
            "smelly",
            "warm",
            "hot",
            "cold"
        };

        private static readonly string[] Walls = new string[]
        {
            "dark stone walls",
            "rough, rocky walls",
            "soft walls of packed dirt",
            "crumbling brick walls",
            "walls made of bright red bricks",
            "smooth black walls with a glassy texture",
            "stone walls filled with exposed fossils",
            "walls made of large fitted concrete blocks",
            "sandstone walls",
            "rocky walls with faint bands of color",
            "rotting wallpaper with an unrecognizeable pattern"
        };

        private static readonly string[] Contents = new string[]
        {
            "There is a crude table in the middle of the room.",
            "In the center of the room stands a dry fountain.",
            "There is broken pottery scattered across the floor.",
            "In the middle of the room is a large pile of rust.",
            "A rotting cot is tucked into the corner.",
            "In the center of the floor is a small drain.",
            "The room is encircled by flickering torches in sconces.",
            "The floor is covered with moldy hay.",
            "In the center of the room is a deep, dark well with no chain or bucket.",
            "There are no other features of note.",
            "The room is otherwise empty.",
            "You notice nothing else interesting about the room."
        };

        public Room GetNewRoom(string username)
        {
            var room = new Room();
            var type = RoomTypes.PickRandom();
            room.Name = $"A {type} room";
            room.Description = $"You are standing in a {type} room with {Walls.PickRandom()}. {Contents.PickRandom()}.";
            room.User = username;
            return room;
        }
    }
}

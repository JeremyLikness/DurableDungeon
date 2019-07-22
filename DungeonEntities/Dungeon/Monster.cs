using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace DungeonEntities.Dungeon
{
    public class Monster : BaseHasInventory, IMonsterOperations
    {
        public string Name { get; set; }
        public bool IsAlive { get; set; }

        public string CurrentRoom { get; set; }

        public void New(string name)
        {
            Name = name;
            IsAlive = true;
        }

        public void SetRoom(string room)
        {
            CurrentRoom = room;
        }

        public void AddInventory(string name)
        {
            RestoreLists();
            InventoryList.Add(name);
            SaveLists();
        }

        public void Kill()
        {
            IsAlive = false;
            InventoryList.Clear();
            InventoryItems = string.Empty;
        }

        [FunctionName(nameof(Monster))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<Monster>();
    }
}

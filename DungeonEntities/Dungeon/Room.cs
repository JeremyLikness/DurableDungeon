using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace DungeonEntities.Dungeon
{
    public class Room : BaseHasInventory, IRoomOperations
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Monster { get; set; }

        public void New(string name)
        {
            Name = name;            
        }

        public void SetDescription(string description)
        {
            Description = description;
        }

        public void SetMonster(string monster)
        {
            Monster = monster;
        }

        public void AddInventory(string inventory)
        {
            RestoreLists();
            InventoryList.Add(inventory);
            SaveLists();
        }

        public void RemoveInventory(string inventory)
        {
            RestoreLists();
            InventoryList.Remove(inventory);
            SaveLists();
        }

        [FunctionName(nameof(Room))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<Room>();
    }
}

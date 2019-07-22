using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace DungeonEntities.Dungeon
{
    public class User : BaseHasInventory, IUserOperations
    {
        public string Name { get; set; }
        public string CurrentRoom { get; set; }
        public bool IsAlive { get; set; }

        public void New(string user)
        {
            Name = user;
            IsAlive = true;
        }

        public void Kill()
        {
            IsAlive = false;
        }

        public void SetRoom(string room)
        {
            CurrentRoom = room;
        }

        public void AddInventory(string inventory)
        {
            RestoreLists();
            InventoryList.Add(inventory);
            SaveLists();
        }

        [FunctionName(nameof(User))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<User>();
    }
}

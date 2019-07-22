using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace DungeonEntities.Dungeon
{
    public class Inventory: IInventoryOperations
    {
        public string Name { get; set; }
        public string Monster { get; set; }
        public string Room { get; set; }
        public bool IsTreasure { get; set; }

        [JsonIgnore]
        public bool UserHasIt
        {
            get
            {
                return string.IsNullOrWhiteSpace(Monster) &&
                    string.IsNullOrWhiteSpace(Room);
            }
        }

        public void New(string name)
        {
            Name = name;
            IsTreasure = false;
        }

        public void SetTreasure()
        {
            IsTreasure = true;
        }

        public void SetMonster(string monster)
        {
            Room = string.Empty;
            Monster = monster;
        }

        public void SetRoom(string room)
        {
            Room = room;
            Monster = string.Empty;
        }

        public void SetUser()
        {
            Room = string.Empty;
            Monster = string.Empty;
        }

        [FunctionName(nameof(Inventory))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<Inventory>();
    }
}

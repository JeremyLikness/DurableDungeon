using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;

namespace DungeonEntities.Dungeon
{
    public class Inventory
    {
        public string Name { get; set; }
        public string Monster { get; set; }
        public string Room { get; set; }
        public bool IsTreasure { get; set; }

        [IgnoreProperty]
        public bool UserHasIt
        {
            get
            {
                return string.IsNullOrWhiteSpace(Monster) &&
                    string.IsNullOrWhiteSpace(Room);
            }
        }

        [FunctionName(nameof(Inventory))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<Inventory>();
    }
}

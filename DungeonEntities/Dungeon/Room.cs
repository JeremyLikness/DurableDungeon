using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace DungeonEntities.Dungeon
{
    public class Room : BaseHasInventory
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Monster { get; set; }

        [FunctionName(nameof(Room))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<Room>();
    }
}

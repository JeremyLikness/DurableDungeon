using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace DungeonEntities.Dungeon
{
    public class Monster : BaseHasInventory
    {
        public string Name { get; set; }
        public bool IsAlive { get; set; }

        public string CurrentRoom { get; set; }

        [FunctionName(nameof(Monster))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<Monster>();
    }
}

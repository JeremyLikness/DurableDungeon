using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace DungeonEntities.Dungeon
{
    public class InventoryList : BaseHasInventory
    {
        [FunctionName(nameof(InventoryList))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<InventoryList>();
    }
}

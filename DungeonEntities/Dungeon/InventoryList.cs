using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using System.Linq;

namespace DungeonEntities.Dungeon
{
    public class InventoryList : BaseHasInventory, IInventoryListOperations
    {
        public void New(Inventory[] inventory)
        {
            InventoryList = (from i in inventory select i.Name).ToList();
            SaveLists();
        }

        [FunctionName(nameof(InventoryList))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<InventoryList>();
    }
}

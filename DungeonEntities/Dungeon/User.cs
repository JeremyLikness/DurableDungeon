using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace DungeonEntities.Dungeon
{
    public class User : BaseHasInventory
    {
        public string Name { get; set; }
        public string CurrentRoom { get; set; }
        public bool IsAlive { get; set; }

        public void New(string user)
        {
            Name = user;
            IsAlive = true;
        }

        [FunctionName(nameof(User))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<User>();
    }
}

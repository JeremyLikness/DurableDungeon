using DurableDungeon.Dungeon;
using DurableDungeon.DungeonMaster;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;
using System.Threading.Tasks;

namespace DurableDungeon.Functions
{
    public static class NewUserSequentialFunctions
    {
        [FunctionName(nameof(RunUserSequentialWorkflow))]
        public static async Task RunUserSequentialWorkflow(
            [OrchestrationTrigger]IDurableOrchestrationContext context,
            ILogger logger)
        {
            var username = context.GetInput<string>();
            logger.LogInformation("Start of user sequential workflow for {user}", username);

            await context.CallActivityAsync(nameof(PlaceUserInRoom), username);
            await context.CallActivityAsync(nameof(PlaceInventoryInRoom), username);
            await context.CallActivityAsync(nameof(PlaceMonsterInRoom), username);
            await context.CallActivityAsync(nameof(PlaceInventoryOnMonster), username);
            await context.CallActivityAsync(nameof(Global.StartNewWorkflow),
                (nameof(MonitorFunctions.GameMonitorWorkflow), username));

            logger.LogInformation("End of RunUserSequentialWorkflow.");
        }

        [FunctionName(nameof(PlaceUserInRoom))]
        public static async Task PlaceUserInRoom(
            [ActivityTrigger]string username,
            [Table(nameof(User))]CloudTable userTable,
            [Table(nameof(Room))]CloudTable roomTable,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Placing user in room for {user}", username);
            var userClient = userTable.AsClientFor<User>();
            var userKey = new User { Name = username };
            var user = await userClient.GetAsync(userKey.PartitionKey, userKey.RowKey);
            var roomClient = roomTable.AsClientFor<Room>();
            var room = (await roomClient.GetAllAsync(username)).First();
            logger.LogInformation("Found room {room} for user {user}",
                room.Name,
                username);
            room.User = user.Name;
            user.CurrentRoom = room.Name;
            await userClient.ReplaceAsync(user);
            await roomClient.ReplaceAsync(room);
            logger.LogInformation("Placed user {user} in room {room}.",
                username,
                room.Name);
            await console.AddAsync($"{username} looks around: {room.Description}");
        }

        [FunctionName(nameof(PlaceMonsterInRoom))]
        public static async Task PlaceMonsterInRoom(
            [ActivityTrigger]string username,
            [Table(nameof(Monster))]CloudTable monsterTable,
            [Table(nameof(Room))]CloudTable roomTable,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Placing monster in room for {user}", username);
            var monsterClient = monsterTable.AsClientFor<Monster>();
            var monster = (await monsterClient.GetAllAsync(username)).First();
            var roomClient = roomTable.AsClientFor<Room>();
            var room = (await roomClient.GetAllAsync(username)).First();
            logger.LogInformation("Found monster {monster} to place in room {room} for user {user}",
                monster.Name,
                room.Name,
                username);
            room.Monster = monster.Name;
            monster.CurrentRoom = room.Name;
            await monsterClient.ReplaceAsync(monster);
            await roomClient.ReplaceAsync(room);
            logger.LogInformation("Placing monster {monster} in room {room} for user {user} successful.",               
                monster.Name,
                room.Name,
                username);
            await console.AddAsync($"{username} notices a {monster.Name} in {room.Name}.");
        }

        [FunctionName(nameof(PlaceInventoryOnMonster))]
        public static async Task PlaceInventoryOnMonster(
            [ActivityTrigger]string username,
            [Table(nameof(Inventory))]CloudTable inventoryTable,
            [Table(nameof(Monster))]CloudTable monsterTable,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Placing inventory on monster for {user}", username);
            var inventoryClient = inventoryTable.AsClientFor<Inventory>();
            var inventoryList = await inventoryClient.GetAllAsync(username);
            var inventory = inventoryList.Where(i => i.IsTreasure).First();
            var monsterClient = monsterTable.AsClientFor<Monster>();
            var monster = (await monsterClient.GetAllAsync(username)).First();
            logger.LogInformation("Found treasure {inventory} for monster {monster} and user {user}", 
                inventory.Name,
                monster.Name,
                username);
            inventory.Monster = monster.Name;
            monster.InventoryList.Add(inventory.Name);
            await monsterClient.ReplaceAsync(monster);
            await inventoryClient.ReplaceAsync(inventory);
            logger.LogInformation("Placing treasure {inventory} on monter {monster} for user {user} successful.",
                inventory.Name,
                monster.Name,
                username);
            await console.AddAsync($"{username} notices a {monster.Name} with a {inventory.Name}.");
        }

        [FunctionName(nameof(PlaceInventoryInRoom))]
        public static async Task PlaceInventoryInRoom(
            [ActivityTrigger]string username,
            [Table(nameof(Inventory))]CloudTable inventoryTable,
            [Table(nameof(Room))]CloudTable roomTable,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Placing inventory in room for {user}", username);
            var inventoryClient = inventoryTable.AsClientFor<Inventory>();
            var inventoryList = await inventoryClient.GetAllAsync(username);
            var inventory = inventoryList.Where(i => !i.IsTreasure).First();
            var roomClient = roomTable.AsClientFor<Room>();
            var room = (await roomClient.GetAllAsync(username)).First();
            logger.LogInformation("Found weapon {inventory} and room {room} for user {user}", 
                inventory.Name,
                room.Name,
                username);
            inventory.Room = room.Name;
            room.InventoryList.Add(inventory.Name);
            await roomClient.ReplaceAsync(room);
            await inventoryClient.ReplaceAsync(inventory);
            logger.LogInformation("Place treasure for user {user} successful.");
            await console.AddAsync($"{username} sees a {inventory.Name} inside the room!");
        }
    }
}

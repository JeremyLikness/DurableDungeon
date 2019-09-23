using DungeonEntities.Dungeon;
using DungeonEntities.DungeonMaster;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace DungeonEntities.Functions
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
            [DurableClient]IDurableClient client,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Placing user in room for {user}", username);

            var room = await username.GetEntityForUserOrThrow<Room>(client);

            logger.LogInformation("Found room {room} for user {username}",
                room.Name,
                username);

            await client.SignalEntityAsync<IUserOperations>(
                username.AsEntityIdFor<User>(),
                operation => operation.SetRoom(room.Name));

            logger.LogInformation("Placed user {user} in room {room}.",
                username,
                room.Name);

            await console.AddAsync($"{username} looks around: {room.Description}");
        }

        [FunctionName(nameof(PlaceMonsterInRoom))]
        public static async Task PlaceMonsterInRoom(
            [ActivityTrigger]string username,
            [DurableClient]IDurableClient client,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Placing monster in room for {user}", username);

            var room = await username.GetEntityForUserOrThrow<Room>(client);
            var monster = await username.GetEntityForUserOrThrow<Monster>(client);

            logger.LogInformation("Found monster {monster} to place in room {room} for user {user}",
                monster.Name,
                room.Name,
                username);

            await client.SignalEntityAsync<IMonsterOperations>(
                username.AsEntityIdFor<Monster>(),
                operation => operation.SetRoom(room.Name));
            
            await client.SignalEntityAsync<IRoomOperations>(
                username.AsEntityIdFor<Room>(),
                operation => operation.SetMonster(monster.Name));

            logger.LogInformation("Placing monster {monster} in room {room} for user {user} successful.",               
                monster.Name,
                room.Name,
                username);
            await console.AddAsync($"{username} notices a {monster.Name} in {room.Name}.");
        }

        [FunctionName(nameof(PlaceInventoryOnMonster))]
        public static async Task PlaceInventoryOnMonster(
            [ActivityTrigger]string username,
            [DurableClient]IDurableClient client,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Placing inventory on monster for {user}", username);

            var inventoryNames = await username.GetEntityForUserOrThrow<InventoryList>(client);
            var inventoryList = await inventoryNames.DeserializeListForUserWithClient(username, client);
            var inventory = inventoryList
                .Where(i => i.IsTreasure)
                .Select(i => i).First();
            var monster = await username.GetEntityForUserOrThrow<Monster>(client);

            logger.LogInformation("Found treasure {inventory} for monster {monster} and user {user}", 
                inventory.Name,
                monster.Name,
                username);

            await client.SignalEntityAsync<IInventoryOperations>(
                username.AsEntityIdFor<Inventory>(inventory.Name),
                operation => operation.SetMonster(monster.Name));

            await client.SignalEntityAsync<IMonsterOperations>(
                username.AsEntityIdFor<Monster>(),
                operation => operation.AddInventory(inventory.Name));

            logger.LogInformation("Placing treasure {inventory} on monster {monster} for user {user} successful.",
                inventory.Name,
                monster.Name,
                username);
            await console.AddAsync($"{username} notices a {monster.Name} with a {inventory.Name}.");
        }

        [FunctionName(nameof(PlaceInventoryInRoom))]
        public static async Task PlaceInventoryInRoom(
            [ActivityTrigger]string username,
            [DurableClient]IDurableClient client,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Placing inventory in room for {user}", username);

            var inventoryNames = await username.GetEntityForUserOrThrow<InventoryList>(client);
            var inventoryList = await inventoryNames.DeserializeListForUserWithClient(username, client);
            var inventory = inventoryList
                .Where(i => !i.IsTreasure)
                .Select(i => i).First();
            var room = await username.GetEntityForUserOrThrow<Room>(client);

            logger.LogInformation("Found weapon {inventory} and room {room} for user {user}", 
                inventory.Name,
                room.Name,
                username);

            await client.SignalEntityAsync<IInventoryOperations>(
                username.AsEntityIdFor<Inventory>(inventory.Name),
                operation => operation.SetRoom(room.Name));

            await client.SignalEntityAsync<IRoomOperations>(
                username.AsEntityIdFor<Room>(),
                operation => operation.AddInventory(inventory.Name));

            logger.LogInformation("Place treasure for user {user} successful.");
            await console.AddAsync($"{username} sees a {inventory.Name} inside the room!");
        }
    }
}

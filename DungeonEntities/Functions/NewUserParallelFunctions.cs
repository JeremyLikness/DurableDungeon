using DungeonEntities.Dungeon;
using DungeonEntities.DungeonMaster;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DungeonEntities.Functions
{
    public static class NewUserParallelFunctions
    {
        private static readonly MonsterMaker _monsterMaker =
            new MonsterMaker();

        private static readonly RoomMaker _roomMaker =
            new RoomMaker();

        private static readonly InventoryMaker _inventoryMaker =
            new InventoryMaker();

        [FunctionName(nameof(RunUserParallelWorkflow))]
        public static async Task RunUserParallelWorkflow(
            [OrchestrationTrigger]IDurableOrchestrationContext context,
            ILogger logger)
        {
            var username = context.GetInput<string>();
            logger.LogInformation("Start of user parallel workflow for {user}", username);

            var parallelTasks = new List<Task>
            {
                context.CallActivityAsync(nameof(CreateMonster), username),
                context.CallActivityAsync(nameof(CreateRoom), username),
                context.CallActivityAsync(nameof(CreateInventory), username)
            };

            await Task.WhenAll(parallelTasks);

            logger.LogInformation("Starting sub-orchestration {workflow} for {user}", 
                nameof(ConfirmationFunctions.UserConfirmationWorkflow), username);
            await context.CallSubOrchestratorAsync(nameof(ConfirmationFunctions.UserConfirmationWorkflow), username);
            logger.LogInformation("End of RunUserParallelWorkflow.");
        }

        [FunctionName(nameof(CreateMonster))]
        public static async Task CreateMonster(
            [ActivityTrigger]string username,
            [DurableClient]IDurableClient client,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Create monster for user {user}", username);
            var monster = _monsterMaker.GetNewMonster();
            var id = username.AsEntityIdFor<Monster>();
            await client.SignalEntityAsync<IMonsterOperations>(id, operation => operation.New(monster.Name));
            await console.AddAsync($"Look out! {monster.Name} is now stalking {username}!");
            logger.LogInformation("Created monster {monster} for user {user} successful", monster.Name, username);
        }

        [FunctionName(nameof(CreateRoom))]
        public static async Task CreateRoom(
            [ActivityTrigger]string username,
            [DurableClient]IDurableClient client,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Create room for user {user}", username);
            var room = _roomMaker.GetNewRoom();
            var id = username.AsEntityIdFor<Room>();
            await client.SignalEntityAsync<IRoomOperations>(id, operation => operation.New(room.Name));
            await client.SignalEntityAsync<IRoomOperations>(id, operation => operation.SetDescription(room.Description));
            await console.AddAsync($"{room.Name} has been prepared for {username}!");
            logger.LogInformation("Creation of {room} for user {user} successful", room.Name, username);
        }

        [FunctionName(nameof(CreateInventory))]
        public static async Task CreateInventory(
            [ActivityTrigger]string username,
            [DurableClient]IDurableClient client,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Create inventory for user {user}", username);
            var inventoryList = _inventoryMaker.MakeNewInventory();
            var id = username.AsEntityIdFor<InventoryList>();
            await client.SignalEntityAsync<IInventoryListOperations>(id, 
                operation => operation.New(inventoryList));
            foreach(var item in inventoryList)
            {
                id = username.AsEntityIdFor<Inventory>(item.Name);
                await client.SignalEntityAsync<IInventoryOperations>(id, 
                    operation => operation.New(item.Name));
                if (item.IsTreasure)
                {
                    await client.SignalEntityAsync<IInventoryOperations>(id, operation => operation.SetTreasure());
                }
            }
            await console.AddAsync($"A {inventoryList[0].Name} and {inventoryList[1].Name} have been added for user {username}!");
            logger.LogInformation("Create {item} and {secondItem} for {user} successful",
                inventoryList[0].Name,
                inventoryList[1].Name,
                username);
        }
    }
}

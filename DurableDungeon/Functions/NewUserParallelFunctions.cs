using DurableDungeon.Dungeon;
using DurableDungeon.DungeonMaster;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DurableDungeon.Functions
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
                context.CallActivityAsync(nameof(CreateUser), username),
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

        [FunctionName(nameof(CreateUser))]
        public static async Task CreateUser(
            [ActivityTrigger]string username,
            [Table(nameof(User))]CloudTable table,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Create user: {user}", username);
            var client = table.AsClientFor<User>();
            var user = new User { Name = username, IsAlive = true };
            await client.InsertAsync(user);
            await console.AddAsync($"Successfully created user {username}");
            logger.LogInformation("Create user {user} successful", username);
        }

        [FunctionName(nameof(CreateMonster))]
        public static async Task CreateMonster(
            [ActivityTrigger]string username,
            [Table(nameof(Monster))]CloudTable table,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Create monster for user {user}", username);
            var client = table.AsClientFor<Monster>();
            var monster = _monsterMaker.GetNewMonster(username);
            await client.InsertAsync(monster);
            await console.AddAsync($"Look out! {monster.Name} is now stalking {username}!");
            logger.LogInformation("Created monster {monster} for user {user} successful", monster.Name, username);
        }

        [FunctionName(nameof(CreateRoom))]
        public static async Task CreateRoom(
            [ActivityTrigger]string username,
            [Table(nameof(Room))]CloudTable table,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Create room for user {user}", username);
            var client = table.AsClientFor<Room>();
            var room = _roomMaker.GetNewRoom(username);
            await client.InsertAsync(room);
            await console.AddAsync($"{room.Name} has been prepared for {username}!");
            logger.LogInformation("Creation of {room} for user {user} successful", room.Name, username);
        }

        [FunctionName(nameof(CreateInventory))]
        public static async Task CreateInventory(
            [ActivityTrigger]string username,
            [Table(nameof(Inventory))]CloudTable table,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Create inventory for user {user}", username);
            var client = table.AsClientFor<Inventory>();
            var inventory = _inventoryMaker.MakeNewInventory(username);
            await client.InsertManyAsync(inventory);
            await console.AddAsync($"A {inventory[0].Name} and {inventory[1].Name} have been added for user {username}!");
            logger.LogInformation("Create {item} and {secondItem} for {user} successful",
                inventory[0].Name,
                inventory[1].Name,
                username);
        }
    }
}

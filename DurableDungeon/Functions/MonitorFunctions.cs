using DurableDungeon.Dungeon;
using DurableDungeon.DungeonMaster;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DurableDungeon.Functions
{
    public static class MonitorFunctions
    {
        public const int ExpiryMinutes = 60;
        public const int CheckIntervalSeconds = 20;
        public const string KILLMONSTER = "KillMonster";
        public const string GOTTREASURE = "GotTreasure";

        [FunctionName(nameof(UserMonitorWorkflow))]
        public static async Task UserMonitorWorkflow(
            [OrchestrationTrigger]IDurableOrchestrationContext context,
            ILogger logger)
        {
            var username = context.GetInput<string>();
            logger.LogInformation("Start of user monitor workflow for {user}", username);

            var expiryTime = context.CurrentUtcDateTime.AddHours(Global.MonitorTimeoutHours);

            await context.CallActivityAsync(nameof(ConsoleFunctions.AddToQueue),
                        $"{username} is being monitored every {CheckIntervalSeconds} seconds.");

            var timeout = false;
            while (context.CurrentUtcDateTime < expiryTime)
            {
                timeout = false;
                var done = await context.CallActivityAsync<bool>(nameof(MonitorUser), username);

                if (done)
                {
                    break;
                }

                var nextCheck = context.CurrentUtcDateTime.AddSeconds(CheckIntervalSeconds);
                await context.CreateTimer(nextCheck, CancellationToken.None);
                timeout = true;
            }
            if (timeout)
            {
                await context.CallActivityAsync(nameof(ConsoleFunctions.AddToQueue),
                    $"{username} monitoring has timed out.");
            }
            await context.CallActivityAsync(nameof(ConsoleFunctions.AddToQueue),
                    $"{username} monitoring is done.");
        }

        [FunctionName(nameof(MonitorUser))]
        public static async Task<bool> MonitorUser(
           [ActivityTrigger]string username,
           [Table(nameof(User))]CloudTable userTable,
           [Table(nameof(Inventory))]CloudTable inventoryTable,
           [Queue(Global.QUEUE)]IAsyncCollector<string> console,
           ILogger logger)
        {
            var temp = new User { Name = username };
            var userClient = userTable.AsClientFor<User>();
            var user = await userClient.GetAsync(temp.PartitionKey, temp.RowKey);
            if (user == null)
            {
                throw new Exception($"User {username} not found!");
            }

            var inventoryClient = inventoryTable.AsClientFor<Inventory>();
            var inventoryList = await inventoryClient.GetAllAsync(username);
            var treasure = inventoryList.FirstOrDefault(i => i.IsTreasure);

            if (treasure == null)
            {
                logger.LogInformation("No treasure found for user {user}", username);
                return false;
            }

            if (user.IsAlive)
            {
                logger.LogInformation("User {user} is alive!", username);
                if (user.InventoryList.Any(i => i == treasure.Name))
                {
                    logger.LogInformation("User {user} has the treasure!!!", username);
                    await console.AddAsync($"{username} has the treasure.");
                    return true;
                }
                else
                {
                    logger.LogInformation("User {user} does not have the treasure yet.", username);
                    await console.AddAsync($"{username} is alive but has not found the treasure.");
                    return false;
                }
            }
            else
            {
                logger.LogInformation("User {user} has died.", username);
                await console.AddAsync($"{username} has died.");
                return true;
            }
        }

        [FunctionName(nameof(GameMonitorWorkflow))]
        public static async Task GameMonitorWorkflow(
            [OrchestrationTrigger]IDurableOrchestrationContext context,
            ILogger logger)
        {
            var username = context.GetInput<string>();
            logger.LogInformation("Start of game monitor workflow for {user}", username);

            var monsterKilled = context.WaitForExternalEvent(KILLMONSTER);
            var treasureFound = context.WaitForExternalEvent(GOTTREASURE);

            await Task.WhenAll(monsterKilled, treasureFound);

            await context.CallActivityAsync(nameof(ConsoleFunctions.AddToQueue),
                $"{username} has won the game!");
            logger.LogInformation("User {user} won the game.", username);
        }
    }
}
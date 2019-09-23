using DungeonEntities.Dungeon;
using DungeonEntities.DungeonMaster;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonEntities.Functions
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
           [DurableClient]IDurableClient client,
           [Queue(Global.QUEUE)]IAsyncCollector<string> console,
           ILogger logger)
        {
            var user = await client.ReadUserEntityAsync<User>(username);
            if (!user.EntityExists)
            {
                throw new Exception($"User {username} not found!");
            }

            user.EntityState.RestoreLists();

            var inventoryNames = await client.ReadUserEntityAsync<InventoryList>(username);
            if (!inventoryNames.EntityExists)
            {
                logger.LogInformation("No inventory found for user {user}", username);
                return false;
            }

            var inventoryList = await inventoryNames.EntityState.DeserializeListForUserWithClient(username, client);

            var treasure = inventoryList.FirstOrDefault(i => i.IsTreasure);

            if (treasure == null)
            {
                logger.LogInformation("No treasure found for user {user}", username);
                return false;
            }

            if (user.EntityState.IsAlive)
            {
                logger.LogInformation("User {user} is alive!", username);
                if (user.EntityState.InventoryList != null
                    && user.EntityState.InventoryList.Any(i => i == treasure.Name))
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
            [DurableClient]IDurableClient client,
            ILogger logger)
        {
            var username = context.GetInput<string>();
            logger.LogInformation("Start of game monitor workflow for {user}", username);

            var monsterKilled = context.WaitForExternalEvent(KILLMONSTER);
            var treasureFound = context.WaitForExternalEvent(GOTTREASURE);

            await Task.WhenAll(monsterKilled, treasureFound);

            await client.SignalEntityAsync(
                UserCounter.Id,
                UserCounter.UserDone);

            await context.CallActivityAsync(nameof(ConsoleFunctions.AddToQueue),
                $"{username} has won the game!");
            logger.LogInformation("User {user} won the game.", username);
        }
    }
}
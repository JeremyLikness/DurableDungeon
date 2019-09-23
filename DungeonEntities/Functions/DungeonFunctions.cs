using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DungeonEntities.Dungeon;
using DungeonEntities.DungeonMaster;
using System;
using DungeonEntities.Functions;

namespace DurableDungeon.Functions
{
    public static class DungeonFunctions
    {
        [FunctionName(nameof(NewUser))]
        public static async Task<IActionResult> NewUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
                HttpRequest req,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            [DurableClient]IDurableClient starter,
            ILogger log)
        {
            log.LogInformation("NewUser called.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string name = data?.name;

            if (string.IsNullOrWhiteSpace(name))
            {
                await console.AddAsync("An attempt to create a user with no name was made.");
                return new BadRequestObjectResult("User name is required.");
            }

            var userCheck = await starter.ReadUserEntityAsync<User>(name);
            if (userCheck.EntityExists)
            {
                await console.AddAsync($"Attempt to add duplicate user {name} failed.");
                return new BadRequestObjectResult("Duplicate username is not allowed.");
            }

            // create the user here
            var id = name.AsEntityIdFor<User>();
            await starter.SignalEntityAsync<IUserOperations>(
                id, user => user.New(name));

            await starter.SignalEntityAsync(
                UserCounter.Id,
                UserCounter.NewUser);

            await starter.StartNewAsync(nameof(NewUserParallelFunctions.RunUserParallelWorkflow), name);
            log.LogInformation("Started new parallel workflow for user {user}", name);

            await starter.StartNewAsync(nameof(MonitorFunctions.UserMonitorWorkflow), name);
            log.LogInformation("Started new monitor workflow for user {user}", name);

            return new OkResult();
        }

        [FunctionName(nameof(GameStatus))]
        public static async Task<IActionResult> GameStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GameStatus/{username}")]
                HttpRequest req,
            [DurableClient]IDurableClient client,
            string username,
            ILogger log)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                log.LogWarning("No username passed.");
                return new BadRequestObjectResult("Username is required.");
            }

            var userCheck = await client.ReadUserEntityAsync<User>(username);
            if (!userCheck.EntityExists)
            {
                log.LogWarning("Username {0} not found", username);
                return new BadRequestObjectResult($"Username '{username}' does not exist.");
            }
            var monsterCheck = await client.ReadUserEntityAsync<Monster>(username);
            var inventoryCheck = await client.ReadUserEntityAsync<InventoryList>(username);
            if (inventoryCheck.EntityExists)
            {
                inventoryCheck.EntityState.RestoreLists();
            }
            var roomCheck = await client.ReadUserEntityAsync<Room>(username);
            var userCount = await client.ReadEntityStateAsync<int>(
                UserCounter.Id);
            return new OkObjectResult(new
            {
                user = userCheck.EntityState,
                activeUsers = userCount.EntityState,
                monster = monsterCheck.EntityState,
                inventory = inventoryCheck.EntityState?.InventoryList,
                room = roomCheck.EntityState
            });
        }

        [FunctionName(nameof(CheckStatus))]
        public static async Task<IActionResult> CheckStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "CheckStatus/{username}/{workflow}")]
                HttpRequest req,
            string username,
            string workflow,
            [DurableClient]IDurableClient query,
            ILogger log)
        {
            log.LogInformation($"CheckStatus called for {username} and {workflow}.");
            var job = await Global.FindJob(query,
                DateTime.UtcNow,
                workflow,
                username,
                false,
                false);
            if (job == null)
            {
                return new NotFoundResult();
            }
            return new OkObjectResult(new
            {
                Created = job.CreatedTime,
                Status = job.RuntimeStatus.ToString()
            });
        }
    }
}

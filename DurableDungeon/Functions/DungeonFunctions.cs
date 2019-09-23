using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DurableDungeon.Dungeon;
using Microsoft.WindowsAzure.Storage.Table;
using DurableDungeon.DungeonMaster;
using System;

namespace DurableDungeon.Functions
{
    public static class DungeonFunctions
    {
        [FunctionName(nameof(NewUser))]
        public static async Task<IActionResult> NewUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
                HttpRequest req,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            [Table(nameof(User))]CloudTable table,
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

            var client = table.AsClientFor<User>();
            var tempUser = new User { Name = name };
            var userCheck = await client.GetAsync(tempUser.PartitionKey, name);
            if (userCheck != null)
            {
                await console.AddAsync($"Attempt to add duplicate user {name} failed.");
                return new BadRequestObjectResult("Duplicate username is not allowed.");
            }

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
            string username,
            [Table(nameof(User))]CloudTable userTable,
            [Table(nameof(Room))]CloudTable roomTable,
            [Table(nameof(Monster))]CloudTable monsterTable,
            [Table(nameof(Inventory))]CloudTable inventoryTable,
            ILogger log)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                log.LogWarning("No username passed.");
                return new BadRequestObjectResult("Username is required.");
            }

            var userClient = userTable.AsClientFor<User>();
            var tempUser = new User { Name = username };
            var userCheck = await userClient.GetAsync(tempUser.PartitionKey, username);
            if (userCheck == null)
            {
                log.LogWarning("Username {0} not found", username);
                return new BadRequestObjectResult($"Username '{username}' does not exist.");
            }
            var monsterList = await monsterTable.AsClientFor<Monster>().GetAllAsync(username);
            var inventoryList = await inventoryTable.AsClientFor<Inventory>().GetAllAsync(username);
            var roomList = await roomTable.AsClientFor<Room>().GetAllAsync(username);
            return new OkObjectResult(new
            {
                user = userCheck,
                monster = monsterList[0],
                inventory = inventoryList,
                room = roomList[0]
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

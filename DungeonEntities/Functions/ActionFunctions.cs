using DungeonEntities.Dungeon;
using DungeonEntities.DungeonMaster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DungeonEntities.Functions
{
    public static class ActionFunctions
    {
        [FunctionName(nameof(Action))]
        public static async Task<IActionResult> Action(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
                HttpRequest req,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            [OrchestrationClient]IDurableOrchestrationClient client,
            ILogger log)
        {
            log.LogInformation("Action called.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string name = data?.name;
            string action = data?.action;
            string target = data?.target;
            string with = data?.with;

            if (string.IsNullOrWhiteSpace(name))
            {
                await console.AddAsync("An attempt to initiate an action with no name was made.");
                return new BadRequestObjectResult("User name is required.");
            }

            var user = await client.ReadUserEntityAsync<User>(name);
            var userCheck = user.EntityState;
            if (!user.EntityExists || !userCheck.IsAlive)
            {
                await console.AddAsync($"Attempt to use missing or dead user {name} failed.");
                return new BadRequestObjectResult("Dead or missing username is not allowed.");
            }
            
            if (action == "get")
            {
                return await GetMethod(console, client, name, target, userCheck);
            }
            else if (action == "kill")
            {
                return await KillMethod(console, client, name, target, with, userCheck);
            }

            return new BadRequestObjectResult($"I do not understand {action}.");
        }

        private static async Task<IActionResult> KillMethod(
            IAsyncCollector<string> console, 
            IDurableOrchestrationClient client,
            string name, 
            string target, 
            string with, 
            User userCheck)
        {
            if (string.IsNullOrEmpty(target))
            {
                await console.AddAsync($"User {name} tried to kill nothing.");
                return new BadRequestObjectResult("Target is required.");
            }
            if (string.IsNullOrEmpty(with))
            {
                await console.AddAsync($"User {name} tried to kill {target} with no weapon.");
                return new BadRequestObjectResult("With is required.");
            }
            if (!userCheck.InventoryList.Contains(with))
            {
                await console.AddAsync($"User {name} tried to use a non-existing {with}.");
                return new BadRequestObjectResult($"User doesn't have {with}");
            }
            var monster = await name.GetEntityForUserOrThrow<Monster>(client);
            if (!monster.IsAlive)
            {
                await console.AddAsync($"User {name} tried to kill an already dead {target}.");
                return new BadRequestObjectResult($"{target} is already dead.");
            }

            var monsterInventory = monster.InventoryList[0];
            var inventoryNames = await name.GetEntityForUserOrThrow<InventoryList>(client);
            var room = await name.GetEntityForUserOrThrow<Room>(client);

            // monster dies
            await client.SignalEntityAsync(
                name.AsEntityIdFor<Monster>(),
                nameof(Monster.Kill));

            // monster drops inventory, inventory => room
            await client.SignalEntityAsync(
                name.AsEntityIdFor<Inventory>(monsterInventory),
                nameof(Inventory.SetRoom),
                room.Name);

            // add inventory to room
            await client.SignalEntityAsync(
                name.AsEntityIdFor<Room>(),
                nameof(Room.AddInventory),
                monsterInventory);

            await console.AddAsync($"User {name} valiantly killed {target} with {with}.");
            await console.AddAsync($"User {name} notices {target} dropped a {monsterInventory}.");
            var gameMonitor = await Global.FindJob(
                        client,
                        DateTime.UtcNow,
                        nameof(MonitorFunctions.GameMonitorWorkflow),
                        name,
                        true,
                        false);
            if (gameMonitor != null)
            {
                await client.RaiseEventAsync(gameMonitor.InstanceId,
                    MonitorFunctions.KILLMONSTER);
            }
            return new OkResult();
        }

        private static async Task<IActionResult> GetMethod(
            IAsyncCollector<string> console,
            IDurableOrchestrationClient client,
            string name,
            string target,
            User userCheck)
        {
            if (string.IsNullOrEmpty(target))
            {
                await console.AddAsync($"User {name} tried to get nothing.");
                return new BadRequestObjectResult("Target is required.");
            }
            var room = await name.GetEntityForUserOrThrow<Room>(client);
            if (room.InventoryList.Contains(target))
            {
                // room loses inventory
                await client.SignalEntityAsync(
                    name.AsEntityIdFor<Room>(),
                    nameof(Room.RemoveInventory),
                    target);

                // user gains inventory
                await client.SignalEntityAsync(
                    name.AsEntityIdFor<User>(),
                    nameof(User.AddInventory),
                    target);

                // inventory moves to user
                await client.SignalEntityAsync(
                    name.AsEntityIdFor<Inventory>(target),
                    nameof(Inventory.SetUser));

                var list = await name.GetEntityForUserOrThrow<InventoryList>(client);
                var inventoryList = await list.DeserializeListForUserWithClient(name, client);
                var inventory = inventoryList.Where(i => i.Name == target)
                    .Select(i => i).First();

                await console.AddAsync($"User {name} successfully grabbed {target}.");
                if (inventory.IsTreasure)
                {
                    await console.AddAsync($"User {name} nabbed the treasure.");
                    var gameMonitor = await Global.FindJob(
                        client,
                        DateTime.UtcNow,
                        nameof(MonitorFunctions.GameMonitorWorkflow),
                        name,
                        true,
                        false);
                    if (gameMonitor != null)
                    {
                        await client.RaiseEventAsync(gameMonitor.InstanceId,
                            MonitorFunctions.GOTTREASURE);
                    }
                }
                return new OkResult();
            }
            else
            {
                await console.AddAsync($"User {name} tried to get a {target} that wasn't there.");
                return new BadRequestObjectResult("Target not found.");
            }
        }
    }
}

using DurableDungeon.Dungeon;
using DurableDungeon.DungeonMaster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DurableDungeon.Functions
{
    public static class ActionFunctions
    {
        [FunctionName(nameof(Action))]
        public static async Task<IActionResult> Action(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
                HttpRequest req,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            [Table(nameof(User))]CloudTable userTable,
            [Table(nameof(Room))]CloudTable roomTable,
            [Table(nameof(Monster))]CloudTable monsterTable,
            [Table(nameof(Inventory))]CloudTable inventoryTable,
            [DurableClient]IDurableClient client,
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

            var userClient = userTable.AsClientFor<User>();
            var tempUser = new User { Name = name };
            var userCheck = await userClient.GetAsync(tempUser.PartitionKey, name);
            if (userCheck == null || !userCheck.IsAlive)
            {
                await console.AddAsync($"Attempt to use missing or dead user {name} failed.");
                return new BadRequestObjectResult("Dead or missing username is not allowed.");
            }
            var monsterClient = monsterTable.AsClientFor<Monster>();
            var roomClient = roomTable.AsClientFor<Room>();
            var inventoryClient = inventoryTable.AsClientFor<Inventory>();

            if (action == "get")
            {
                return await GetMethod(console, client, name, target, userClient, userCheck, roomClient, inventoryClient);
            }
            else if (action == "kill")
            {
                return await KillMethod(console, client, name, target, with, userCheck, monsterClient, roomClient, inventoryClient);
            }

            return new BadRequestObjectResult($"I do not understand {action}.");
        }

        private static async Task<IActionResult> KillMethod(
            IAsyncCollector<string> console, 
            IDurableClient client,
            string name, 
            string target, 
            string with, 
            User userCheck, 
            DataAccess<Monster> monsterClient, 
            DataAccess<Room> roomClient, 
            DataAccess<Inventory> inventoryClient)
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
            var monster = await monsterClient.GetAsync(name, target);
            if (monster == null)
            {
                await console.AddAsync($"User {name} tried to kill a ghost named {target}.");
                return new BadRequestObjectResult($"{target} not found.");
            }
            if (!monster.IsAlive)
            {
                await console.AddAsync($"User {name} tried to kill an already dead {target}.");
                return new BadRequestObjectResult($"{target} is already dead.");
            }
            var monsterInventory = monster.InventoryList[0];
            monster.InventoryList.Remove(monsterInventory);
            monster.IsAlive = false;
            await monsterClient.ReplaceAsync(monster);
            var room = (await roomClient.GetAllAsync(name))[0];
            room.InventoryList.Add(monsterInventory);
            await roomClient.ReplaceAsync(room);
            var inventory = await inventoryClient.GetAsync(name, monsterInventory);
            inventory.Monster = string.Empty;
            inventory.Room = room.Name;
            await inventoryClient.ReplaceAsync(inventory);
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
            IDurableClient client,
            string name,
            string target,
            DataAccess<User> userClient,
            User userCheck,
            DataAccess<Room> roomClient,
            DataAccess<Inventory> inventoryClient)
        {
            if (string.IsNullOrEmpty(target))
            {
                await console.AddAsync($"User {name} tried to get nothing.");
                return new BadRequestObjectResult("Target is required.");
            }
            var room = (await roomClient.GetAllAsync(name))[0];
            if (room.InventoryList.Contains(target))
            {
                room.InventoryList.Remove(target);
                await roomClient.ReplaceAsync(room);
                userCheck.InventoryList.Add(target);
                await userClient.ReplaceAsync(userCheck);
                var inventory = await inventoryClient.GetAsync(name, target);
                inventory.Room = string.Empty;
                await inventoryClient.ReplaceAsync(inventory);
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

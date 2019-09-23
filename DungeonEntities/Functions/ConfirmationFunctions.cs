using DungeonEntities.Dungeon;
using DungeonEntities.DungeonMaster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonEntities.Functions
{
    public static class ConfirmationFunctions
    {
        public const string APPROVAL_TASK = "ApprovalTask";

        [FunctionName(nameof(UserConfirmationWorkflow))]
        public static async Task UserConfirmationWorkflow(
            [OrchestrationTrigger]IDurableOrchestrationContext context,
            ILogger logger)
        {
            var username = context.GetInput<string>();
            logger.LogInformation("Start of user confirmation workflow for {user}", username);

            await context.CallActivityAsync(nameof(ConsoleFunctions.AddToQueue),
                $"User {username} now has {Global.ExpirationMinutes} minutes to confirm they are ready.");

            using (var timeoutCts = new CancellationTokenSource())
            {
                var dueTime = context.CurrentUtcDateTime
                    .Add(TimeSpan.FromMinutes(Global.ExpirationMinutes));

                var approvalEvent = context.WaitForExternalEvent<bool>(APPROVAL_TASK);

                logger.LogInformation($"Now: {context.CurrentUtcDateTime} Timeout: {dueTime}");

                var durableTimeout = context.CreateTimer(dueTime, timeoutCts.Token);

                var winner = await Task.WhenAny(approvalEvent, durableTimeout);

                if (winner == approvalEvent && approvalEvent.Result)
                {
                    timeoutCts.Cancel();
                    logger.LogInformation("User {user} confirmed.", username);
                    await context.CallActivityAsync(nameof(ConsoleFunctions.AddToQueue),
                        $"{username} has accepted the challenge!.");
                    await context.CallActivityAsync(nameof(Global.StartNewWorkflow),
                        ((nameof(NewUserSequentialFunctions.RunUserSequentialWorkflow),
                        username)));
                }
                else
                {
                    logger.LogInformation("User {user} confirmation timed out.", username);
                    await context.CallActivityAsync(nameof(ConsoleFunctions.AddToQueue),
                        $"User {username} failed to confirm in time.");
                    await context.CallActivityAsync(nameof(KillUser), username);
                }
            }

            await context.CallActivityAsync(nameof(ConsoleFunctions.AddToQueue),
                $"User confirmation workflow complete for {username}.");
        }

        [FunctionName(nameof(ConfirmUser))]
        public static async Task<IActionResult> ConfirmUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
                HttpRequest req,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            [DurableClient]IDurableClient durableClient,
            ILogger log)
        {
            log.LogInformation("ConfirmUser called.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string name = data?.name;

            if (string.IsNullOrWhiteSpace(name))
            {
                await console.AddAsync("An attempt to confirm a user with no name was made.");
                return new BadRequestObjectResult("User name is required.");
            }

            var userCheck = await durableClient.ReadUserEntityAsync<User>(name);
            if (!userCheck.EntityExists)
            {
                await console.AddAsync($"Attempt to confirm missing user {name} failed.");
                return new BadRequestObjectResult("Username does not exist.");
            }

            log.LogInformation("User {user} is valid, searching for workflow.", name);

            var instance = await durableClient.FindJob(
                DateTime.UtcNow,
                nameof(UserConfirmationWorkflow),
                name);

            if (instance == null)
            {
                log.LogInformation("Workflow not found for user {user}.", name);
                return new NotFoundResult();
            }
            log.LogInformation("Workflow with id {instanceId} found for user {user}.", instance.InstanceId, name);
            await durableClient.RaiseEventAsync(instance.InstanceId, APPROVAL_TASK, true);
            return new OkResult();
        }

        [FunctionName(nameof(KillUser))]
        public static async Task<bool> KillUser(
            [ActivityTrigger]string username,
            [DurableClient]IDurableClient client,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console,
            ILogger logger)
        {
            logger.LogInformation("Kill user: {user}", username);
            var user = await client.ReadUserEntityAsync<User>(username);
            if (!user.EntityExists)
            {
                throw new Exception($"KillUser: User {username} not found!");
            }
            await client.SignalEntityAsync<IUserOperations>(
                username.AsEntityIdFor<User>(),
                operation => operation.Kill());
            await client.SignalEntityAsync(
                UserCounter.Id,
                UserCounter.UserDone);
            await console.AddAsync($"Unfortunately user {username} died from waiting too long!");
            logger.LogInformation("KillUser {user} successful", username);
            return true;
        }
    }
}

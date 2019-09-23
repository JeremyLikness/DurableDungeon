using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DungeonEntities.Functions
{
    public static class Global
    {
        public const string QUEUE = "console";

        public const int ExpirationMinutes = 2;
        public const int DefaultSpanMinutes = 60;
        public const int MonitorTimeoutHours = 1;

        [FunctionName(nameof(StartNewWorkflow))]
        public static async Task StartNewWorkflow(
            [ActivityTrigger](string function, string username) payload,
            [DurableClient]IDurableClient client,
            ILogger logger
            )
        {
            logger.LogInformation("Starting workflow {workflow} for user {user}",
                payload.function, payload.username);
            await client.StartNewAsync(payload.function, payload.username);
        }

        public static async Task<DurableOrchestrationStatus> FindJob(
            this IDurableClient client,
            DateTime time,
            string workflowName,
            string username,
            bool runningOnly = true,
            bool confirmation = true)
        {
            var filter = runningOnly ?
                new List<OrchestrationRuntimeStatus> { OrchestrationRuntimeStatus.Running }
                : new List<OrchestrationRuntimeStatus>();
            var offset = TimeSpan.FromMinutes(confirmation ? 
                ExpirationMinutes : DefaultSpanMinutes);
            var instances = await client.GetStatusAsync(
                time.Subtract(offset),
                time.Add(offset),
                filter);
            foreach (var instance in instances)
            {
                if (instance.Name == workflowName &&
                        instance.Input.ToObject<string>() == username)
                {                    
                    return instance;
                }
            }
            return null;
        }        
    }
}

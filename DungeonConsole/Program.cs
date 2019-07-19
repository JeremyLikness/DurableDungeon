using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonConsole
{
    /// <summary>
    ///     This app will listen to the "console" queue to narrate the
    ///     durable functions demo.
    ///     
    ///     You must set environment variable "storage_connection" to the 
    ///     storage connection string. 
    ///     
    ///     It will default to storage emulator:
    ///         UseDevelopmentStorage=true;
    /// </summary>
    class Program
    {
        const string CONNECTION = "STORAGE_CONNECTION";
        const string QUEUE_NAME = "console";
        const string DEFAULT_CONNECTION = "UseDevelopmentStorage=true";

        static void Main(string[] args)
        {
            var storageConnection = Environment.GetEnvironmentVariable(CONNECTION);

            if (string.IsNullOrWhiteSpace(storageConnection))
            {
                Console.WriteLine($"{CONNECTION} not set. Defaulting to:");
                Console.WriteLine($"{DEFAULT_CONNECTION}");
                storageConnection = DEFAULT_CONNECTION;
            }

            var storageAccount = CloudStorageAccount.Parse(storageConnection);
            var client = storageAccount.CreateCloudQueueClient();           
            var queue = client.GetQueueReference(QUEUE_NAME);
            queue.CreateIfNotExistsAsync().Wait();

            Task.Run(async () => await WatchQueueAsync(queue)).Wait();
        }

        private static async Task WatchQueueAsync(CloudQueue queue)
        {
            var running = true;
            var found = false;
            while (running)
            {
                var messages = await queue.GetMessagesAsync(5);
                var dequeue = new List<CloudQueueMessage>();
                foreach (var message in messages)
                {
                    dequeue.Add(message);
                    if (!found)
                    {
                        Console.WriteLine("*");
                        found = true;
                    }
                    Console.WriteLine(message.AsString);                    
                }
                if (dequeue.Count < 1)
                {
                    found = false;
                    Console.Write(".");
                }
                else
                {
                    foreach(var message in dequeue)
                    {
                        await queue.DeleteMessageAsync(message);
                    }
                }
                Thread.Sleep(500);
                found = false;
            }
        }
    }
}

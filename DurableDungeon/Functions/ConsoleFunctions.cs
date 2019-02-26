using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace DurableDungeon.Functions
{
    public static class ConsoleFunctions
    {        
        [FunctionName(nameof(AddToQueue))]
        public static async Task AddToQueue(
            [ActivityTrigger]string message,
            [Queue(Global.QUEUE)]IAsyncCollector<string> console)
        {
            await console.AddAsync(message);
        }
    }
}

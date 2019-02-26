using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace DurableDungeon.Dungeon
{
    public class User : BaseHasInventory 
    {
        [IgnoreProperty]
        public string Name
        {
            get
            {
                return RowKey;
            }
            set
            {
                RowKey = value;
                ConfigureKeys();
            }
        }

        public string CurrentRoom { get; set; }
        public bool IsAlive { get; set; }

        private void ConfigureKeys()
        {
            if (string.IsNullOrWhiteSpace(RowKey))
            {
                throw new System.Exception($"User requires a name.");
            }
            PartitionKey = RowKey.Substring(0, 1).ToUpperInvariant();            
        }
    }
}

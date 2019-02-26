using DurableDungeon.DungeonMaster;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace DurableDungeon.Dungeon
{
    public class Monster : BaseHasInventory
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
            }
        }

        [IgnoreProperty]
        public string AssociatedUser
        {
            get
            {
                return PartitionKey;
            }
            set
            {
                PartitionKey = value;
            }
        }

        public bool IsAlive { get; set; }

        public string CurrentRoom { get; set; }
    }
}

using DurableDungeon.DungeonMaster;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace DurableDungeon.Dungeon
{
    public class Room : BaseHasInventory
    {
        [IgnoreProperty]
        public string Name
        {
            get { return RowKey; }
            set { RowKey = value; }
        }
        public string Description { get; set; }

        [IgnoreProperty]
        public string User
        {
            get { return PartitionKey; }
            set { PartitionKey = value; }
        }
            
        public string Monster { get; set; }        
    }
}

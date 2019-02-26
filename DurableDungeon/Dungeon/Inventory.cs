using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace DurableDungeon.Dungeon
{
    public class Inventory : TableEntity
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
        public string AssociatedUser
        {
            get { return PartitionKey; }
            set { PartitionKey = value; }
        }
        public string Monster { get; set; }
        public string Room { get; set; }
        public bool IsTreasure { get; set; }

        [IgnoreProperty]
        public bool UserHasIt
        {
            get
            {
                return string.IsNullOrWhiteSpace(Monster) &&
                    string.IsNullOrWhiteSpace(Room);
            }
        }
    }
}

using DurableDungeon.DungeonMaster;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace DurableDungeon.Dungeon
{
    public abstract class BaseHasInventory : TableEntity, IHaveLists
    {
        public BaseHasInventory()
        {
            InventoryList = new List<string>();
        }

        public string InventoryItems { get; set; }

        [IgnoreProperty]
        public List<string> InventoryList { get; set; }

        public virtual void RestoreLists()
        {
            InventoryList = InventoryItems?.AsList();
        }

        public virtual void SaveLists()
        {
            InventoryItems = InventoryList?.AsString();
        }
    }
}

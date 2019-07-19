using DungeonEntities.DungeonMaster;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace DungeonEntities.Dungeon
{
    public abstract class BaseHasInventory : IHaveLists
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

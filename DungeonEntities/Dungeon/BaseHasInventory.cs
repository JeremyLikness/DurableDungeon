using DungeonEntities.DungeonMaster;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
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

        [JsonIgnore]
        public List<string> InventoryList { get; set; }

        public virtual void RestoreLists()
        {
            if (string.IsNullOrWhiteSpace(InventoryItems))
            {
                InventoryList = new List<string>();
            }
            else
            {
                InventoryList = InventoryItems.AsList();
            }
        }

        public virtual void SaveLists()
        {
            InventoryItems = InventoryList?.AsString();
        }
    }
}

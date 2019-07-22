using System;
using System.Collections.Generic;
using System.Text;

namespace DungeonEntities.Dungeon
{
    public interface IInventoryOperations
    {
        void New(string name);
        void SetTreasure();
        void SetMonster(string monster);
        void SetRoom(string room);
        void SetUser();
    }
}

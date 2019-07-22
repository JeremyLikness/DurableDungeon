namespace DungeonEntities.Dungeon
{
    public interface IMonsterOperations
    {
        void New(string name);
        void SetRoom(string room);
        void AddInventory(string name);
        void Kill();        
    }
}

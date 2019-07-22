namespace DungeonEntities.Dungeon
{
    public interface IUserOperations
    {
        void New(string user);
        void Kill();
        void SetRoom(string room);
        void AddInventory(string inventory);        
    }
}

namespace DungeonEntities.Dungeon
{
    public interface IRoomOperations
    {
        void New(string name);
        void SetDescription(string description);
        void SetMonster(string monster);
        void AddInventory(string inventory);
        void RemoveInventory(string inventory);
    }
}

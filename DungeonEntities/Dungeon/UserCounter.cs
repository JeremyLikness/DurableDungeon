using Microsoft.Azure.WebJobs;

namespace DungeonEntities.Dungeon
{
    public class UserCounter
    {
        public const string NewUser = "newuser";
        public const string UserDone = "done";

        public static EntityId Id
        {
            get
            {
                return new EntityId(nameof(UserCounter), nameof(User));
            }
        }

        [FunctionName(nameof(UserCounter))]
        public static void Counter([EntityTrigger]IDurableEntityContext ctx)
        {
            int currentValue = ctx.GetState<int>();

            switch (ctx.OperationName)
            {
                case NewUser:
                    currentValue += 1;
                    break;
                case UserDone:
                    currentValue -= 1;
                    break;
            }

            ctx.SetState(currentValue);
        }
    }
}

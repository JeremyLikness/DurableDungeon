using DungeonEntities.Dungeon;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DungeonEntities.DungeonMaster
{
    public static class Extensions
    {
        public static readonly string Monster = nameof(Monster);
        public static readonly string InventoryList = nameof(InventoryList);
        public static readonly string Room = nameof(Room);

        private static readonly Random Rnd = new Random();
        
        public static string PickRandom(this string[] selection)
            => selection[Rnd.Next(0, selection.Length - 1)];

        public static string AsIdFor(this string key, string user)
            => $"{user}:{key}";

        public static EntityId AsEntityIdFor<T>(this string user)
        {
            return new EntityId(nameof(T), user);
        }

        public static Task<EntityStateResponse<T>> ReadUserEntityAsync<T>(this IDurableOrchestrationClient client, string user)
        {
            var id = user.AsEntityIdFor<T>();
            return client.ReadEntityStateAsync<T>(id);
        }

        public static void PrepareForSave<T>(this T item)
        {
            if (item is IHaveLists)
            {
                var itemWithList = item as IHaveLists;
                itemWithList.SaveLists();
            }
        }

        public static void PrepareAfterLoad<T>(this T item)
        {
            if (item is IHaveLists)
            {
                var itemWithList = item as IHaveLists;
                itemWithList.RestoreLists();
            }
        }

        public static string AsString(this List<string> list)
        {
            return string.Join(',', list);
        }

        public static List<string> AsList(this string item)
        {
            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(item))
            {
                list.AddRange(item.Split(","));
            }
            return list;
        }        
    }
}

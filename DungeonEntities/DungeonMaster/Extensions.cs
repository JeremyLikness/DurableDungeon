using DungeonEntities.Dungeon;
using Dynamitey;
using Dynamitey.DynamicObjects;
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

        public static EntityId AsEntityIdFor<T>(this string user, string treasureName = null)
        {
            var key = string.IsNullOrWhiteSpace(treasureName) ?
                  user : $"{user}:{treasureName}";
            return new EntityId(typeof(T).Name, key);
        }

        public static async Task<EntityStateResponse<T>> ReadUserEntityAsync<T>(this IDurableClient client, string user)
        {
            var id = user.AsEntityIdFor<T>();
            var result = await client.ReadEntityStateAsync<T>(id);
            if (result.EntityState is IHaveLists)
            {
                ((IHaveLists)result.EntityState).RestoreLists();
            }
            return result;
        }

        public static async Task<T> GetEntityForUserOrThrow<T>(this string username, IDurableClient client)
        {
            var check = await client.ReadUserEntityAsync<T>(username);
            if (!check.EntityExists)
            {
                throw new Exception($"No {typeof(T)} found for user {username}");
            }
            return check.EntityState;
        }

        public static async Task<List<Inventory>> DeserializeListForUserWithClient(this InventoryList list, string user, IDurableClient client)
        {
            var result = new List<Inventory>();
            list.RestoreLists();
            foreach (var item in list.InventoryList)
            {
                var id = user.AsEntityIdFor<Inventory>(item);
                var inventory = await client.ReadEntityStateAsync<Inventory>(id);
                if (inventory.EntityExists)
                {
                    result.Add(inventory.EntityState);
                }
            }
            return result;
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

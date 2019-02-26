using DurableDungeon.Dungeon;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;

namespace DurableDungeon.DungeonMaster
{
    public static class Extensions
    {
        private static readonly Random Rnd = new Random();
        
        public static DataAccess<T> AsClientFor<T>(this CloudTable table)
            where T : TableEntity, new()
        {
            return new DataAccess<T>(table);
        }

        public static string PickRandom(this string[] selection)
        {
            return selection[Rnd.Next(0, selection.Length - 1)];
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

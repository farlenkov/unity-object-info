using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityObjectInfo
{
    public static class ListExt
    {
        public static T GetByID<T>(this T[] list, int id) where T : ObjectInfo
        {
            for (var i = 0; i < list.Length; i++)
            {
                var item = list[i];

                if (item.ID == id)
                    return item;
            }

            return null;
        }

        public static bool TryGetByID<T>(this T[] list, int id, out T result) where T : ObjectInfo
        {
            result = list.GetByID(id);
            return result != null;
        }
    }
}

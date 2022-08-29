using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtility;

namespace UnityObjectInfo
{
    public abstract class ObjectInfo : ScriptableObject
    {
        [ReadOnly]
        public ushort ID;

        public bool Enabled = true;

        // STATIC

        static List<ObjectInfo> All;// { get; private set; }
        static Dictionary<int, ObjectInfo> ByID;// { get; private set; }
        static Dictionary<Type, object> ByType;// { get; private set; }

#if UNITY_2017_1_OR_NEWER

        public static void LoadFromResources()
        {
            var assets = Resources.LoadAll<ObjectInfo>("Info");
            All = new List<ObjectInfo>(assets);
            ByID = new Dictionary<int, ObjectInfo>(assets.Length);
            ByType = new Dictionary<Type, object>();

            foreach (var asset in assets)
                ByID.Add(asset.ID, asset);
        }

#endif

        public static void LoadFromList<ITEM, LIST>(LIST list) 
            where ITEM : ObjectInfo
            where LIST : InfoList<ITEM>
        {
            All = All ?? new List<ObjectInfo>();
            ByID = ByID ?? new Dictionary<int, ObjectInfo>();
            ByType = ByType ?? new Dictionary<Type, object>();

            list.Init();
            ByType.Add(typeof(ITEM), list);

            foreach (var asset in list.All)
            {
                All.Add(asset);
                ByID.Add(asset.ID, asset);
            }
        }
         
        public static bool TryGetByID<INFO>(int id, out INFO info) where INFO : ObjectInfo
        {
            if (All == null)
            {
                Log.Error("[ObjectInfo: TryGetByID] All == null");
                info = null;
                return false;
            }

            if (ByID.TryGetValue(id, out var asset))
            {
                if (typeof(INFO).IsAssignableFrom(asset.GetType()))
                {
                    info = asset as INFO;
                    return true;
                }
                else
                {
                    Log.Error(
                        "[ObjectInfo: TryGetByID] Incompatible type: {0} but expected {1}",
                        asset.GetType(),
                        typeof(INFO));
                }
            }

            info = null;
            return false;
        }

        public static bool TryGetByType<INFO>(out InfoList<INFO> info_list) where INFO : ObjectInfo
        {
            if (ByType.TryGetValue(typeof(INFO), out var obj_list))
            {
                info_list = (InfoList<INFO>)obj_list;
                return true;
            }

            info_list = null;

            for (var i = 0; i < All.Count; i++)
            {
                var info = All[i];

                if (typeof(INFO).IsAssignableFrom(info.GetType()))
                {
                    if (info_list == null)
                        info_list = new InfoList<INFO>();

                    info_list.Add(info as INFO);
                }
            }

            if (info_list == null)
                return false;

            info_list.Init();
            ByType.Add(typeof(INFO), info_list);
            return true;
        }

#if UNITY_EDITOR

        public static INFO LoadInfo<INFO>(int id) where INFO : ObjectInfo
        {
            var infos = Resources.LoadAll<INFO>("");

            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];

                if (info.ID == id)
                    return info;
            }

            return null;
        }

#endif
    }
}
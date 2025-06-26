using System;
using System.Collections;
using System.Collections.Generic;
using CoreUtils;
using Newtonsoft.Json;
using UnityEngine;
using UnityUtility;

namespace UnityObjectInfo
{
    public abstract class ObjectInfo : ScriptableObject
    {
        [ReadOnly]
        public int ID;

        public bool Enabled = true;

        // STATIC

        internal static List<ObjectInfo> All { get; private set; }
        internal static Dictionary<int, ObjectInfo> ByID { get; private set; }
        internal static Dictionary<Type, InfoList> ByType { get; private set; }

        public static bool IsLoaded => ByType != null;
        public static long ExportDate { get; private set; }

#if UNITY_2017_1_OR_NEWER

        public static T LoadFromResources<T>(string path = "Info")
            where T : new()
        {
            var rootObject = new T();
            var listType = typeof(InfoList);
            var sourceType = rootObject.GetType();
            var sourceFields = sourceType.GetFields();

            foreach (var field in sourceFields)
            {
                if (!listType.IsAssignableFrom(field.FieldType))
                    continue;

                var list = (InfoList)Activator.CreateInstance(field.FieldType);
                var items = Resources.LoadAll(path, list.InfoType);
                list.AddArray(items);
                field.SetValue(rootObject, list);
            }

            return rootObject;
        }

#endif

        public static void LoadFromObjectFields<T>(T sourceObject) where T : SerializationContainer
        {
            var listType = typeof(InfoList);
            var sourceType = sourceObject.GetType();
            var sourceFields = sourceType.GetFields();

            All = new List<ObjectInfo>();
            ByID = new Dictionary<int, ObjectInfo>();
            ByType = new Dictionary<Type, InfoList>();
            ExportDate = sourceObject.ExportDate;

            foreach (var field in sourceFields)
            {
                if (!listType.IsAssignableFrom(field.FieldType))
                    continue;

                var list = (InfoList)field.GetValue(sourceObject);

                if (list == null)
                {
                    Log.Error(
                        "[ObjectInfo: LoadFromObjectFields] ERROR: {0}.{1} is null",
                        sourceType.Name,
                        field.Name);

                    continue;
                }

                var type = list.GetType();

                if (!ByType.ContainsKey(type))
                {
                    ByType.Add(type, list);
                    list.AddToObjectInfoCache();
                    list.Init();
                }
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

        public static bool TryGetByType<LIST>(out LIST infoList)
            where LIST : InfoList, new()
        {
            if (ByType == null)
            {
                Log.Error("[ObjectInfo: TryGetByType] ByType == null");
                infoList = null;
                return false;
            }

            if (ByType.TryGetValue(typeof(LIST), out var obj_list))
            {
                infoList = (LIST)obj_list;
                return true;
            }

            infoList = new LIST();

            for (var i = 0; i < All.Count; i++)
            {
                var info = All[i];

                if (infoList.InfoType.IsAssignableFrom(info.GetType()))
                    infoList.Add(info);
            }

            if (infoList.Count == 0)
                return false;

            infoList.Init();
            ByType.Add(typeof(LIST), infoList);
            return true;
        }

        public static bool TryGetByType<INFO>(out InfoList<INFO> infoList) where INFO : ObjectInfo
        {
            return TryGetByType<InfoList<INFO>>(out infoList);
        }

        public static bool TryGetFirst<INFO>(out INFO info) where INFO : ObjectInfo
        {
            if (!TryGetByType<INFO>(out var list))
            {
                info = default;
                return false;
            }

            if (list.Count == 0)
            {
                info = default;
                return false;
            }

            info = list[0];
            return true;
        }

#if UNITY_EDITOR

        [JsonIgnore] public virtual int MinID => 1;
        [JsonIgnore] public virtual int MaxID => ushort.MaxValue;

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
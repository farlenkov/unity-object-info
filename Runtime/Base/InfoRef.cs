using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityUtility;

namespace UnityObjectInfo
{
    [Serializable]
    public class InfoRef 
    {
        public ushort ID;
        public virtual Type InfoType => typeof(ObjectInfo);

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is ObjectInfo info))
                return false;

            return ID == info.ID;
        }

        public static bool operator ==(InfoRef ref1, InfoRef ref2)
        {
            return ref1?.ID == ref2?.ID;
        }

        public static bool operator !=(InfoRef ref1, InfoRef ref2)
        {
            return ref1?.ID != ref2?.ID;
        }
    }

    [Serializable]
    public class InfoRef<INFO> : InfoRef where INFO : ObjectInfo
    {
        [JsonIgnore]
        public override Type InfoType => typeof(INFO);

        INFO AssetCache;

        public virtual INFO GetAsset()
        {
            if (AssetCache != null)
                return AssetCache;

            if (ID == 0)
                return default;

            if (ObjectInfo.TryGetByID<INFO>(ID, out var asset))
            {
                AssetCache = asset;
                return AssetCache;
            }

#if UNITY_EDITOR

            if (!Application.isPlaying)
                return ObjectInfo.LoadInfo<INFO>(ID);
#endif

            return default;
        }

#if UNITY_2017_1_OR_NEWER
        public virtual INFO GetAssetOrDefault()
        {
            return GetAsset() ?? ScriptableObject.CreateInstance<INFO>();
        }
#endif

#if UNITY_EDITOR

        public virtual INFO GetEditorAsset()
        {
            var all = Resources.LoadAll<INFO>("");

            for (var i = 0; i < all.Length; i++)
            {
                var item = all[i];

                if (item.ID == ID)
                    return item;
            }

            return null;
        }
#endif
        }

    public static class InfoRefExt
    {
        public static bool IsDefined(this InfoRef preset_ref)
        {
            return
                preset_ref != null &&
                preset_ref.ID > 0;
        }

        public static bool IsDefined<INFO>(this InfoRef<INFO> info_ref) where INFO : ObjectInfo
        {
            return info_ref?.ID > 0;
        }

        public static bool TryGetAsset<INFO>(this InfoRef<INFO> info_ref, out INFO asset) where INFO : ObjectInfo
        {
            if (!info_ref.IsDefined())
            {
                asset = null;
                return false;
            }

            asset = info_ref.GetAsset();
            return asset != null;
        }
    }
}
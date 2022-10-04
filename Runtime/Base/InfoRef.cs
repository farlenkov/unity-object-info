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

            if (ObjectInfo.TryGetByID<INFO>(ID, out var asset))
            {
                AssetCache = asset;
                return AssetCache;
            }

#if UNITY_EDITOR

            if (!Application.isPlaying)
                return ObjectInfo.LoadInfo<INFO>(ID);
#endif

            return null;
        }
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
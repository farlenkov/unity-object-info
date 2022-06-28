using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityObjectInfo
{
    public abstract class EntityInfo : ObjectInfo
    {
        
    }

    [Serializable]
    public class EntityInfoRef : InfoRef<EntityInfo>
    {
        protected override Dictionary<int, EntityInfo> Index => null;
    }
}
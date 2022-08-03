using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityObjectInfo
{
    //[CreateAssetMenu]
    public class SampleEntity : EntityInfo
    {
        [HideInInspector]
        public SampleComponentRef Component1;
        [HideInInspector]
        public SampleComponentRef Component2;
    }

    [Serializable]
    public class SampleEntityRef : InfoRef<SampleEntity>
    {
        protected override Dictionary<int, SampleEntity> Index => null;
    }
}
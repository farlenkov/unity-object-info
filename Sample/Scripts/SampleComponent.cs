using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityObjectInfo
{
    [Serializable]
    public class SampleComponent : ComponentInfo
    {
        [Space]
        public SampleEntityRef OtherEntity;
        public SampleComponentRef OtherComponent;

        [Space]
        public int Value1;
        public string Value2;
        public GameObject Value3;

        [Space]
        public SampleComponentData Data1;
        public SampleComponentData Data2;
        public SampleComponentData Data3;
    }

    [Serializable]
    public struct SampleComponentData // : IComponentData
    {
        public int Value;
    }

    public class SampleComponentList : InfoList<SampleComponent>
    {

    }

    [Serializable]
    public class SampleComponentRef : InfoRef<SampleComponent>
    {

    }
}
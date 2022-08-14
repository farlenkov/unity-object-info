using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtility;

namespace UnityObjectInfo
{
    public abstract class ComponentInfo : ObjectInfo
    {
        [HideInInspector]
        public EntityInfoRef Entity;

#if UNITY_EDITOR

        [HideInInspector]
        public string ShortName;

        [NonSerialized]
        UnityEditor.Editor MyEditor;

        public UnityEditor.Editor GetEditor()
        {
            if (MyEditor == null)
                MyEditor = UnityEditor.Editor.CreateEditor(this);

            return MyEditor;
        }

#endif
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
        Editor MyEditor;

        public Editor GetEditor()
        {
            if (MyEditor == null)
                MyEditor = Editor.CreateEditor(this);

            return MyEditor;
        }

#endif
    }
}
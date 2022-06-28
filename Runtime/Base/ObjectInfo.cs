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

        public static INFO GetInfo<INFO>(int id) where INFO : ObjectInfo
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
    }
}
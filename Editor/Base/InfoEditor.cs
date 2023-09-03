using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityUtility;

namespace UnityObjectInfo
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ObjectInfo), true)]
    public class InfoEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        public virtual bool OnImport()
        {
            return false;
        }

        // STATIC

        public static int InfoField<INFO>(
            string label,
            int id,
            params GUILayoutOption[] options)
            where INFO : ObjectInfo
        {
            var infos = Resources.LoadAll<INFO>("");
            return Field(label, id, infos, options);
        }

        public static int Field<INFO>(
            string label,
            int id,
            INFO[] infos,
            params GUILayoutOption[] options)
            where INFO : ObjectInfo
        {
            var info = (INFO)null;

            for (var i = 0; i < infos.Length; i++)
                if (infos[i].ID == id)
                    info = infos[i];

            if (label == null)
                info = (INFO)EditorGUILayout.ObjectField(info, typeof(INFO), false, options);
            else
                info = (INFO)EditorGUILayout.ObjectField(label, info, typeof(INFO), false, options);

            if (info != null)
                return info.ID;
            else
                return 0;
        }

        public static ushort Field<INFO>(
            string label,
            int id,
            Rect position)
            where INFO : ObjectInfo
        {
            var infos = Resources.LoadAll<INFO>("");
            var info = (INFO)null;

            for (var i = 0; i < infos.Length; i++)
                if (infos[i].ID == id)
                    info = infos[i];

            info = (INFO)EditorGUI.ObjectField(
                position,
                label,
                info,
                typeof(INFO),
                false);

            if (info != null)
                return info.ID;
            else
                return 0;
        }

        public static int Popup<INFO>(
            int id,
            List<INFO> list,
            bool addNoneOption = false)
            where INFO : ObjectInfo
        {
            return Popup(null, id, list, addNoneOption);
        }

        public static int Popup<INFO>(
            string label,
            int id,
            List<INFO> list,
            bool addNoneOption = false)
            where INFO : ObjectInfo
        {
            var offset = addNoneOption ? 1 : 0;
            var names = new string[list.Count + offset];
            var ids = new int[list.Count + offset];

            if (addNoneOption)
            {
                ids[0] = 0;
                names[0] = "None";
            }

            for (var i = 0; i < list.Count; i++)
            {
                ids[i + offset] = list[i].ID;
                names[i + offset] = $"{list[i].name}";
            }

            if (string.IsNullOrWhiteSpace(label))
                return EditorGUILayout.IntPopup(id, names, ids);
            else
                return EditorGUILayout.IntPopup(label, id, names, ids);
        }
    }
}
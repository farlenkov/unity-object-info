using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityUtility;

namespace UnityObjectInfo
{
    [CustomPropertyDrawer(typeof(InfoRef), true)]
    public class InfoRefEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            var prop = (InfoRef)property.GetTargetObjectOfProperty();

            if (prop != null)
            {
                prop.ID = prop.ID == 0
                    ? InfoField($"{label.text} [ref]", prop.ID, position, prop.InfoType)
                    : InfoField($"{label.text} [ref: {prop.ID}]", prop.ID, position, prop.InfoType);
            }
            else
            {
                InfoField(label.text + " [ref]", 0, position, null);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(property.serializedObject.targetObject);

                if (property.serializedObject.targetObject is Component)
                {
                    var comp = property.serializedObject.targetObject as Component;

                    if (comp.gameObject != null &&
                        comp.gameObject.scene != null)
                    {
                        EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 16;
        }

        static object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;

            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                if (f != null)
                    return f.GetValue(source);

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (p != null)
                    return p.GetValue(source, null);

                type = type.BaseType;
            }
            return null;
        }

        static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;

            if (enumerable == null)
                return null;

            var enm = enumerable.GetEnumerator();

            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;

            for (int i = 0; i <= index; i++)
                if (!enm.MoveNext())
                    return null;

            return enm.Current;
        }

        public static int InfoField(
            string label,
            int id,
            Rect position,
            Type infoType)
        {
            var infoList = Resources.LoadAll<ObjectInfo>("");
            var info = (ObjectInfo)null;

            for (var i = 0; i < infoList.Length; i++)
            {
                if (infoList[i].ID == id)
                {
                    info = infoList[i];
                    break;
                }
            }

            info = (ObjectInfo)EditorGUI.ObjectField(position, label, info, infoType, false);

            if (info != null)
                return info.ID;
            else
                return 0;
        }

        public static int InfoField(
            string label,
            int id,
            Type infoType,
            params GUILayoutOption[] options)
        {
            var infoList = Resources.LoadAll<ObjectInfo>("");
            var info = (ObjectInfo)null;

            if (id > 0)
            {
                for (var i = 0; i < infoList.Length; i++)
                {
                    if (infoList[i].ID == id)
                    {
                        info = infoList[i];
                        break;
                    }
                }
            }

            if (label == null)
                info = (ObjectInfo)EditorGUILayout.ObjectField(info, infoType, false, options);
            else
                info = (ObjectInfo)EditorGUILayout.ObjectField(label, info, infoType, false, options);

            if (info != null)
                return info.ID;
            else
                return 0;
        }

        public static bool InfoRefList<INFO, REF>(ref List<REF> list)
            where INFO : ObjectInfo
            where REF : InfoRef<INFO>, new()
        {
            var isDirty = false;
            var newId = 0;

            if (list == null)
                list = new List<REF>();

            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                newId = InfoField(null, item.ID, typeof(INFO));

                if (newId == 0)
                {
                    list.RemoveAt(i);
                    isDirty = true;
                    i--;
                }
                else
                {
                    if (newId != item.ID)
                    {
                        item.ID = newId;
                        isDirty = true;
                    }
                }
            }

            if (list.Count > 0)
                GUILayout.Space(6);

            newId = InfoField(null, 0, typeof(INFO));

            if (newId > 0)
            {
                list.Add(new REF() { ID = newId });
                isDirty = true;
            }

            return isDirty;
        }
    }

    public abstract class InfoRefEditor<PROP, INFO> : PropertyDrawer
        where PROP : InfoRef
        where INFO : ObjectInfo
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            var prop = (PROP)property.GetTargetObjectOfProperty();

            if (prop != null)
                prop.ID = InfoEditor.Field<INFO>(label.text + " [ref]", prop.ID, position);
            else
                InfoEditor.Field<INFO>(label.text + " [ref]", 0, position);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(property.serializedObject.targetObject);

                if (property.serializedObject.targetObject is Component)
                {
                    var comp = property.serializedObject.targetObject as Component;

                    if (comp.gameObject != null &&
                        comp.gameObject.scene != null)
                        EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 16;
        }
    }
}
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

            var prop = (InfoRef)GetTargetObjectOfProperty(property);

            if (prop != null)
            {
                prop.ID = prop.ID == 0
                    ? PresetField($"{label.text} [ref]", prop.ID, position, prop.InfoType)
                    : PresetField($"{label.text} [ref: {prop.ID}]", prop.ID, position, prop.InfoType);
            }
            else
            {
                PresetField(label.text + " [ref]", 0, position, null);
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

        static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null)
                return null;

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');

            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
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

        public static ushort PresetField(
            string label,
            int id,
            Rect position,
            Type preset_type)
        {
            var presets = Resources.LoadAll<ObjectInfo>("");
            var preset = (ObjectInfo)null;

            for (var i = 0; i < presets.Length; i++)
            {
                if (presets[i].ID == id)
                {
                    preset = presets[i];
                    break;
                }
            }

            preset = (ObjectInfo)EditorGUI.ObjectField(position, label, preset, preset_type, false);

            if (preset != null)
                return preset.ID;
            else
                return 0;
        }

        public static ushort PresetField(
            string label,
            int id,
            Type preset_type,
            params GUILayoutOption[] options)
        {
            var presets = Resources.LoadAll<ObjectInfo>("");
            var preset = (ObjectInfo)null;

            if (id > 0)
            {
                for (var i = 0; i < presets.Length; i++)
                {
                    if (presets[i].ID == id)
                    {
                        preset = presets[i];
                        break;
                    }
                }
            }

            if (label == null)
                preset = (ObjectInfo)EditorGUILayout.ObjectField(preset, preset_type, false, options);
            else
                preset = (ObjectInfo)EditorGUILayout.ObjectField(label, preset, preset_type, false, options);

            if (preset != null)
                return preset.ID;
            else
                return 0;
        }

        public static bool PresetRefList<PRESET, REF>(ref List<REF> list)
            where PRESET : ObjectInfo
            where REF : InfoRef<PRESET>, new()
        {
            var is_dirty = false;
            var new_id = (ushort)0;

            if (list == null)
                list = new List<REF>();

            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                new_id = PresetField(null, item.ID, typeof(PRESET));

                if (new_id == 0)
                {
                    list.RemoveAt(i);
                    is_dirty = true;
                    i--;
                }
                else
                {
                    if (new_id != item.ID)
                    {
                        item.ID = new_id;
                        is_dirty = true;
                    }
                }
            }

            if (list.Count > 0)
                GUILayout.Space(6);

            new_id = PresetField(null, 0, typeof(PRESET));

            if (new_id > 0)
            {
                list.Add(new REF() { ID = new_id });
                is_dirty = true;
            }

            return is_dirty;
        }
    }

    public abstract class PresetRefEditor<PROP, PRESET> : PropertyDrawer
        where PROP : InfoRef
        where PRESET : ObjectInfo
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            var prop = (PROP)property.GetTargetObjectOfProperty();

            if (prop != null)
                prop.ID = InfoEditor.Field<PRESET>(label.text + " [ref]", prop.ID, position);
            else
                InfoEditor.Field<PRESET>(label.text + " [ref]", 0, position);

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
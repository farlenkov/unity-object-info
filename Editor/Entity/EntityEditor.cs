using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityUtility;

namespace UnityObjectInfo
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(EntityInfo), true)]
    public class EntityEditor : InfoEditor
    {
        protected EntityInfo Target => target as EntityInfo;
        static Dictionary<string, bool> Foldouts;

        // DRAW

        public override void OnInspectorGUI()
        {            
            DrawDefaultInspector();
            DrawComponentNew(Target);
            DrawComponentOld(Target);
        }
                
        protected void DrawComponentNew(EntityInfo Target)
        {
            if (!EditorUtility.IsPersistent(Target))
                return;

            var component_fields = GetComponentFields(Target);

            for (var i = 0; i < component_fields.Count; i++)
            {
                var component_field = component_fields[i];
                var component_ref = component_field.GetValue(Target) as InfoRef;

                // CHECK DUPLICATE

                if (component_ref.ID > 0)
                {
                    component_fields.RemoveAt(i);
                    i--;
                }
            }

            GUILayout.Space(10);

            var names = new string[component_fields.Count + 1];
            names[0] = "Select component to add...";

            for (var i = 0; i < component_fields.Count; i++)
                names[i + 1] = component_fields[i].Name;

            var selected_index = EditorGUILayout.Popup("Components", 0, names);

            if (selected_index > 0)
            {
                var selected_field = component_fields[selected_index - 1];
                var selected_ref = selected_field.GetValue(Target) as InfoRef;
                var component = AddComponent(Target, selected_ref.InfoType, selected_field.Name);
                
                selected_ref.ID = component.ID;

                EditorUtility.SetDirty(Target);
                AssetDatabase.SaveAssets();
            }
        }

        public static T AddComponent<T>(EntityInfo entityInfo, string name) where T : ComponentInfo
        {
            return (T)AddComponent(entityInfo, typeof(T), name);
        }

        public static ComponentInfo AddComponent(EntityInfo entityInfo, Type type, string name)
        {
            var component = (ComponentInfo)CreateInstance(type);
            component.name = $"{entityInfo.name}_{name}";
            component.Entity = new EntityInfoRef() { ID = entityInfo.ID };
            component.ShortName = name;

            AssetDatabase.AddObjectToAsset(component, entityInfo);
            EditorUtility.SetDirty(entityInfo);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return component;
        }

        protected void DrawComponentOld(EntityInfo Target, bool drawUnlinked = true)
        {
            var components = LoadComponents<ComponentInfo>(Target);
            var component_fields = GetComponentFields(Target);

            foreach (var component_field in component_fields)
            {
                var component_ref = component_field.GetValue(Target) as InfoRef;

                if (component_ref.ID == 0)
                    continue;

                foreach (var component in components)
                {
                    if (component.ID == component_ref.ID)
                    {
                        DrawComponent(Target, component, component_ref);
                        components.Remove(component);
                        break;
                    }
                }
            }

            if (drawUnlinked &&
                components.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("UNLINKED COMPONENTS (ERROR):");

                for (var i = 0; i < components.Count; i++)
                {
                    var component = components[i];
                    DrawComponent(Target, component, null);
                }
            }
        }

        static void DrawComponent(
            EntityInfo Target,
            ComponentInfo component,
            InfoRef component_ref)
        {
            if (Foldouts == null)
                Foldouts = new Dictionary<string, bool>();

            var foldout_key = $"{Target.GetType().Name}_{component.ShortName}";
            var show = Foldouts.GetItem(foldout_key);
            var component_type = component.GetType();

            //show = EditorGUILayout.BeginFoldoutHeaderGroup(
            //    show,
            //    component.ShortName,
            //    null,
            //    (pos) =>
            //    {
            //        var menu = new GenericMenu();
            //        menu.AddItem(new GUIContent("Remove"), false, () => { RemoveComponent(Target, component, component_ref); });
            //        menu.DropDown(pos);
            //    });

            // FOLDOUT HEADER

            GUILayout.BeginHorizontal();
            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            show = GUILayout.Toggle(show, (show ? "[-] ":"[+] ") +  component.ShortName, "Button");
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            var remove = GUILayout.Button("Remove", GUILayout.Width(80));
            GUILayout.EndHorizontal();

            // COMPONENT EDITOR

            if (show)
            {
                var style = new GUIStyle(EditorStyles.helpBox);
                style.padding = new RectOffset(10, 10, 10, 10);

                GUILayout.BeginVertical(style);
                var editor = component.GetEditor();
                editor.OnInspectorGUI();
                GUILayout.EndVertical();
            }

            Foldouts.SetItem(foldout_key, show);

            if (remove)
                RemoveComponent(Target, component, component_ref);
        }

        static void RemoveComponent(
            EntityInfo Target,
            ComponentInfo component,
            InfoRef component_ref)
        {
            if (component_ref != null)
                component_ref.ID = 0;

            DestroyImmediate(component, true);

            EditorUtility.SetDirty(Target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // IMPORT

        public override bool OnImport()
        {
            return FixComponentNamesAndRefs();
        }

        bool FixComponentNamesAndRefs()
        {
            var is_dirty = false;
            var component_fields = GetComponentFields(Target);
            var components = LoadComponents<ComponentInfo>(Target);

            foreach (var component_field in component_fields)
            {
                var component_ref = component_field.GetValue(Target) as InfoRef;

                if (component_ref.ID == 0)
                    continue;

                // GET Component BY FIELD

                var component_by_field = (ComponentInfo)null;

                foreach (var component in components)
                {                    
                    if (component.ID == component_ref.ID)
                    {
                        component_by_field = component;
                        break;
                    }

                    if (InfoPostprocessor.OldIDs.TryGetValue(component.ID, out var old_id))
                    {
                        if (old_id == component_ref.ID)
                        {
                            component_by_field = component;
                            component_ref.ID = component.ID;
                            is_dirty = true;
                            break;
                        }
                    }
                }

                if (component_by_field == null)
                {
                    // FIX Component ID

                    component_ref.ID = 0;
                    is_dirty = true;
                }
                else
                {
                    // FIX Component NAME

                    var component_name = $"{Target.name}_{component_field.Name}";

                    if (component_by_field.name != component_name)
                    {
                        component_by_field.name = component_name;
                        is_dirty = true;
                    }

                    // FIX ENTITY ID

                    if (component_by_field.Entity.ID != Target.ID)
                    {
                        component_by_field.Entity.ID = Target.ID;
                        is_dirty = true;
                    }
                }
            }

            return is_dirty;
        }

        // COMMON

        public static List<FieldInfo> GetComponentFields(EntityInfo info)
        {
            var result = new List<FieldInfo>();
            var ref_type = typeof(InfoRef);
            var component_type = typeof(ComponentInfo);

            foreach (var field in info.GetType().GetFields())
            {
                if (field.FieldType.IsSubclassOf(ref_type))
                {
                    var preset_ref = field.GetValue(info) as InfoRef;
                    var preset_type = preset_ref.InfoType;

                    if (preset_type.IsSubclassOf(component_type))
                        result.Add(field);
                }
            }

            return result;
        }

        public static List<T> LoadComponents<T>(EntityInfo info, bool sortByName = true) where T : ComponentInfo
        {
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(info));
            var components = new List<T>();

            foreach (var asset in assets)
                if (asset is T component)
                    components.Add(component);

            if (sortByName)
                components = components.OrderBy(o => o.name).ToList();

            return components;
        }
    }
}
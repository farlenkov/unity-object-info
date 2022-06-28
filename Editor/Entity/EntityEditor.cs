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
        EntityInfo Target => target as EntityInfo;
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
                var component = (ComponentInfo)CreateInstance(selected_ref.InfoType);

                component.name = $"{Target.name}_{selected_field.Name}";
                component.Entity = new EntityInfoRef() { ID = Target.ID };
                component.ShortName = selected_field.Name;

                AssetDatabase.AddObjectToAsset(component, Target);
                EditorUtility.SetDirty(Target);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                selected_ref.ID = component.ID;
                EditorUtility.SetDirty(Target);
                AssetDatabase.SaveAssets();
            }
        }

        protected void DrawComponentOld(EntityInfo Target)
        {
            var components = LoadComponents(Target);
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

            if (components.Count > 0)
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

            show = EditorGUILayout.BeginFoldoutHeaderGroup(
                show,
                component.ShortName,
                null,
                (pos) =>
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Remove"), false, () => { RemoveComponent(Target, component, component_ref); });
                    menu.DropDown(pos);
                });

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
            EditorGUILayout.EndFoldoutHeaderGroup();
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
            var components = LoadComponents(Target);

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

        public static List<ComponentInfo> LoadComponents(EntityInfo info)
        {
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(info));
            var components = new List<ComponentInfo>();

            foreach (var asset in assets)
                if (asset is ComponentInfo component)
                    components.Add(component);

            components = components.OrderBy(o => o.name).ToList();
            return components;
        }
    }
}
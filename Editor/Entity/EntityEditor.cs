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

            var componentFields = GetComponentFields(Target);

            for (var i = 0; i < componentFields.Count; i++)
            {
                var componentField = componentFields[i];
                var componentRef = componentField.GetValue(Target) as InfoRef;

                // CHECK DUPLICATE

                if (componentRef.ID > 0)
                {
                    componentFields.RemoveAt(i);
                    i--;
                }
            }

            GUILayout.Space(10);

            var names = new string[componentFields.Count + 1];
            names[0] = "Select component to add...";

            for (var i = 0; i < componentFields.Count; i++)
                names[i + 1] = componentFields[i].Name;

            var selectedIndex = EditorGUILayout.Popup("Components", 0, names);

            if (selectedIndex > 0)
            {
                var selectedField = componentFields[selectedIndex - 1];
                var selectedRef = selectedField.GetValue(Target) as InfoRef;
                var component = AddComponent(Target, selectedRef.InfoType, selectedField.Name);

                selectedRef.ID = component.ID;

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
            var componentFields = GetComponentFields(Target);

            foreach (var componentField in componentFields)
            {
                var componentRef = componentField.GetValue(Target) as InfoRef;

                if (componentRef.ID == 0)
                    continue;

                foreach (var component in components)
                {
                    if (component.ID == componentRef.ID)
                    {
                        DrawComponent(Target, component, componentRef);
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
            InfoRef componentRef)
        {
            if (Foldouts == null)
                Foldouts = new Dictionary<string, bool>();

            var foldoutKey = $"{Target.GetType().Name}_{component.ShortName}";
            var show = Foldouts.GetItem(foldoutKey);
            var componentType = component.GetType();

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
            show = GUILayout.Toggle(show, (show ? "[-] " : "[+] ") + component.ShortName, "Button");
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

            Foldouts.SetItem(foldoutKey, show);

            if (remove)
                RemoveComponent(Target, component, componentRef);
        }

        static void RemoveComponent(
            EntityInfo Target,
            ComponentInfo component,
            InfoRef componentRef)
        {
            if (componentRef != null)
                componentRef.ID = 0;

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
            var isDirty = false;
            var componentFields = GetComponentFields(Target);
            var components = LoadComponents<ComponentInfo>(Target);

            foreach (var componentField in componentFields)
            {
                var componentRef = componentField.GetValue(Target) as InfoRef;

                if (componentRef.ID == 0)
                    continue;

                // GET Component BY FIELD

                var componentByField = (ComponentInfo)null;

                foreach (var component in components)
                {
                    if (component.ID == componentRef.ID)
                    {
                        componentByField = component;
                        break;
                    }

                    if (InfoPostprocessor.OldIDs.TryGetValue(component.ID, out var old_id))
                    {
                        if (old_id == componentRef.ID)
                        {
                            componentByField = component;
                            componentRef.ID = component.ID;
                            isDirty = true;
                            break;
                        }
                    }
                }

                if (componentByField == null)
                {
                    // FIX Component ID

                    componentRef.ID = 0;
                    isDirty = true;
                }
                else
                {
                    // FIX Component NAME

                    var componentName = $"{Target.name}_{componentField.Name}";

                    if (componentByField.name != componentName)
                    {
                        componentByField.name = componentName;
                        isDirty = true;
                    }

                    // FIX ENTITY ID

                    if (componentByField.Entity.ID != Target.ID)
                    {
                        componentByField.Entity.ID = Target.ID;
                        isDirty = true;
                    }
                }
            }

            return isDirty;
        }

        // COMMON

        public static List<FieldInfo> GetComponentFields(EntityInfo info)
        {
            var result = new List<FieldInfo>();
            var refType = typeof(InfoRef);
            var componentType = typeof(ComponentInfo);

            foreach (var field in info.GetType().GetFields())
            {
                if (field.FieldType.IsSubclassOf(refType))
                {
                    var preset_ref = field.GetValue(info) as InfoRef;
                    var preset_type = preset_ref.InfoType;

                    if (preset_type.IsSubclassOf(componentType))
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
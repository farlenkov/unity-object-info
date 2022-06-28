using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityUtility;

namespace UnityObjectInfo
{
    internal class InfoPostprocessor : AssetPostprocessor
    {
        static bool has_changes;
        static ushort last_id;
        static internal Dictionary<ushort, ushort> OldIDs = new Dictionary<ushort, ushort>();

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            //Log.InfoEditor(
            //    "Postprocessor: {0} {1} {2} {3} \n\n [imported] \n {4} \n\n [deleted] \n {5} \n\n [moved] \n {6} \n\n [movedFromAssetPaths] \n {7} \n",
            //    importedAssets.Length,
            //    deletedAssets.Length,
            //    movedAssets.Length,
            //    movedFromAssetPaths.Length,
            //    string.Join("\n", importedAssets),
            //    string.Join("\n", deletedAssets),
            //    string.Join("\n", movedAssets),
            //    string.Join("\n", movedFromAssetPaths));

            has_changes = false;
            last_id = 0;

            foreach (string path in importedAssets)
                PostprocessObjectInfo(path);

            if (has_changes)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        static void PostprocessObjectInfo(string path)
        {
            var root_info = AssetDatabase.LoadAssetAtPath<ObjectInfo>(path);

            if (root_info == null)
                return;

            PostprocessObjectInfo(root_info);

            var sub_assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

            foreach (var sub_asset in sub_assets)
                if (sub_asset is ObjectInfo sub_info)
                    PostprocessObjectInfo(sub_info, root_info);
        }

        static void PostprocessObjectInfo(
            ObjectInfo info, 
            ObjectInfo root = null)
        {
            CheckID(info, root);

            var editor = Editor.CreateEditor(info) as InfoEditor;

            if (editor != null)
                has_changes = has_changes || editor.OnImport();
        }

        static void CheckID(ObjectInfo info, ObjectInfo root_info)
        {
            if (info.ID == 0)
            {
                SetID(info, root_info);
                Log.InfoEditor("[ObjectInfoPostprocessor] Add ID: 0 > {0} '{1}'", info.ID, info.name);
            }
            else
            {
                var other_infos = Resources.LoadAll<ObjectInfo>("");

                for (var i = 0; i < other_infos.Length; i++)
                {
                    var other_info = other_infos[i];

                    if (other_info.ID == info.ID &&
                        other_info != info)
                    {
                        var old_id = info.ID;
                        SetID(info, root_info);
                        
                        OldIDs.Add(info.ID, old_id);
                        Log.InfoEditor("[ObjectInfoPostprocessor] Change ID: {0} > {1} '{2}'", old_id, info.ID, info.name);
                    }
                }
            }
        }

        static void SetID(ObjectInfo info, ObjectInfo root_info)
        {
            info.ID = (ushort)(DateTime.UtcNow.Ticks % ushort.MaxValue);

            if (info.ID == last_id)
                info.ID = ++last_id;
            else
                last_id = info.ID;

            if (root_info != null)
                EditorUtility.SetDirty(root_info);
            else
                EditorUtility.SetDirty(info);

            has_changes = true;
        }
    }
}
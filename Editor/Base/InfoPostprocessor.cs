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
        static bool hasChanges;
        static int lastId;
        static internal Dictionary<int, int> OldIDs = new ();

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

            hasChanges = false;
            lastId = 0;

            foreach (string path in importedAssets)
                PostprocessObjectInfo(path);

            if (hasChanges)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        static void PostprocessObjectInfo(string path)
        {
            var rootInfo = AssetDatabase.LoadAssetAtPath<ObjectInfo>(path);

            if (rootInfo == null)
                return;

            PostprocessObjectInfo(rootInfo);

            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

            foreach (var sub_asset in subAssets)
                if (sub_asset is ObjectInfo sub_info)
                    PostprocessObjectInfo(sub_info, rootInfo);
        }

        static void PostprocessObjectInfo(
            ObjectInfo info,
            ObjectInfo root = null)
        {
            CheckID(info, root);

            var editor = Editor.CreateEditor(info) as InfoEditor;

            if (editor != null)
                hasChanges = hasChanges || editor.OnImport();
        }

        static void CheckID(ObjectInfo info, ObjectInfo rootInfo)
        {
            if (info.ID == 0)
            {
                SetID(info, rootInfo);
                Log.InfoEditor("[ObjectInfoPostprocessor] Add ID: 0 > {0} '{1}'", info.ID, info.name);
            }
            else
            {
                var otherInfos = Resources.LoadAll<ObjectInfo>("");

                for (var i = 0; i < otherInfos.Length; i++)
                {
                    var otherInfo = otherInfos[i];

                    if (otherInfo.ID == info.ID &&
                        otherInfo != info)
                    {
                        var oldId = info.ID;
                        SetID(info, rootInfo);

                        OldIDs.Add(info.ID, oldId);
                        Log.InfoEditor("[ObjectInfoPostprocessor] Change ID: {0} > {1} '{2}'", oldId, info.ID, info.name);
                    }
                }
            }
        }

        static void SetID(ObjectInfo info, ObjectInfo rootInfo)
        {
            info.ID = (int)(DateTime.UtcNow.Ticks % ushort.MaxValue);

            if (info.ID == lastId)
                info.ID = ++lastId;
            else
                lastId = info.ID;

            if (rootInfo != null)
                EditorUtility.SetDirty(rootInfo);
            else
                EditorUtility.SetDirty(info);

            hasChanges = true;
        }
    }
}
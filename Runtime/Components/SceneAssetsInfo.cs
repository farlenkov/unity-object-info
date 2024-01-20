using System;
using System.IO;
using UnityEngine;
using UnityUtility;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace UnityObjectInfo
{
    public class SceneAssetsInfo : ComponentInfo
    {
        [HideInInspector]
        public string[] SceneNames;

#if UNITY_EDITOR

        public SceneAsset[] SceneAssets;

        void OnValidate()
        {
            if (SceneAssets != null)
            {
                SceneNames = new string[SceneAssets.Length];

                for (var i = 0; i < SceneAssets.Length; i++)
                {
                    var sceneAsset = SceneAssets[i];
                    var scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                    var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    SceneNames[i] = sceneName;
                }
            }
        }

        [ContextMenu("Load All")]
        public void LoadAll()
        {
            for (var i = 0; i < SceneAssets.Length; i++)
            {
                if (i == 0)
                    SceneAssets[i].OpenSingle();
                else
                    SceneAssets[i].OpenAdditive();
            }
        }

#endif
    }

    [Serializable]
    public class SceneAssetsInfoRef : InfoRef<SceneAssetsInfo> { }

    public class SceneAssetsInfoList : InfoList<SceneAssetsInfo> { }
}
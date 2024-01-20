using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityUtility;

namespace UnityObjectInfo
{
    public abstract class SceneAssetsWindow<T> : BaseWindow where T : EntityInfo
    {
        protected int SelectedEntityID;

        protected abstract SceneAssetsInfo[] GetScenes(T selectedEntity);

        protected virtual bool ShowDisabledEntityAssets => true;
        protected virtual Color SelectedColor => new Color(1.0f, 0.5f, 0);

        void OnGUI()
        {
            var entityAssets = GetEntityAssets();

            if (entityAssets.Length == 0)
                return;

            VerticalScroll(() => 
            {
                // ENTITY

                var selectedEntity = (T)null;
                var isOpenAllClicked = false;

                foreach (var entityAsset in entityAssets)
                {
                    if (!entityAsset.Enabled &&
                        !ShowDisabledEntityAssets)
                        continue;

                    if (SelectedEntityID == entityAsset.ID)
                        GUI.color = SelectedColor;

                    Horizontal(() => 
                    {
                        ButtonAlignment(TextAnchor.MiddleLeft);

                        if (Button(entityAsset.name))
                            SelectedEntityID = entityAsset.ID;

                        ButtonAlignment(TextAnchor.MiddleCenter);

                        if (Button("Open All", 70f))
                        {
                            isOpenAllClicked = true;
                            OpenAll(entityAsset);
                        }
                    });

                    if (SelectedEntityID == entityAsset.ID)
                        selectedEntity = entityAsset;

                    GUI.color = Color.white;
                }

                if (isOpenAllClicked)
                    return;

                if (selectedEntity == null)
                {
                    selectedEntity = entityAssets[0];
                    SelectedEntityID = selectedEntity.ID;
                }

                // SCENES

                GUILayout.Space(10f);
                GUILayout.Label(selectedEntity.name, EditorStyles.centeredGreyMiniLabel);

                var scenesByEntity = GetScenes(selectedEntity);

                foreach (var scenes in scenesByEntity)
                {
                    foreach (var scene in scenes.SceneAssets)
                    {
                        if (SceneButton(scene))
                            return;
                    }

                    GUILayout.Space(10f);
                }
            });
        }

        protected T[] GetEntityAssets()
        {
            return Resources.LoadAll<T>("");
        }

        bool SceneButton(SceneAsset scene)
        {
            var clicked = false;

            Horizontal(() =>
            {
                ButtonAlignment(TextAnchor.MiddleLeft);

                if (Button(scene.name))
                {
                    scene.OpenSingle();
                    clicked = true;
                }

                if (!clicked)
                {
                    ButtonAlignment(TextAnchor.MiddleCenter);

                    if (Button("+", 40f))
                        scene.OpenAdditive();
                }
            });

            return clicked;
        }

        void OpenAll(T entityAsset)
        {
            var scenesByEntity = GetScenes(entityAsset);
            var isFirst = true;

            foreach (var scenes in scenesByEntity)
            {
                foreach (var scene in scenes.SceneAssets)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        scene.OpenSingle();
                    }
                    else
                    {
                        scene.OpenAdditive();
                    }
                }
            }
        }
    }
}

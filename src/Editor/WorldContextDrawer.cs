﻿using System;
using UnityEditor;
using UnityEngine;

namespace npg.tomatoecs.Editor
{
    [CustomEditor(typeof(WorldContextObject))]
    public class WorldContextDrawer : UnityEditor.Editor
    {
        private uint _currentEntityId;
        private Entity _currentEntity;
        private bool _isValueSelected;
        private Type _selectedValue;
        private GUIStyle _coloredBoxStyle;
        private bool _hashSetFoldout;

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            if (_coloredBoxStyle == null)
            {
                _coloredBoxStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = MakeTex(1, 1, Color.grey) }
                };
            }


            var contextObject = (WorldContextObject)target;

            GUILayout.BeginHorizontal();
            var intEntityId = (int)_currentEntityId;
            intEntityId = EditorGUILayout.IntField("Current Entity Id", intEntityId);
            if (intEntityId < 0)
            {
                intEntityId = 0;
            }

            _currentEntityId = (uint)intEntityId;

            if (GUILayout.Button("Get Entity"))
            {
                _currentEntity = contextObject.Context.GetEntity(_currentEntityId);
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();
            if (_currentEntity.IsActive)
            {
                EntityDrawer.DrawEntity(_currentEntity);
            }

            GUILayout.EndVertical();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = tex.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = col;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
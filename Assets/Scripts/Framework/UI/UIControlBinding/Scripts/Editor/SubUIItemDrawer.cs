#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SkierFramework
{
    public class SubUIItemDrawer
    {
        private UIControlDataEditor     _container;
        private SubUIItemData           _itemData;
        private bool                    _foldout = true;

        public SubUIItemDrawer(UIControlDataEditor container, SubUIItemData itemData)
        {
            _container = container;
            _itemData = itemData;
        }

        public bool Draw()
        {
            Rect rect = EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("子UI名 ", UIControlDataEditor.skin.label);
                    _itemData.name = EditorGUILayout.TextField(_itemData.name, UIControlDataEditor.skin.textField).Trim();
                    EditorGUILayout.Space();
                    _foldout = EditorGUILayout.Foldout(_foldout, _foldout ? "收起" : "展开", true);

                    if (GUILayout.Button("+", EditorStyles.miniButton))
                    {
                        _container.AddSubUIAfter(this);
                        return false;
                    }

                    if (GUILayout.Button("-", EditorStyles.miniButton))
                    {
                        _container.RemoveSubUI(this);
                        return false;
                    }
                }
                EditorGUILayout.EndHorizontal();


                if (_foldout)
                {
                    EditorGUILayout.Space();
                    _itemData.subUIData = EditorGUILayout.ObjectField(_itemData.subUIData as Object, typeof(UIControlData), true) as UIControlData;
                }
            }
            EditorGUILayout.EndVertical();

            if (EditorGUIUtility.isProSkin)
                GUI.Box(new Rect(rect.x - 10f, rect.y - 5f, rect.width + 20f, rect.height + 15f), "");
            else
                GUI.Box(new Rect(rect.x - 10f, rect.y - 5f, rect.width + 20f, rect.height + 15f), "", UIControlDataEditor.skin.box);

            PostProcess();
            return true;
        }

        private void PostProcess()
        {
            // 默认将控件的名字作为变量名
            if (_itemData.subUIData != null && string.IsNullOrEmpty(_itemData.name))
                _itemData.name = _itemData.subUIData.name.Trim();
        }
    }

}
#endif

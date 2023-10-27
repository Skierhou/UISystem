#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace SkierFramework
{
    public class ControlItemDrawer
    {
        private UIControlDataEditor     _container;
        private CtrlItemData            _itemData;
        private bool                    _foldout = true;
        private int                     _controlTypeIdx = 0;

        public ControlItemDrawer(UIControlDataEditor container, CtrlItemData item)
        {
            _container = container;
            _itemData = item;
        }

        public bool Draw()
        {
            EditorGUI.BeginChangeCheck();
            Rect rect = EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("变量名 ", UIControlDataEditor.skin.label, GUILayout.Width(60f));
                string newName = EditorGUILayout.TextField(_itemData.name, UIControlDataEditor.skin.textField).Trim();

                if (newName != _itemData.name)
                {
                    _itemData.name = newName;
                    (_container.target as UIControlData).SetDirty();
                }

                EditorGUILayout.Space();
                _foldout = EditorGUILayout.Foldout(_foldout, _foldout ? "收起" : "展开", true);

                if (GUILayout.Button("+", EditorStyles.miniButton))
                {
                    _container.AddControlAfter(this);
                    return false;
                }

                if (GUILayout.Button("-", EditorStyles.miniButton))
                {
                    _container.RemoveControl(this);
                    return false;
                }
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            // 控件列表
            if (_foldout)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("变量类型 ", UIControlDataEditor.skin.label, GUILayout.Width(60f));

                    if (_controlTypeIdx == 0 && !string.IsNullOrEmpty(_itemData.type))
                        _controlTypeIdx = FindTypeIdx(_itemData.type);

                    if (EditorGUI.EndChangeCheck())
                    {
                        (_container.target as UIControlData).SetDirty();
                    }

                    EditorGUI.BeginChangeCheck();
                    _controlTypeIdx = EditorGUILayout.Popup(_controlTypeIdx, _container.allTypeNames, UIControlDataEditor.popupAlignLeft);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if(_controlTypeIdx != 0)
                        {
                            if (!ChangeControlsTypeTo(_controlTypeIdx))
                                _controlTypeIdx = 0; // 切换失败，重置回自动
                        }
                        else // 被主动设置为了自动
                            _itemData.type = string.Empty;

                        (_container.target as UIControlData).SetDirty();
                        return false;
                    }

                    EditorGUI.BeginChangeCheck();
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();


                EditorGUILayout.Space();
                for (int i = 0, imax = _itemData.targets.Length; i < imax; i++)
                {
                    Object obj = _itemData.targets[i];
                    EditorGUILayout.BeginHorizontal();

                    EditorGUI.BeginChangeCheck();
                    _itemData.targets[i] = EditorGUILayout.ObjectField(obj, typeof(Object), true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        (_container.target as UIControlData).SetDirty();
                    }

                    EditorGUILayout.Space(); EditorGUILayout.Space(); EditorGUILayout.Space();
                    if (GUILayout.Button("+", EditorStyles.miniButton))
                    {
                        InsertItem(i + 1);
                        return false;
                    }
                    if (GUILayout.Button("-", EditorStyles.miniButton))
                    {
                        if(_itemData.targets.Length == 1)
                        {
                            Debug.LogError("至少应保留一个控件");
                            return false;
                        }
                        else
                        {
                            RemoveItem(i);
                            return false;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
            }
        

            EditorGUILayout.EndVertical();

            if (EditorGUIUtility.isProSkin)
                GUI.Box(new Rect(rect.x - 10f, rect.y - 5f, rect.width + 20f, rect.height + 15f), "");
            else
                GUI.Box(new Rect(rect.x - 10f, rect.y - 5f, rect.width + 20f, rect.height + 15f), "", UIControlDataEditor.skin.box);

            if (EditorGUI.EndChangeCheck())
            {
                (_container.target as UIControlData).SetDirty();
            }

            PostProcess();
            return true;
        }

        private void PostProcess()
        {
            // 默认将新添加的第一个控件的名字作为变量名
            if (_itemData.targets.Length > 0 && _itemData.targets[0] != null && string.IsNullOrEmpty(_itemData.name))
                _itemData.name = _itemData.targets[0].name.Trim();
        }

        private int FindTypeIdx(string typeName)
        {
            string[] allTypeNames = _container.allTypeNames;
            for (int i = 0, imax = allTypeNames.Length; i < imax; i++)
            {
                if(allTypeNames[i] == typeName)
                    return i;
            }
            return 0;
        }

        private void InsertItem(int idx)
        {
            Object[] newArr = new Object[_itemData.targets.Length + 1];
            for(int i = 0; i < idx; i++)
            {
                newArr[i] = _itemData.targets[i];
            }
            newArr[idx] = null;
            for(int i = idx + 1; i < newArr.Length; i++)
            {
                newArr[i] = _itemData.targets[i - 1];
            }

            _itemData.targets = newArr;
        }

        private void RemoveItem(int idx)
        {
            Object[] newArr = new Object[_itemData.targets.Length - 1];
            for(int i = 0; i < idx; i++)
            {
                newArr[i] = _itemData.targets[i];
            }

            for(int i = idx; i < newArr.Length; i++)
            {
                newArr[idx] = _itemData.targets[i + 1];
            }

            _itemData.targets = newArr;
        }

        /// <summary>
        /// 将控件切换到指定类型
        /// </summary>
        /// <param name="typeIdx"></param>
        private bool ChangeControlsTypeTo(int typeIdx)
        {
            System.Type targetType = _container.allTypes[typeIdx];
            string targetTypeName = _container.allTypeNames[typeIdx];
            bool isGameObject = targetType == typeof(GameObject);


            for(int i = 0, imax = _itemData.targets.Length; i < imax; i++)
            {
                Object obj = _itemData.targets[i];
                if (obj == null)
                {
                    Debug.LogErrorFormat("[{0}.{1}] control[{2}] is null"
                        , _container.target.name, _itemData.name, i);
                    return false;
                }

                if(obj.GetType() != typeof(GameObject))
                {
                    if((obj as Component) == null)
                    {
                        Debug.LogErrorFormat("[{0}.{1}] control[{2}] [{3}] must be GameObject or a Component"
                            , _container.target.name, _itemData.name, i, obj.name);
                        return false;
                    }
                    obj = (obj as Component).gameObject;
                }

                GameObject go = obj as GameObject;
                if (isGameObject)
                    _itemData.targets[i] = go;
                else
                {
                    Component comp = go.GetComponent(targetType);
                    if(comp == null)
                    {
                        Debug.LogErrorFormat("[{0}.{1}] control[{2}] [{3}] isn't a {4}"
                            , _container.target.name, _itemData.name, i, go.name, targetTypeName);
                        return false;
                    }
                    _itemData.targets[i] = comp;
                }
            }

            _itemData.type = targetTypeName;
            return true;
        }
    }

}
#endif

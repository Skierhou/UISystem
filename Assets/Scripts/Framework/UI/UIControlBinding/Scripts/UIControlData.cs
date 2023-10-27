/*
    URL: https://github.com/Misaka-Mikoto-Tech/UIControlBinding
    使用方法:
    UE: 将此脚本添加到UI根节点，与程序协商好需要绑定的控件及其变量名后，将需要绑定的控件拖到脚本上
    程序: 点此脚本右上角的齿轮，点 "复制代码到剪贴板" 按钮

    UIManager 加载示例：
    `` C#
        IBindableUI uiA = Activator.CreateInstance(Type.GetType("UIA")) as IBindableUI;
        GameObject prefab = Resources.Load<GameObject>("UI/UIA"); // you can get ui config from config file
        GameObject go = Instantiate(prefab);
        UIControlData ctrlData = go.GetComponent<UIControlData>();
        if(ctrlData != null)
        {
            ctrlData.BindDataTo(uiA);
        }
    ``

 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
#if XLUA
using XLua;
#endif
using UnityEngine.Profiling;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Playables;
#endif

namespace SkierFramework
{
    /// <summary>
    /// 单个控件数据
    /// </summary>
    [Serializable]
    public class CtrlItemData
    {
        public string                       name = string.Empty;
#if UNITY_EDITOR
        [HideInInspector]
        public string                       type = string.Empty;
#endif
        public UnityEngine.Object[]         targets = new UnityEngine.Object[1];

        public override string ToString()
        {
            return name;
        }
    }

    /// <summary>
    /// 单个子UI数据
    /// </summary>
    [Serializable]
    public class SubUIItemData
    {
        public string           name                = string.Empty;
        public UIControlData    subUIData           = null;

        public override string ToString()
        {
            return name;
        }
    }

    /// <summary>
    /// 被绑定的UI类字段信息
    /// </summary>
    public class UIFieldsInfo
    {
        public Type type;
        public List<FieldInfo> controls = new List<FieldInfo>(10);
        public List<FieldInfo> subUIs = new List<FieldInfo>();
    }

    /// <summary>
    /// 当前UI所有的绑定数据以及子UI指定
    /// </summary>
    [DisallowMultipleComponent]
    public class UIControlData : MonoBehaviour
    {
        /// <summary>
        /// 所有绑定的组件，不允许重名
        /// </summary>
        public List<CtrlItemData>        ctrlItemDatas;
        /// <summary>
        /// 子UI数据
        /// </summary>
        public List<SubUIItemData>       subUIItemDatas;

        /// <summary>
        /// 被绑定的UI
        /// </summary>
        public List<WeakReference<IBindableUI>> bindUIRefs;

        /// <summary>
        /// 缓存所有打开过的UI类型的字段数据（如果有需求可以在特定时机清理以节约内存）
        /// </summary>
        public static Dictionary<Type, UIFieldsInfo> s_uiFieldsCache = new Dictionary<Type, UIFieldsInfo>();

#region Editor
#if UNITY_EDITOR
        /// <summary>
        /// 已知类型列表，自定义类型可以添加到下面指定区域
        /// </summary>
        private static Dictionary<string, Type> _typeMap = new Dictionary<string, Type>()
        {
            { "TextMeshProUGUI", typeof(TMPro.TextMeshProUGUI) },
            { "TextMeshPro", typeof(TMPro.TextMeshPro) },
            { "TMP_InputField", typeof(TMPro.TMP_InputField) },
            { "TMP_Dropdown", typeof(TMPro.TMP_Dropdown) },
            { "Text", typeof(Text)},
            { "RawImage", typeof(RawImage)},
            { "Button", typeof(Button)},
            { "Toggle", typeof(Toggle)},
            { "Slider", typeof(Slider)},
            { "Scrollbar", typeof(Scrollbar)},
            { "Dropdown", typeof(Dropdown)},
            { "InputField", typeof(InputField)},
            { "Canvas", typeof(Canvas)},
            { "UIScrollView", typeof(UIScrollView) },
            { "ScrollRect", typeof(ScrollRect)},
            { "SpriteRenderer", typeof(SpriteRenderer)},
            { "GridLayoutGroup", typeof(GridLayoutGroup) },
            { "Animation", typeof(Animation) },
            { "VideoPlayer", typeof(UnityEngine.Video.VideoPlayer) },
            { "CanvasGroup", typeof(CanvasGroup) },
            { "PlayableDirector", typeof(PlayableDirector) },
            { "UITweener", typeof(UITweener) },

            ////////自定义控件类型请放这里////////



            //////////////////////////////////////

            { "Image", typeof(Image)},
            { "RectTransform", typeof(RectTransform)},
            { "Transform", typeof(Transform)},
            { "GameObject", typeof(GameObject)},
        };

        public static string[] GetAllTypeNames()
        {
            string[] keys = new string[_typeMap.Count + 1];
            keys[0] = "自动";
            _typeMap.Keys.CopyTo(keys, 1);
            return keys;
        }

        public static Type[] GetAllTypes()
        {
            Type[] types = new Type[_typeMap.Count + 1];
            types[0] = typeof(UnityEngine.Object);
            _typeMap.Values.CopyTo(types, 1);
            return types;
        }
#endif
#endregion

#region BindDataToC#UI
        /// <summary>
        /// 将当前数据绑定到某窗口类实例的字段，UI 加载后必须被执行
        /// </summary>
        /// <param name="ui">需要绑定数据的 UI</param>
        public void BindDataTo(IBindableUI ui)
        {
            if (ui == null)
                return;

#if DEBUG_LOG
            float time = Time.realtimeSinceStartup;
            Profiler.BeginSample("BindDataTo");
#endif
            UIFieldsInfo fieldInfos = GetUIFieldsInfo(ui.GetType());
            
            var controls = fieldInfos.controls;
            for (int i = 0, imax = controls.Count; i < imax; i++)
            {
                try
                {
                    BindCtrl(ui, controls[i]);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            var subUIs = fieldInfos.subUIs;
            for (int i = 0, imax = subUIs.Count; i < imax; i++)
                BindSubUI(ui, subUIs[i]);

            if (bindUIRefs == null)
                bindUIRefs = new List<WeakReference<IBindableUI>>();

            bindUIRefs.Add(new WeakReference<IBindableUI>(ui));

#if DEBUG_LOG
            Profiler.EndSample();
            float span = Time.realtimeSinceStartup - time;
            if (span > 0.002f)
                Debug.LogWarningFormat("BindDataTo {0} 耗时{1}ms", ui.GetType().Name, span * 1000f);
#endif
        }

        private void BindCtrl(IBindableUI ui, FieldInfo fi)
        {
            int itemIdx = GetCtrlIndex(fi.Name);
            if (itemIdx == -1)
            {
                Debug.LogWarningFormat("can not find binding control of name [{0}] in prefab", fi.Name);
                return;
            }

            var objs = ctrlItemDatas[itemIdx];

            Type fieldType = fi.FieldType;
            if (fieldType.IsArray)
            {
                Array arrObj = Array.CreateInstance(fieldType.GetElementType(), objs.targets.Length);

                // 给数组元素设置数据
                for (int j = 0, jmax = objs.targets.Length; j < jmax; j++)
                {
                    if (objs.targets[j] != null)
                        arrObj.SetValue(objs.targets[j], j);
                    else
                        Debug.LogErrorFormat("Component {0}[{1}] is null", objs.name, j);
                }
                fi.SetValue(ui, arrObj);
            }
            else
            {
                UnityEngine.Object component = GetComponent(itemIdx);
                if (component != null)
                    fi.SetValue(ui, component);
                else
                    Debug.LogErrorFormat("Component {0} is null", objs.name);
            }
        }

        private void BindSubUI(IBindableUI ui, FieldInfo fi)
        {
            int subUIIdx = GetSubUIIndex(fi.Name);
            if(subUIIdx == -1)
            {
                Debug.LogErrorFormat("can not find binding subUI of name [{0}] in prefab", fi.Name);
                return;
            }

            fi.SetValue(ui, subUIItemDatas[subUIIdx].subUIData);
        }

        /// <summary>
        /// 获取指定UI类的字段信息
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static UIFieldsInfo GetUIFieldsInfo(Type type)
        {
            UIFieldsInfo uIFieldsInfo;
            if (s_uiFieldsCache.TryGetValue(type, out uIFieldsInfo))
                return uIFieldsInfo;

            uIFieldsInfo = new UIFieldsInfo() { type = type };
            FieldInfo[] fis = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for(int i = 0, imax = fis.Length; i < imax; i++)
            {
                FieldInfo fi = fis[i];

                if (fi.IsDefined(typeof(ControlBindingAttribute), false))
                    uIFieldsInfo.controls.Add(fi);
                else if (fi.IsDefined(typeof(SubUIBindingAttribute), false))
                    uIFieldsInfo.subUIs.Add(fi);
            }

            s_uiFieldsCache.Add(type, uIFieldsInfo);

            return uIFieldsInfo;
        }
#endregion

#region BindDataToLuaTable

#if XLUA
        public void BindDataToLua(IBindableUI ui, LuaTable luaTable)
        {
            if (luaTable == null)
                return;

            foreach(var itemData in ctrlItemDatas)
            {
                var targets = itemData.targets;
                if(targets.Length == 0)
                {
                    Debug.LogErrorFormat("control {0} is null", itemData.name);
                    continue;
                }

                if(targets.Length == 1)
                {
                    if (targets[0] != null)
                        luaTable.Set(itemData.name, itemData.targets[0]);
                    else
                        Debug.LogErrorFormat("Component {0} is null", itemData.name);
                }
                else
                {
                    LuaTable tmpTbl = luaTable.env.NewTable();
                    for(int i = 0, imax = targets.Length; i < imax; i++)
                    {
                        if (targets[i] != null)
                            tmpTbl.Set(i + 1, targets[i]);
                        else
                            Debug.LogErrorFormat("Component {0}[{1}] is null", itemData.name, i);
                    }

                    luaTable.Set(itemData.name, tmpTbl);
                }
            }

            foreach(var subUI in subUIItemDatas)
            {
                luaTable.Set(subUI.name, subUI.subUIData);
            }

            if (bindUIRefs == null)
                bindUIRefs = new List<WeakReference<IBindableUI>>();

            bindUIRefs.Add(new WeakReference<IBindableUI>(ui));
        }
#endif
#endregion

#region UnBind
        private static List<UIControlData> s_tmpControlDataForUnbind = new List<UIControlData>();
        /// <summary>
        /// 解除指定UI及其子节点自动绑定字段的引用
        /// </summary>
        /// <param name="uiGo"></param>
        public static void UnBindUI(GameObject uiGo)
        {
            if (uiGo == null)
                return;

#if DEBUG_LOG
            float time = Time.realtimeSinceStartup;
            Profiler.BeginSample("UnBindUI");
#endif

            uiGo.GetComponentsInChildren(true, s_tmpControlDataForUnbind);
            for (int i = 0, imax = s_tmpControlDataForUnbind.Count; i < imax; i++)
            {
                UIControlData controlData = s_tmpControlDataForUnbind[i];
                if (controlData.bindUIRefs == null)
                    continue;

                List<WeakReference<IBindableUI>> bindUIRefs = controlData.bindUIRefs;
                for (int j = 0, jmax = bindUIRefs.Count; j < jmax; j++)
                {
                    WeakReference<IBindableUI> bindUIRef = bindUIRefs[j];
                    IBindableUI bindUI;
                    if (!bindUIRef.TryGetTarget(out bindUI))
                        continue;

                    LuaViewRunner luaViewRunner = bindUI as LuaViewRunner;
                    if (luaViewRunner == null)
                    {
                        UIFieldsInfo fieldInfos = GetUIFieldsInfo(bindUI.GetType());
                        var controls = fieldInfos.controls;
                        for (int k = 0, kmax = controls.Count; k < kmax; k++)
                            controls[k].SetValue(bindUI, null);

                        var subUIs = fieldInfos.subUIs;
                        for (int k = 0, kmax = subUIs.Count; k < kmax; k++)
                            subUIs[k].SetValue(bindUI, null);
                    }
#if XLUA
                    else
                    {
                        LuaTable luaTable = luaViewRunner.luaUI;
                        if (luaTable == null)
                            continue;

                        List<CtrlItemData> ctrlItemData = controlData.ctrlItemDatas;
                        for(int k = 0, kmax = ctrlItemData.Count; k < kmax; k++)
                        {
                            CtrlItemData itemData = ctrlItemData[k];
                            luaTable.Set<string, object>(itemData.name, null);
                        }

                        List<SubUIItemData> subUIItemDatas = controlData.subUIItemDatas;
                        for (int k = 0, kmax = subUIItemDatas.Count; k < kmax; k++)
                        {
                            SubUIItemData subUIItemData = subUIItemDatas[k];
                            luaTable.Set<string, object>(subUIItemData.name, null);
                        }
                    }
#endif
                }

                controlData.bindUIRefs = null;
            }
            s_tmpControlDataForUnbind.Clear();

#if DEBUG_LOG
            Profiler.EndSample();
            float span = Time.realtimeSinceStartup - time;
            if (span > 0.002f)
                Debug.LogWarningFormat("UnBindUI {0} 耗时{1}ms", uiGo.name, span * 1000f);
#endif
        }
        #endregion

        #region Get,不建议使用

        /// <summary>
        /// 找到指定名称的第一个组件, 不存在返回 null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetComponent<T>(string name) where T : Component
        {
            int idx = GetCtrlIndex(name);
            if (idx == -1)
                return null;

            var targets = ctrlItemDatas[idx].targets;
            if (targets.Length == 0)
                return null;

            return targets[0] as T;
        }

        public new UnityEngine.Object GetComponent(string name)
        {
            int idx = GetCtrlIndex(name);
            if (idx == -1)
                return null;

            var targets = ctrlItemDatas[idx].targets;
            if (targets.Length == 0)
                return null;

            return targets[0];
        }

        public UnityEngine.Object GetComponent(int idx)
        {
            if (idx == -1 || idx >= ctrlItemDatas.Count)
                return null;

            var targets = ctrlItemDatas[idx].targets;
            if (targets.Length == 0)
                return null;

            return targets[0];
        }

        public UnityEngine.Object[] GetComponents(string name)
        {
            int idx = GetCtrlIndex(name);
            if (idx == -1)
                return null;

            return ctrlItemDatas[idx].targets;
        }

        public UnityEngine.Object[] GetComponents(int idx)
        {
            if (idx == -1 || idx >= ctrlItemDatas.Count)
                return null;

            return ctrlItemDatas[idx].targets;
        }



        private int  GetCtrlIndex(string name)
        {
            for (int i = 0, imax = ctrlItemDatas.Count; i < imax; i++)
            {
                CtrlItemData item = ctrlItemDatas[i];
                if (item.name == name)
                    return i;
            }
            return -1;
        }

        private int GetSubUIIndex(string name)
        {
            for(int i = 0, imax = subUIItemDatas.Count; i < imax; i++)
            {
                SubUIItemData item = subUIItemDatas[i];
                if (item.name == name)
                    return i;
            }
            return -1;
        }

#endregion

#region For Editor
#if UNITY_EDITOR

        public bool dataHasChanged = false;

        public bool CorrectComponents()
        {
            if (ctrlItemDatas == null) return true;

            bool isOK = true;
            for(int i = 0, imax = ctrlItemDatas.Count; i < imax; i++)
            {
                if (string.IsNullOrEmpty(ctrlItemDatas[i].name)) // TODO Check if is a valid varible name
                {
                    Debug.LogErrorFormat("[{1}]第 {0} 个控件没有名字，请修正", i + 1, gameObject.name);
                    return false;
                }

                for (int j = ctrlItemDatas.Count - 1; j >= 0; j--)
                {
                    if(ctrlItemDatas[i].name == ctrlItemDatas[j].name && i != j)
                    {
                        Debug.LogErrorFormat("[{3}]控件名字 [{0}] 第 {1} 项与第 {2} 项重复，请修正", ctrlItemDatas[i].name, i + 1, j + 1, gameObject.name);
                        return false;
                    }
                }
            }

            isOK = ReplaceTargetsToUIComponent();
            if(isOK)
                Debug.LogFormat("[{0}]控件绑定修正完毕", gameObject.name);

            return isOK;
        }

        public bool CheckSubUIs()
        {
            for (int i = 0, imax = subUIItemDatas.Count; i < imax; i++)
            {
                var subUI = subUIItemDatas[i];
                if(subUI != null)
                {
                    if (string.IsNullOrEmpty(subUI.name))
                    {
                        Debug.LogErrorFormat("[{0}]第 {1} 个子UI没有设置名字, 请修正", gameObject.name, i + 1);
                        return false;
                    }

                    if(subUI.subUIData == null)
                    {
                        Debug.LogErrorFormat("[{0}]第 {1} 个子UI没有赋值, 请修正", gameObject.name, i + 1);
                        return false;
                    }

                    // 必须拖当前 Prefab 下的子UI
                    if (!IsInCurrentPrefab(subUI.subUIData.transform))
                    {
                        Debug.LogErrorFormat("[{0}]第 {1} 个子UI [{2}]不是当前 Prefab 下的对象，请修正", gameObject.name, i + 1, subUI.name);
                        return false;
                    }
                }
                else
                {
                    Debug.LogError("internal error at ControlBinding, pls contact author");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 由于自动拖上去的对象永远都是 GameObject，所以我们需要把它修正为正确的对象类型
        /// </summary>
        private bool ReplaceTargetsToUIComponent()
        {
            for (int i = 0, imax = ctrlItemDatas.Count; i < imax; i++)
            {
                var objs = ctrlItemDatas[i].targets;
                Type type = null;
                for(int j = 0, jmax = objs.Length; j < jmax; j++)
                {
                    if(objs[j] == null)
                    {
                        Debug.LogErrorFormat("[{2}]控件名字 [{0}] 第 {1} 项为空，请修正", ctrlItemDatas[i].name, j + 1, gameObject.name);
                        return false;
                    }

                    GameObject go = objs[j] as GameObject;
                    if (go == null)
                        go = (objs[j] as Component).gameObject;

                    // 必须拖当前 Prefab 下的控件
                    if (!IsInCurrentPrefab(go.transform))
                    {
                        Debug.LogErrorFormat("[{2}]控件名字 [{0}] 第 {1} 项不是当前 Prefab 下的控件，请修正", ctrlItemDatas[i].name, j + 1, gameObject.name);
                        return false;
                    }

                    UnityEngine.Object correctComponent = FindCorrectComponent(go, ctrlItemDatas[i].type);
                    if(correctComponent == null)
                    {
                        Debug.LogErrorFormat("[{3}]控件 [{0}] 第 {1} 项不是 {2} 类型，请修正", ctrlItemDatas[i].name, j + 1, ctrlItemDatas[i].type, gameObject.name);
                        return false;
                    }

                    
                    if (type == null) // 当前变量的第一个控件时执行
                    {
                        if (string.IsNullOrEmpty(ctrlItemDatas[i].type))
                        {
                            type = correctComponent.GetType();
                        }else
                        {
                            if(!_typeMap.TryGetValue(ctrlItemDatas[i].type, out type))
                            {
                                Debug.LogError("Internal Error, pls contact author");
                                return false;
                            }
                        }
                    }
                    else if(correctComponent.GetType() != type && !correctComponent.GetType().IsSubclassOf(type))
                    {
                        Debug.LogErrorFormat("[{2}]控件名字 [{0}] 第 {1} 项与第 1 项的类型不同，请修正", ctrlItemDatas[i].name, j + 1, gameObject.name);
                        return false;
                    }

                    if (objs[j] != correctComponent)
                        dataHasChanged = true;

                    objs[j] = correctComponent;
                }

                if(type.Name != ctrlItemDatas[i].type)
                {
                    ctrlItemDatas[i].type = type.Name;
//#if UNITY_2019_1_OR_NEWER
//                    EditorUtility.ClearDirty(this);
//#endif
                    EditorUtility.SetDirty(this);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                }
                ctrlItemDatas[i].type = type.Name;
            }
            return true;
        }

        private bool IsInCurrentPrefab(Transform t)
        {
            do
            {
                if (t == transform)
                    return true;
                t = t.parent;
            } while (t != null);
            return false;
        }

        private UnityEngine.Object FindCorrectComponent(GameObject go, string typename)
        {
            if (typename == "GameObject")
                return go;

            List<Component> components = new List<Component>();
            go.GetComponents(components);

            Func<Type, Component> getSpecialTypeComp = (Type t) =>
            {
                foreach (var comp in components)
                {
                    Type compType = comp.GetType();
                    if (compType == t || compType.IsSubclassOf(t))
                    {
                        return comp;
                    }
                }
                return null;
            };

            Component newComp = null;

            if (string.IsNullOrEmpty(typename))
            {
                // 类型名为空则为自动类型，在 _typeMap 里从上往下找
                foreach (var kv in _typeMap)
                {
                    newComp = getSpecialTypeComp(kv.Value);
                    if (newComp != null)
                        break;
                }
            }
            else
            {// 指定了类型名则只找指定类型的控件
                Type type = null;
                if (_typeMap.TryGetValue(typename, out type))
                {
                    newComp = getSpecialTypeComp(type);
                }
            }

            return newComp;
        }

        private bool IsNeedSave()
        {
            foreach(var ctrl in ctrlItemDatas)
            {
                if (string.IsNullOrEmpty(ctrl.type))
                    return true;
            }
            return false;
        }


        [ContextMenu("复制代码到剪贴板(Private)")]
        public void CopyCodeToClipBoardPrivate()
        {
            CopyCodeToClipBoardImpl("private");
        }

        [ContextMenu("复制代码到剪贴板(Protected)")]
        public void CopyCodeToClipBoardProtected()
        {
            CopyCodeToClipBoardImpl("protected");
        }

        [ContextMenu("复制代码到剪贴板(Public)")]
        public void CopyCodeToClipBoardPublic()
        {
            CopyCodeToClipBoardImpl("public");
        }

        private void CopyCodeToClipBoardImpl(string accessLevel)
        {
            // 调用保存资源会导致 prefab 发生变化，因此只有有需要时才保存
            if (IsNeedSave())
                UIBindingPrefabSaveHelper.SavePrefab(gameObject);

            StringBuilder sb = new StringBuilder(1024);
            sb.AppendLine("#region 控件绑定变量声明，自动生成请勿手改");
            sb.AppendLine("\t\t#pragma warning disable 0649"); // 变量未赋值

            foreach (var ctrl in ctrlItemDatas)
            {
                if (ctrl.targets.Length == 0)
                    continue;

                if (ctrl.targets.Length == 1)
                    sb.AppendFormat("\t\t[ControlBinding]\r\n\t\t{0} {1} {2};\r\n", accessLevel, ctrl.type, ctrl.name);
                else
                    sb.AppendFormat("\t\t[ControlBinding]\r\n\t\t{0} {1}[] {2};\r\n", accessLevel, ctrl.type, ctrl.name);
            }

            sb.AppendLine();
            foreach(var subUI in subUIItemDatas)
            {
                sb.AppendFormat("\t\t[SubUIBinding]\r\n\t\t{0} UIControlData {1};\r\n", accessLevel, subUI.name);
            }
            sb.AppendLine("\t\t#pragma warning restore 0649");
            sb.Append("#endregion\r\n\r\n");

            UnityEngine.GUIUtility.systemCopyBuffer = sb.ToString();
        }

        [ContextMenu("复制代码到剪贴板(Lua)")]
        public void CopyCodeToClipBoardLua()
        {
            // 调用保存资源会导致 prefab 发生变化，因此只有有需要时才保存
            if (IsNeedSave())
                UIBindingPrefabSaveHelper.SavePrefab(gameObject);

            StringBuilder sb = new StringBuilder(1024);
            sb.Append("-- 控件绑定变量声明，自动生成请勿手改\r\n");

            foreach (var ctrl in ctrlItemDatas)
            {
                if (ctrl.targets.Length == 0)
                    continue;

                sb.AppendFormat("local {0}\r\n", ctrl.name);
            }

            sb.AppendFormat("\r\n");
            sb.AppendFormat("-- SubUI\r\n");
            foreach (var subUI in subUIItemDatas)
            {
                sb.AppendFormat("local {0}\r\n", subUI.name);
            }
            sb.Append("-- 控件绑定定义结束\r\n\r\n");

            UnityEngine.GUIUtility.systemCopyBuffer = sb.ToString();
        }


        public void SetDirty()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
#if UNITY_2021_1_OR_NEWER
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject);
#else
            var prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject);
#endif
            if (prefabStage != null)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
#endif
        }
#endif
        #endregion
    }

}

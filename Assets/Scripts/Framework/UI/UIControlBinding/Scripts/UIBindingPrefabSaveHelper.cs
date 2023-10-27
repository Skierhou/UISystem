#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;

#if UNITY_2019_1_OR_NEWER
#if UNITY_2020_1_OR_NEWER
using UnityEditor.SceneManagement;
#   else
using UnityEditor.Experimental.SceneManagement;
#   endif
#endif

namespace SkierFramework
{
    public class UIBindingPrefabSaveHelper : UnityEditor.AssetModificationProcessor
    {
        static UIBindingPrefabSaveHelper()
        {
#if UNITY_2019_1_OR_NEWER
            PrefabStage.prefabSaving += OnPrefabStageSaving;
#endif
        }

#if UNITY_2019_1_OR_NEWER
        /// <summary>
        /// 当点击Perfab编辑场景的Save按钮时修改数据不会立刻保存，因此需要在其执行前主动保存一下
        /// </summary>
        /// <param name="go"></param>
        static void OnPrefabStageSaving(GameObject go)
        {
            string path = AssetDatabase.GetAssetPath(go);
            OnWillSaveAssets(new string[] { path });
            AssetDatabase.SaveAssets();
        }
#endif
        /// <summary>
        /// 保存资源时修正控件绑定数据
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        static string[] OnWillSaveAssets(string[] paths)
        {
            GameObject goInHierarchy = Selection.activeGameObject;
            if(goInHierarchy != null)
            {
                // 从根节点开始遍历，以免当前prefab有多个子UI修改时无法被全部修正
                var rootTran = goInHierarchy.transform;
                while (rootTran.parent != null)
                    rootTran = rootTran.parent;

                UIControlData[] uiControlData = rootTran.GetComponentsInChildren<UIControlData>();
                if (uiControlData != null)
                {
                    foreach (var comp in uiControlData)
                    {
                        comp.CorrectComponents();
                        comp.CheckSubUIs();
                    }
                }
            }

            return paths;
        }

        public static void SavePrefab(GameObject goInHierarchy)
        {
            Object goPrefab = null;
            GameObject objValid = null;
            GameObject objToCheck = goInHierarchy;
            string prefabPath = null;

            do
            {
#if UNITY_2019_1_OR_NEWER
                var currPrefab = PrefabUtility.GetCorrespondingObjectFromSource(objToCheck);
#else
                var currPrefab = PrefabUtility.GetPrefabParent(objToCheck);
#endif

                if (currPrefab == null)
                    break;

                string currPath = AssetDatabase.GetAssetPath(currPrefab);
                if (prefabPath == null)
                    prefabPath = currPath;

                if (currPath != prefabPath) // 已经到root或者当前是嵌套prefab并且已经到达上一层prefab
                    break;

                goPrefab = currPrefab;
                objValid = objToCheck;

                var t = objToCheck.transform.parent;
                if (t != null)
                    objToCheck = t.gameObject;
                else
                    break;
            } while (true);

            if (objValid != null)
#if UNITY_2019_1_OR_NEWER
                goPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(objValid, prefabPath, InteractionMode.AutomatedAction);
#else
                PrefabUtility.ReplacePrefab(goInHierarchy, goPrefab, ReplacePrefabOptions.ConnectToPrefab);
#endif
            else
                Debug.LogFormat("<color=red>当前对象不属于Prefab, 请将其保存为 Prefab</color>");
        }
    }

}
#endif
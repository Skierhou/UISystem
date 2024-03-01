#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.IO;
using SkierFramework;
using System;
using Newtonsoft.Json;

public class UICreateWindow : EditorWindow
{
    #region MenuItem
    [MenuItem("Assets/Create/CreateUI")]
    private static void CopyUI()
    {
        if (Selection.activeObject == null || !(Selection.activeObject is GameObject))
        {
            Debug.LogError("请选择UI预制体!");
            return;
        }
        UICreateWindow.OpenWindow().uiPrefab = Selection.activeObject as GameObject;
    }

    [MenuItem("Tools/UI管理")]
    public static UICreateWindow OpenWindow()
    {
        var window = GetWindow<UICreateWindow>("UI管理");
        if (window == null)
        {
            window = CreateWindow<UICreateWindow>("UI管理");
        }
        window.name = "UI管理";
        window.Focus();
        return window;
    }
    #endregion

    private string m_Input;

    private Vector2 scroll;
    private Vector2 scroll2;
    private Vector2 scroll3;
    private string UIViewTemplate;
    private string UIConfig;
    private string UIType;
    private string saveUIPath;
    private string uiName;
    public GameObject uiPrefab;
    private Dictionary<string, string> uiNames = new Dictionary<string, string>();
    private Dictionary<string, UIConfigJson> uiJsonDatas = new Dictionary<string, UIConfigJson>();

    private bool isWindow = true;
    private UILayer layer = UILayer.NormalLayer;

    private void OnEnable()
    {
        TryGetPath(ref UIViewTemplate, nameof(UIViewTemplate), ".txt");
        TryGetPath(ref UIType, nameof(UIType), ".cs");
        TryGetPath(ref UIConfig, nameof(UIConfig), ".json");
        saveUIPath = PlayerPrefs.GetString(nameof(saveUIPath), "Assets/Scripts");

        uiJsonDatas.Clear();
        uiNames.Clear();
        string[] strs = Enum.GetNames(typeof(UIType));
        foreach (var str in strs)
        {
            if (str.Equals("Max")) continue;

            var jsonData = GetUIJson(str);
            if (jsonData == null || string.IsNullOrEmpty(jsonData.path)) continue;
            var scriptPath = GetUIScript(str, true);
            uiNames.AddOrUpdate(str, scriptPath);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("通过[Tools/UI管理]可以打开");
        scroll = EditorGUILayout.BeginScrollView(scroll);
        {
            EditorGUILayout.HelpBox("UI基础文件", MessageType.Info);
            {
                PathField("UI模板文件.txt", ref UIViewTemplate, nameof(UIViewTemplate), ".txt");
                PathField("UI配置文件.json", ref UIConfig, nameof(UIConfig), ".json");
                PathField("UIType.cs", ref UIType, nameof(UIType), ".cs");
            }
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            {
                scroll2 = EditorGUILayout.BeginScrollView(scroll2, "box", GUILayout.Width(position.width * 0.4f - 6));
                {
                    EditorGUILayout.HelpBox("已创建的UI", MessageType.Info);
                    m_Input = EditorGUILayout.TextField(m_Input, EditorStyles.toolbarSearchField, GUILayout.Height(20));
                    string[] strs = Enum.GetNames(typeof(UIType));
                    foreach (var str in strs)
                    {
                        if (str.Equals("Max")) continue;
                        if (!string.IsNullOrEmpty(m_Input) && !str.Contains(m_Input)) continue;

                        var jsonData = GetUIJson(str);
                        var scriptPath = GetUIScript(str);
                        if (jsonData == null || string.IsNullOrEmpty(jsonData.path) || string.IsNullOrEmpty(scriptPath)) continue;

                        var defaultColor = GUI.color;
                        if (str.Equals(uiName))
                        {
                            GUI.color = Color.yellow;
                        }
                        EditorGUILayout.BeginHorizontal("box");
                        if (GUILayout.Button("选中"))
                        {
                            uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(jsonData.path);
                        }
                        EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<GameObject>(jsonData.path), typeof(GameObject), true);
                        EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<TextAsset>(scriptPath), typeof(TextAsset), true);
                        EditorGUILayout.EndHorizontal();
                        GUI.color = defaultColor;
                    }
                }
                EditorGUILayout.EndScrollView();
                scroll3 = EditorGUILayout.BeginScrollView(scroll3, "box", GUILayout.Width(position.width * 0.6f - 6));
                {
                    EditorGUILayout.HelpBox("UI操作", MessageType.Info);
                    uiPrefab = EditorGUILayout.ObjectField("UI预制体", uiPrefab, typeof(GameObject), true) as GameObject;
                    if (uiPrefab != null)
                    {
                        uiName = uiPrefab.name;
                        var uiScriptPath = GetUIScript(uiName);
                        if (string.IsNullOrEmpty(uiScriptPath))
                        {
                            if (GUILayout.Button($"选择创建路径:{saveUIPath}"))
                            {
                                var newPath = EditorUtility.OpenFolderPanel("UI生成路径", saveUIPath, "");
                                saveUIPath = newPath.Replace(Application.dataPath, "Assets");
                                PlayerPrefs.SetString(nameof(saveUIPath), saveUIPath);
                            }
                            if (uiPrefab != null)
                            {
                                EditorGUILayout.TextField("UI生成路径", $"{saveUIPath}/{uiName}.cs");
                            }

                            isWindow = EditorGUILayout.Toggle("是否为窗口", isWindow);
                            layer = (UILayer)EditorGUILayout.EnumPopup("UILayer设置", layer);

                            var defaultColor = GUI.color;
                            GUI.color = Color.green;
                            if (GUILayout.Button("创建UI"))
                            {
                                // 生成代码
                                string str = Regex.Replace(File.ReadAllText(UIViewTemplate), "UIXXXView", uiName);
                                UIControlData uiControlData = uiPrefab.GetComponent<UIControlData>();
                                if (uiControlData != null)
                                {
                                    uiControlData.CopyCodeToClipBoardPrivate();
                                }
                                str = Regex.Replace(str, "//UIControlData", uiControlData != null ? UnityEngine.GUIUtility.systemCopyBuffer : "");
                                string newPath = $"{saveUIPath}/{uiName}.cs";
                                File.WriteAllText(newPath, str);

                                var jsonData = new UIConfigJson
                                {
                                    uiType = uiName,
                                    path = AssetDatabase.GetAssetPath(uiPrefab),
                                    isWindow = isWindow,
                                    uiLayer = layer.ToString(),
                                };

                                uiJsonDatas.Add(uiName, jsonData);
                                uiNames.Add(uiName, newPath);
                                SaveJson();

                                // 生成UIType
                                var newStr = Regex.Replace(File.ReadAllText(UIType), "Max,", $"{uiName},\n\t\tMax,");
                                File.Delete(UIType);
                                File.WriteAllText(UIType, newStr);

                                Debug.Log("生成成功：" + newPath);

                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();
                            }
                            GUI.color = defaultColor;
                        }
                        else
                        {
                            var jsonData = GetUIJson(uiName);

                            EditorGUILayout.ObjectField("已创建脚本", AssetDatabase.LoadAssetAtPath(uiScriptPath, typeof(TextAsset)), typeof(TextAsset), true);
                            jsonData.isWindow = EditorGUILayout.Toggle("是否为窗口", jsonData.isWindow);
                            Enum.TryParse(jsonData.uiLayer, out UILayer layer);
                            jsonData.uiLayer = EditorGUILayout.EnumPopup("UILayer设置", layer).ToString();

                            var defaultColor = GUI.color;
                            GUI.color = Color.green;
                            if (GUILayout.Button("保存设置"))
                            {
                                SaveJson();
                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();
                            }
                            GUI.color = defaultColor;

                            GUI.color = Color.red;
                            if (GUILayout.Button("删除UI脚本"))
                            {
                                if (EditorUtility.DisplayDialog("是否确认删除", $"请确认是否删除:\n{uiScriptPath}\n同时会清除Json，UIType中相关数据", "确定", "取消"))
                                {
                                    // 清除UIType中指定类型
                                    var uiTypeStr = File.ReadAllText(UIType);
                                    int index = uiTypeStr.IndexOf(uiName);
                                    int leftIndex = uiTypeStr.Substring(0, index).LastIndexOf(',') + 1;
                                    int rightIndex = uiTypeStr.Substring(index, uiTypeStr.Length - index).IndexOf(',') + index + 1;
                                    var newStr = uiTypeStr.Substring(0, leftIndex) + uiTypeStr.Substring(rightIndex, uiTypeStr.Length - rightIndex);
                                    File.Delete(UIType);
                                    File.WriteAllText(UIType, newStr);
                                    // 清除UIConfig中的指定类型
                                    uiJsonDatas.Remove(uiName);
                                    uiNames.Remove(uiName);
                                    SaveJson();
                                    // 删除文件
                                    File.Delete(uiScriptPath);

                                    AssetDatabase.SaveAssets();
                                    AssetDatabase.Refresh();
                                    uiNames.Remove(uiName);
                                }
                            }
                            GUI.color = defaultColor;
                        }
                    }
                    else
                    {
                        uiName = "";
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void SaveJson()
    {
        List<UIConfigJson> list = new List<UIConfigJson>();
        foreach (var name in uiNames.Keys)
        {
            if (uiJsonDatas.TryGetValue(name, out var data))
            {
                list.Add(data);
            }
        }
        File.Delete(UIConfig);
        File.WriteAllText(UIConfig, JsonConvert.SerializeObject(list, Formatting.Indented));
    }

    private void TryGetPath(ref string path, string pathName, string endsWith)
    {
        path = PlayerPrefs.GetString(pathName);
        if (!File.Exists(path))
        {
            string[] ids = AssetDatabase.FindAssets(pathName);
            if (ids != null)
            {
                foreach (var id in ids)
                {
                    var str = AssetDatabase.GUIDToAssetPath(id);
                    if (str.EndsWith(endsWith))
                    {
                        path = str;
                    }
                }
            }
        }
    }

    private string GetUIScript(string name, bool tryFind = false)
    {
        if (uiNames.TryGetValue(name, out string path))
        {
            return path;
        }

        if (!tryFind)
        {
            return string.Empty;
        }

        if (GetUIJson(name) == null) return string.Empty;

        string[] ids = AssetDatabase.FindAssets(name);
        if (ids != null)
        {
            foreach (var id in ids)
            {
                var str = AssetDatabase.GUIDToAssetPath(id);
                if (str.EndsWith(".cs"))
                {
                    return str;
                }
            }
        }
        return string.Empty;
    }

    private UIConfigJson GetUIJson(string name)
    {
        if (uiJsonDatas.Count == 0)
        {
            try
            {
                var json = File.ReadAllText(UIConfig);
                var list = JsonConvert.DeserializeObject<List<UIConfigJson>>(json);
                foreach (var item in list)
                {
                    uiJsonDatas.AddOrUpdate(item.uiType, item);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        if (uiJsonDatas.TryGetValue(name, out var jsonData))
        {
            return jsonData;
        }
        return null;
    }

    private void PathField(string name, ref string path, string pathName, string endsWith)
    {
        var obj = EditorGUILayout.ObjectField(name, AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset)), typeof(TextAsset), true);
        if (obj != null)
        {
            var newPath = AssetDatabase.GetAssetPath(obj);
            if (newPath.EndsWith(endsWith) && !newPath.Equals(path))
            {
                path = newPath;
                PlayerPrefs.SetString(pathName, path);
            }
        }
    }
}
#endif

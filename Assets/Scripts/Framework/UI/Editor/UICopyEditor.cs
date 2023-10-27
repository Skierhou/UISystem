#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.IO;
using SkierFramework;

public class UICopyEditor : Editor
{
    [MenuItem("Assets/Create/CreateUI")]
    private static void CopyUI()
    {
        if (Selection.activeObject == null || !(Selection.activeObject is GameObject))
        {
            Debug.LogError("请选择UI预制体!");
            return;
        }

        string templatePath = "Assets/Scripts/UI/Editor/UIViewTemplate.txt";

        if (!File.Exists(templatePath))
        {
            string[] ids = AssetDatabase.FindAssets("UIViewTemplate");
            if (ids != null && ids.Length > 0)
            {
                templatePath = AssetDatabase.GUIDToAssetPath(ids[0]);
            }
        }

        string dirPath = Path.GetFullPath(Path.GetDirectoryName(templatePath) + "\\..\\");

        string path = EditorUtility.OpenFolderPanel("选择路径", dirPath, "");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        // 生成代码
        string str = Regex.Replace(File.ReadAllText(templatePath), "UIXXXView", Selection.activeObject.name);
        UIControlData uiControlData = (Selection.activeObject as GameObject).GetComponent<UIControlData>();
        if (uiControlData != null)
        {
            uiControlData.CopyCodeToClipBoardPrivate();
        }
        str = Regex.Replace(str, "//UIControlData", uiControlData != null ? UnityEngine.GUIUtility.systemCopyBuffer : "");
        string newPath = path + "/" + Selection.activeObject.name + ".cs";
        File.WriteAllText(newPath, str);

        // 生成UIConfig.json配置
        string jsonPath = "Assets/AssetsPackage/UI/UIConfig.json";
        string newStr = Regex.Replace(File.ReadAllText(jsonPath), "]", string.Format(",\t{{\n" +
            "\t\t\"uiType\": \"{0}\",\n" +
            "\t\t\"path\": \"{1}\",\n" +
            "\t\t\"isWindow\": true,\n" +
            "\t\t\"uiLayer\": \"NormalLayer\"\n" +
            "\t}}\n]", Selection.activeObject.name, AssetDatabase.GetAssetPath(Selection.activeObject)));
        File.Delete(jsonPath);
        File.WriteAllText(jsonPath, newStr);

        // 生成UIType
        string typePath = "Assets/Scripts/UI/UIViewBase/UIType.cs";
        if (!File.Exists(typePath))
        {
            string[] ids = AssetDatabase.FindAssets("UIType");
            if (ids != null && ids.Length > 0)
            {
                typePath = AssetDatabase.GUIDToAssetPath(ids[0]);
            }
        }
        newStr = Regex.Replace(File.ReadAllText(typePath), "Max,", string.Format("{0},\n\t\tMax,", Selection.activeObject.name));
        File.Delete(typePath);
        File.WriteAllText(typePath, newStr);

        Debug.Log("生成成功：" + newPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif

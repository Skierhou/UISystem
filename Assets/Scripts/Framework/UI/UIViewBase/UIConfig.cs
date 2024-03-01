using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SkierFramework
{
    [System.Serializable]
    public class UIConfigJson
    {
        public string uiType;
        public string path;
        public bool isWindow;
        public string uiLayer;
    }

    public class UIConfig
    {
        public string path;
        public UIType uiType;
        public UILayer uiLayer;
        public Type viewType;
        public bool isWindow;

        private const string UIConfigPath = "Assets/AssetsPackage/UI/UIConfig.json";

        public static AsyncOperationHandle GetAllConfigs(Action<List<UIConfig>> callback) 
        {
            return ResourceManager.Instance.LoadAssetAsync<UnityEngine.TextAsset>(UIConfigPath, (textAsset) =>
            {
                if (textAsset != null)
                {
                    var list = new List<UIConfig>();
                    var uiConfigs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UIConfigJson>>(textAsset.text);
                    foreach (var config in uiConfigs)
                    {
                        if (!Enum.TryParse<UILayer>(config.uiLayer, out UILayer layer))
                        {
                            layer = UILayer.NormalLayer;
                            Debug.LogErrorFormat("UIConfig.json 中的：{0}  uiLayer解析异常 {1}", config.path, config.uiLayer);
                        }
                        if (!Enum.TryParse<UIType>(config.uiType, out UIType type))
                        {
                            Debug.LogErrorFormat("UIConfig.json 中的：{0}  uiType解析异常 {1}", config.path, config.uiType);
                        }
                        Type viewType = GetType(config.uiType.ToString());
                        if (viewType == null)
                        {
                            viewType = GetType($"{typeof(UIConfig).Namespace}.{config.uiType}");
                        }
                        list.Add(new UIConfig
                        {
                            path = config.path,
                            uiLayer = layer,
                            uiType = type,
                            viewType = viewType,
                            isWindow = config.isWindow
                        });
                    }
                    callback?.Invoke(list);
                }
                else
                {
                    Debug.LogError("未找到配置：" + UIConfigPath);
                }
            }, true);
        }

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (System.Reflection.Assembly assembly in assemblies)
            {
                type = Type.GetType(string.Format("{0}, {1}", typeName, assembly.FullName));
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
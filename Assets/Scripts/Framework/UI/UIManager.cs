using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace SkierFramework
{
    public enum UIBlackType
    {
        None,       // 无黑边，全适应
        Height,     // 保持高度填满，两边黑边
        Width,      // 保持宽度填满, 上下黑边
        AutoBlack,  // 自动黑边(选中左右或上下黑边最少的一方)
    }

    public class UIManager : SingletonMono<UIManager>
    {
        public int width = 1080;
        public int height = 1920;
        public UIBlackType uiBlackType = UIBlackType.None;

        private Transform _root;
        private Camera _worldCamera;
        private Camera _uiCamera;
        /// <summary>
        /// 屏幕渐变遮罩
        /// </summary>
        private CanvasGroup _blackMask;
        private CanvasGroup _backgroundMask;
        private Tweener _fadeTweener;
        /// <summary>
        /// 黑边
        /// </summary>
        private RectTransform[] _blacks = new RectTransform[2];

        private Dictionary<UIType, UIViewController> _viewControllers;
        private Dictionary<UILayer, UILayerLogic> _layers;
        private HashSet<UIType> _openViews;
        private HashSet<UIType> _residentViews;

        public EventSystem EventSystem { get; private set; }
        public EventController<UIEvent> Event { get; private set; }

        private void Awake()
        {
            Initilize();
        }

        private void Initilize()
        {
            _layers = new Dictionary<UILayer, UILayerLogic>();
            _viewControllers = new Dictionary<UIType, UIViewController>();
            _openViews = new HashSet<UIType>();
            _residentViews = new HashSet<UIType>();
            Event = new EventController<UIEvent>();

            _worldCamera = Camera.main;
            _worldCamera.cullingMask &= int.MaxValue ^ (1 << Layer.UI);

            var root = GameObject.Find("UIRoot");
            if (root == null)
            {
                root = new GameObject("UIRoot");
            }
            root.layer = Layer.UI;
            GameObject.DontDestroyOnLoad(root);
            _root = root.transform;

            var camera = GameObject.Find("UICamera");
            if (camera == null)
            {
                camera = new GameObject("UICamera");
            }
            _uiCamera = camera.GetOrAddComponent<Camera>();
            _uiCamera.cullingMask = 1 << Layer.UI;
            _uiCamera.transform.SetParent(_root);
            _uiCamera.orthographic = true;

            EventSystem = EventSystem.current;

            var layers = Enum.GetValues(typeof(UILayer));
            foreach (UILayer layer in layers)
            {
                bool is3d = layer == UILayer.SceneLayer;
                Canvas layerCanvas = UIExtension.CreateLayerCanvas(layer, is3d, _root, is3d ? _worldCamera : _uiCamera, width, height);
                UILayerLogic uILayerLogic = new UILayerLogic(layer, layerCanvas);
                _layers.Add(layer, uILayerLogic);
            }
            _blackMask = UIExtension.CreateBlackMask(_layers[UILayer.BlackMaskLayer].canvas.transform);
            _backgroundMask = UIExtension.CreateBlackMask(_layers[UILayer.BackgroundLayer].canvas.transform);
        }

        private void Update()
        {
            ChangeOrCreateBlack();
        }

        /// <summary>
        /// 创建或者调整黑边，需间隔触发，由于有些设备屏幕是可以转动，是动态的
        /// </summary>
        private void ChangeOrCreateBlack()
        {
            if (_layers == null) return;
            var parent = _layers[UILayer.BackgroundLayer].canvas.transform as RectTransform;
            var uIBlackType = GetUIBlackType();
            switch (uIBlackType)
            {
                case UIBlackType.Height:
                    // 高度适配时的左右黑边
                    var rect = _blacks[0];
                    if (rect == null)
                    {
                        _blacks[0] = rect = UIExtension.CreateBlackMask(parent, 1, "right").transform as RectTransform;
                        rect.pivot = new Vector2(0, 0.5f);
                        rect.anchorMin = new Vector2(1, 0);
                        rect.anchorMax = new Vector2(1, 1);
                        rect.sizeDelta = new Vector2(Screen.width, 0);
                    }
                    else if (Mathf.Abs(rect.anchoredPosition.x * 2 + parent.rect.width - width) < 1)
                    {
                        return;
                    }
                    rect.anchoredPosition = new Vector2((width - parent.rect.width) / 2, 0);

                    rect = _blacks[1];
                    if (rect == null)
                    {
                        _blacks[1] = rect = UIExtension.CreateBlackMask(parent, 1, "left").transform as RectTransform;
                        rect.pivot = new Vector2(1, 0.5f);
                        rect.anchorMin = new Vector2(0, 0);
                        rect.anchorMax = new Vector2(0, 1);
                        rect.sizeDelta = new Vector2(Screen.width, 0);
                    }
                    rect.anchoredPosition = new Vector2(-(width - parent.rect.width) / 2, 0);
                    break;
                case UIBlackType.Width:
                    // 宽度适配时的上下黑边
                    rect = _blacks[0];
                    if (rect == null)
                    {
                        _blacks[0] = rect = UIExtension.CreateBlackMask(parent, 1, "top").transform as RectTransform;
                        rect.pivot = new Vector2(0.5f, 0);
                        rect.anchorMin = new Vector2(0, 1);
                        rect.anchorMax = new Vector2(1, 1);
                        rect.sizeDelta = new Vector2(0, Screen.height);
                    }
                    else if (Mathf.Abs(rect.anchoredPosition.y * 2 + parent.rect.height - height) < 1)
                    {
                        return;
                    }
                    rect.anchoredPosition = new Vector2(0, (height - parent.rect.height) / 2);

                    rect = _blacks[1];
                    if (rect == null)
                    {
                        _blacks[1] = rect = UIExtension.CreateBlackMask(parent, 1, "bottom").transform as RectTransform;
                        rect.pivot = new Vector2(0.5f, 1);
                        rect.anchorMin = new Vector2(0, 0);
                        rect.anchorMax = new Vector2(1, 0);
                        rect.sizeDelta = new Vector2(0, Screen.height);
                    }
                    rect.anchoredPosition = new Vector2(0, -(height - parent.rect.height) / 2);
                    break;
                default:
                    break;
            }
        }

        public UIBlackType GetUIBlackType()
        {
            var uIBlackType = uiBlackType;
            if (uIBlackType == UIBlackType.AutoBlack)
            {
                var parent = _layers[UILayer.BackgroundLayer].canvas.transform as RectTransform;
                if (Mathf.Abs(width - parent.rect.width) > Mathf.Abs(height - parent.rect.height))
                    uIBlackType = UIBlackType.Width;
                else
                    uIBlackType = UIBlackType.Height;
            }
            return uIBlackType;
        }

        public Rect GetSafeArea()
        {
            Rect rect = Screen.safeArea;
            if (uiBlackType == UIBlackType.Width)
            {
                var parent = _layers[UILayer.BackgroundLayer].canvas.transform as RectTransform;
                float blackArea = Mathf.Abs(height - parent.rect.height) / 2;
                rect.yMin = Mathf.Max(0, rect.yMin - blackArea);
                rect.yMax = Mathf.Min(rect.yMax + blackArea, Screen.height);
            }
            else if (uiBlackType == UIBlackType.Height)
            {
                var parent = _layers[UILayer.BackgroundLayer].canvas.transform as RectTransform;
                float blackArea = Mathf.Abs(width - parent.rect.width) / 2;
                rect.xMin = Mathf.Max(0, rect.xMin - blackArea);
                rect.xMax = Mathf.Min(rect.xMax + blackArea, Screen.width);
            }
            return rect;
        }

        public void EnableBackgroundMask(bool enable)
        {
            _backgroundMask.alpha = enable ? 1 : 0;
        }

        public AsyncOperationHandle InitUIConfig()
        {
            // 初始化需要加载所有UI的配置
            return UIConfig.GetAllConfigs((list) =>
            {
                foreach (var cfg in list)
                {
                    if (_viewControllers.ContainsKey(cfg.uiType))
                    {
                        Debug.LogErrorFormat("存在相同的uiType:{0}， 请检查UIConfig是否重复！", cfg.uiType.ToString());
                        continue;
                    }

                    _viewControllers.Add(cfg.uiType, new UIViewController
                    {
                        uiPath = cfg.path,
                        uiType = cfg.uiType,
                        uiLayer = _layers[cfg.uiLayer],
                        uiViewType = cfg.viewType,
                        isWindow = cfg.isWindow,
                    });
                }
            });
        }

        /// <summary>
        /// 注册常驻UI
        /// </summary>
        public void AddResidentUI(UIType type)
        {
            _residentViews.Add(type);
        }

        public void Open(UIType type, object userData = null, Action callback = null)
        {
            if (!_viewControllers.ContainsKey(type))
            {
                Debug.LogErrorFormat("未配置uiType:{0}， 请检查UIConfig.cs！", type.ToString());
                return;
            }

            _openViews.Add(type);
            _viewControllers[type].Open(userData, callback);
        }

        public AsyncOperationHandle Preload(UIType type)
        {
            if (!_viewControllers.TryGetValue(type, out var controller))
            {
                Debug.LogErrorFormat("未配置uiType:{0}， 请检查UIConfig.cs！", type.ToString());
                return default;
            }
            return controller.Load();
        }

        public void PreloadAll()
        {
            foreach (var controller in _viewControllers.Values)
            {
                ResourceManager.Instance.LoadAssetAsync<GameObject>(controller.uiPath, null);
            }
        }

        public bool IsOpen(UIType type)
        {
            return _openViews.Contains(type);
        }

        public void Close(UIType type, Action callback = null)
        {
            if (!_viewControllers.ContainsKey(type))
            {
                Debug.LogErrorFormat("未配置uiType:{0}， 请检查UIConfig.cs！", type.ToString());
                return;
            }

            _openViews.Remove(type);
            _viewControllers[type].Close(callback);
        }

        /// <summary>
        /// UI建议都用事件进行交互，最好不使用该接口
        /// </summary>
        public T GetView<T>(UIType type) where T : UIView
        {
            if (!_viewControllers.ContainsKey(type))
            {
                Debug.LogErrorFormat("未配置uiType:{0}， 请检查UIConfig.cs！", type.ToString());
                return null;
            }

            return _viewControllers[type].uiView as T;
        }

        public void CloseAll(UIType ignoreType = UIType.Max, bool closeResidentView = false)
        {
            var list = ListPool<UIType>.Get();

            foreach (var uiType in _openViews)
            {
                if (ignoreType == uiType) continue;

                if (closeResidentView || !_residentViews.Contains(uiType))
                {
                    _viewControllers[uiType].Close();
                    list.Add(uiType);
                }
            }
            foreach (var uiType in list)
            {
                _openViews.Remove(uiType);
            }
            ListPool<UIType>.Release(list);
        }

        public void ReleaseAll()
        {
            foreach (var controller in _viewControllers.Values)
            {
                if (!_residentViews.Contains(controller.uiType))
                {
                    _openViews.Remove(controller.uiType);
                    controller.Release();
                }
            }
        }

        public void FadeIn(float duration = 0.5f, TweenCallback callback = null)
        {
            if (_fadeTweener != null && _fadeTweener.IsPlaying())
                _fadeTweener.Complete();
            _fadeTweener = _blackMask.DOFade(1.0f, duration);
            _fadeTweener.onComplete = callback;
        }

        public void FadeOut(float duration = 0.5f, TweenCallback callback = null)
        {
            if (_fadeTweener != null && _fadeTweener.IsPlaying())
                _fadeTweener.Complete();
            _fadeTweener = _blackMask.DOFade(0.0f, duration);
            _fadeTweener.onComplete = callback;
        }

        public void FadeInOut(float duration = 1.0f, TweenCallback callback = null)
        {
            if (_fadeTweener != null && _fadeTweener.IsPlaying())
                _fadeTweener.Complete();
            _fadeTweener = _blackMask.DOFade(1.0f, duration * 0.5f);
            _fadeTweener.onComplete += () =>
            {
                _fadeTweener = _blackMask.DOFade(0.0f, duration * 0.5f);
                _fadeTweener.onComplete = callback;
            };
        }

        public void Cancel()
        {
            if (_layers.TryGetValue(UILayer.NormalLayer, out var layer) && layer.openedViews.Count > 0)
            {
                var viewController = layer.openedViews.Peek();
                if (viewController.uiView != null)
                {
                    viewController.uiView.OnCancel();
                }
            }
        }
    }
}

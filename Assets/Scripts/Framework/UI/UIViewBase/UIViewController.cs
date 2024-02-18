using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SkierFramework
{
    public class UIViewController
    {
        // 配置
        public UIType uiType;
        public string uiPath;
        public Type uiViewType;
        public UILayerLogic uiLayer;
        public bool isWindow;

        public UIView uiView;
        public UIViewAnim uiViewAnim;
        public bool isLoading = false;
        public bool isOpen = false;
        public bool isPause = false;
        public int order;
        /// <summary>
        /// 在我上面的界面(非窗口界面)的数量
        /// </summary>
        public int topViewNum;

        public AsyncOperationHandle Load(object userData = null, Action callback = null)
        {
            isLoading = true;

            if (isOpen)
            {
                order = uiLayer.PopOrder(this);
            }
            return ResourceManager.Instance.InstantiateAsync(uiPath, (go) =>
            {
                if (!isLoading)
                {
                    ResourceManager.Instance.Recycle(go);
                    callback?.Invoke();
                    Release();
                    return;
                }

                isLoading = false;
                uiView = (UIView)go.GetOrAddComponent(uiViewType);
                uiViewAnim = go.GetComponent<UIViewAnim>();
                uiView.transform.SetParentEx(uiLayer.canvas.transform);
                RectTransform rectTransform = uiView.transform as RectTransform;

                switch (UIManager.Instance.GetUIBlackType())
                {
                    case UIBlackType.None:
                        // 全适配
                        rectTransform.SetAnchor(AnchorPresets.StretchAll);
                        rectTransform.anchoredPosition = Vector2.zero;
                        rectTransform.sizeDelta = Vector2.zero;
                        break;
                    case UIBlackType.Height:
                        // 保持高度填满，两边留黑边
                        rectTransform.SetAnchor(AnchorPresets.VertStretchCenter);
                        rectTransform.anchoredPosition = Vector2.zero;
                        rectTransform.sizeDelta = new Vector2(UIManager.Instance.width, 0);
                        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, UIManager.Instance.width);
                        break;
                    case UIBlackType.Width:
                        // 保持宽度填满，上下留黑边
                        rectTransform.SetAnchor(AnchorPresets.HorStretchMiddle);
                        rectTransform.anchoredPosition = Vector2.zero;
                        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, UIManager.Instance.height);
                        rectTransform.sizeDelta = new Vector2(0, UIManager.Instance.height);
                        break;
                }

                uiView.OnInit(go.GetComponent<UIControlData>(), this);
                uiView.transform.SetAsLastSibling();

                if (isOpen)
                {
                    Open(userData, callback, true);
                }
                else
                {
                    Close(callback);
                }
            });
        }

        public void Open(object userData = null, Action callback = null, bool isFirstOpen = false)
        {
            isOpen = true;
            if (isLoading) return;

            if (uiView == null)
            {
                Load(userData, callback);
            }
            else
            {
                if (!isFirstOpen && isOpen && order > 0)
                {
                    TrueClose();
                }
                TrueOpen(userData, callback);
                if (uiViewAnim != null)
                {
                    uiViewAnim.Open();
                }
            }
        }

        public void Close(Action callback = null)
        {
            isOpen = false;
            if (isLoading) return;

            if (uiView != null)
            {
                if (uiViewAnim != null)
                {
                    uiViewAnim.Close(() => { TrueClose(callback); });
                }
                else
                {
                    TrueClose(callback);
                }
            }
        }

        public void Release()
        {
            if (uiView != null)
            {
                if (isOpen)
                    TrueClose();
                uiView.OnRelease();
                GameObject.Destroy(uiView.gameObject);
            }
            uiView = null;
            uiViewAnim = null;
            isLoading = false;
            isOpen = false;
            order = 0;
        }

        private void TrueOpen(object userData = null, Action callback = null)
        {
            uiLayer.OpenUI(this);
            SetVisible(true);
            // 刷新一下显示
            AddTopViewNum(0);
            uiView.OnOpen(userData);
            uiView.OnResume();
            callback?.Invoke();
        }

        private void TrueClose(Action callback = null)
        {
            uiLayer.CloseUI(this);
            // 刷新一下显示
            AddTopViewNum(-100000);
            SetVisible(false);
            uiView.OnPause();
            uiView.OnClose();
            callback?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            if (uiView != null)
            {
                uiView.gameObject.SetActive(visible);
            }
        }

        public void AddTopViewNum(int num)
        {
            topViewNum += num;
            topViewNum = Mathf.Max(0, topViewNum);
            SetVisible(topViewNum <= 0);
        }
    }

}

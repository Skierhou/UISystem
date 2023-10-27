using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkierFramework
{
    public enum UILayer
    {
        SceneLayer = 1000,
        BackgroundLayer = 2000,
        NormalLayer = 3000,
        InfoLayer = 4000,
        TopLayer = 5000,
        TipLayer = 6000,
        BlackMaskLayer = 7000,
    }

    public class UILayerLogic
    {
        public UILayer layer;
        public Canvas canvas;
        private int maxOrder;
        private HashSet<int> orders;
        public Stack<UIViewController> openedViews;

        public UILayerLogic(UILayer layer, Canvas canvas)
        {
            this.layer = layer;
            this.canvas = canvas;
            maxOrder = (int)layer;
            orders = new HashSet<int>();
            openedViews = new Stack<UIViewController>();
        }

        public void CloseUI(UIViewController closedUI)
        {
            int order = closedUI.order;
            PushOrder(closedUI);
            closedUI.order = 0;

            if (openedViews.Count > 0)
            {
                var topViewController = openedViews.Peek();
                // 拿到最上层UI，如果被暂停的话，则恢复，
                // 暂停和恢复不影响其是否被覆盖隐藏，只要不是最上层UI都应该标记暂停状态
                if (topViewController != null && topViewController.isPause)
                {
                    topViewController.isPause = false;
                    if (topViewController.uiView != null)
                    {
                        topViewController.uiView.OnResume();
                    }
                }

                if (!closedUI.isWindow)
                {
                    foreach (var viewController in openedViews)
                    {
                        if (viewController != closedUI
                            && viewController.isOpen
                            && viewController.order < order)
                        {
                            viewController.AddTopViewNum(-1);
                        }
                    }
                }
            }
        }

        public void OpenUI(UIViewController openedUI)
        {
            if (openedUI.order == 0)
            {
                openedUI.order = PopOrder(openedUI);
            }

            foreach (var viewController in openedViews)
            {
                if (viewController != openedUI
                    && viewController.isOpen
                    && viewController.order < openedUI.order
                    && viewController.uiView != null)
                {
                    if (!viewController.isPause)
                    {
                        viewController.isPause = true;
                        viewController.uiView.OnPause();
                    }
                    if (!openedUI.isWindow)
                    {
                        viewController.AddTopViewNum(1);
                    }
                }
            }
        }

        public void PushOrder(UIViewController closedUI)
        {
            int order = closedUI.order;
            if (orders.Remove(order))
            {
                // 重新计算最大值
                maxOrder = (int)layer;
                foreach (var item in orders)
                {
                    maxOrder = Mathf.Max(maxOrder, item);
                }

                // 移除界面
                List<UIViewController> list = ListPool<UIViewController>.Get();
                while (openedViews.Count > 0)
                {
                    var view = openedViews.Pop();
                    if (view != closedUI)
                    {
                        list.Add(view);
                    }
                    else
                    {
                        break;
                    }
                }
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    openedViews.Push(list[i]);
                }
                ListPool<UIViewController>.Release(list);
            }
        }

        public int PopOrder(UIViewController uIViewController)
        {
            maxOrder += 10;
            orders.Add(maxOrder);
            openedViews.Push(uIViewController);
            return maxOrder;
        }
    }

}

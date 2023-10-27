using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace SkierFramework
{
    public enum UIAdaptType
    {
        All,
        LeftOrTop,
        RightOrBottom,
    }

    public class UIAdapter : MonoBehaviour
    {
        public UIAdaptType uIAdaptType = UIAdaptType.All;

        private float cd;

        private void Update()
        {
            // 为避免旋转屏幕，华为分屏机等导致分辨率变化，且安全区变化的问题，需要持续检测
            if (Time.time > cd)
            {
                InitAdapter();
                cd = Time.time + 1;
            }
        }

        private void InitAdapter()
        {
            var safeArea = Screen.safeArea;
            if (UIManager.Instance != null)
            {
                safeArea = UIManager.Instance.GetSafeArea();
            }
            var orientation = Screen.orientation;
            RectTransform rectTransform = transform as RectTransform;
            rectTransform.sizeDelta = Vector2.zero;
            if (orientation == ScreenOrientation.LandscapeLeft || orientation == ScreenOrientation.LandscapeRight)
            {
                switch (uIAdaptType)
                {
                    case UIAdaptType.All:
                        rectTransform.anchorMin = new Vector2(safeArea.xMin / Screen.width, 0);
                        rectTransform.anchorMax = new Vector2(safeArea.xMax / Screen.width, 1);
                        break;
                    case UIAdaptType.LeftOrTop:
                        if (orientation == ScreenOrientation.LandscapeLeft)
                        {
                            rectTransform.anchorMin = new Vector2(safeArea.xMin / Screen.width, 0);
                            rectTransform.anchorMax = new Vector2(1, 1);
                        }
                        else
                        {
                            rectTransform.anchorMin = new Vector2(0, 0);
                            rectTransform.anchorMax = new Vector2(safeArea.xMax / Screen.width, 1);
                        }
                        break;
                    case UIAdaptType.RightOrBottom:
                        if (orientation == ScreenOrientation.LandscapeLeft)
                        {
                            rectTransform.anchorMin = new Vector2(0, 0);
                            rectTransform.anchorMax = new Vector2(safeArea.xMax / Screen.width, 1);
                        }
                        else
                        {
                            rectTransform.anchorMin = new Vector2(safeArea.xMin / Screen.width, 0);
                            rectTransform.anchorMax = new Vector2(1, 1);
                        }
                        break;
                }
            }
            else if (orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown)
            {
                switch (uIAdaptType)
                {
                    case UIAdaptType.All:
                        rectTransform.anchorMin = new Vector2(0 , safeArea.yMin / Screen.height);
                        rectTransform.anchorMax = new Vector2(1 , safeArea.yMax / Screen.height);
                        break;
                    case UIAdaptType.LeftOrTop:
                        if (orientation == ScreenOrientation.Portrait)
                        {
                            rectTransform.anchorMin = new Vector2(0, 0);
                            rectTransform.anchorMax = new Vector2(1, safeArea.yMax / Screen.height);
                        }
                        else
                        {
                            rectTransform.anchorMin = new Vector2(0, safeArea.yMin / Screen.height);
                            rectTransform.anchorMax = new Vector2(1, 1);
                        }
                        break;
                    case UIAdaptType.RightOrBottom:
                        if (orientation == ScreenOrientation.Portrait)
                        {
                            rectTransform.anchorMin = new Vector2(0, safeArea.yMin / Screen.height);
                            rectTransform.anchorMax = new Vector2(1, 1);
                        }
                        else
                        {
                            rectTransform.anchorMin = new Vector2(0, 0);
                            rectTransform.anchorMax = new Vector2(1, safeArea.yMax / Screen.height);
                        }
                        break;
                }
            }
        }
    }
}

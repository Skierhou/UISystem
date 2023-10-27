using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace SkierFramework
{
    /* 获取内置图片
    * AssetDatabase.GetBuiltinExtraResource
    * "UI/Skin/UISprite.psd"
    "UI/Skin/Background.psd"
    "UI/Skin/InputFieldBackground.psd"
    "UI/Skin/Knob.psd"
    "UI/Skin/Checkmark.psd"
    "UI/Skin/DropdownArrow.psd"
    "UI/Skin/UIMask.psd"
    */
    public class OverrideUICreate
    {
        [MenuItem("GameObject/UI/Image", true)]
        public static void IgnoreImage()
        {
        }

        [MenuItem("GameObject/UI/Image.")]
        public static void CreateImage()
        {
            var image = Create<Image>();
            image.raycastTarget = false;
            image.maskable = image.GetComponentInParent<RectMask2D>() != null || image.GetComponentInParent<Mask>() != null;
            image.gameObject.SetLayerRecursively(Layer.UI);
        }

        [MenuItem("GameObject/UI/Raw Image", true)]
        public static void IgnoreRawImage()
        {
        }

        [MenuItem("GameObject/UI/RawImage")]
        public static void CreateRawImage()
        {
            var image = Create<RawImage>();
            image.raycastTarget = false;
            image.maskable = image.GetComponentInParent<RectMask2D>() != null || image.GetComponentInParent<Mask>() != null;
            image.gameObject.SetLayerRecursively(Layer.UI);
        }

        [MenuItem("GameObject/UI/Button")]
        public static void CreateButton()
        {
            var image = Create<Image>("Button");
            var button = image.AddComponent<Button>();
            image.maskable = image.GetComponentInParent<RectMask2D>() != null || image.GetComponentInParent<Mask>() != null;
            image.rectTransform.sizeDelta = new Vector2(160, 30);
            image.gameObject.SetLayerRecursively(Layer.UI);
        }

        [MenuItem("GameObject/UI/Text - TextMeshPro", true)]
        public static void IgnoreTextMeshPro()
        {
        }

        [MenuItem("GameObject/UI/Text -  TextMeshPro")]
        public static void CreateTextMeshPro()
        {
            var textMeshPro = Create<TextMeshProUGUI>("Text");
            textMeshPro.raycastTarget = false;
            textMeshPro.color = Color.black;
            textMeshPro.maskable = textMeshPro.GetComponentInParent<RectMask2D>() != null || textMeshPro.GetComponentInParent<Mask>() != null;
            textMeshPro.gameObject.SetLayerRecursively(Layer.UI);
        }

        [MenuItem("GameObject/UI/Button - TextMeshPro", true)]
        public static void IgnoreButtonTextMeshPro()
        {
        }

        [MenuItem("GameObject/UI/Button -  TextMeshPro")]
        public static void CreateButtonTextMeshPro()
        {
            var image = Create<Image>("Button");
            var button = image.AddComponent<Button>();
            var textMeshPro = Create<TextMeshProUGUI>("Text", button.transform);
            textMeshPro.raycastTarget = false;
            textMeshPro.maskable = textMeshPro.GetComponentInParent<RectMask2D>() != null || textMeshPro.GetComponentInParent<Mask>() != null;
            textMeshPro.text = "Button";
            textMeshPro.alignment = TextAlignmentOptions.Center;
            textMeshPro.alignment = TextAlignmentOptions.Midline;

            textMeshPro.rectTransform.anchorMin = Vector3.zero;
            textMeshPro.rectTransform.anchorMax = Vector3.one;
            textMeshPro.rectTransform.sizeDelta = Vector2.zero;
            textMeshPro.color = Color.black;
            textMeshPro.fontSize = 24;

            image.maskable = textMeshPro.maskable;
            image.rectTransform.sizeDelta = new Vector2(160, 30);
            image.gameObject.SetLayerRecursively(Layer.UI);
        }

        #region ScrollRect
        [MenuItem("GameObject/UI/Scroll View", true)]
        public static void IgnoreScrollView()
        {
        }

        [MenuItem("GameObject/UI/UIScrollView")]
        public static ScrollRect CreateScrollView()
        {
            var image = Create<Image>("UIScrollView");
            image.raycastTarget = true;
            image.maskable = false;
            image.rectTransform.sizeDelta = new Vector2(200, 200);
            var scrollRect = image.AddComponent<ScrollRect>();
            var uIScrollView = image.AddComponent<UIScrollView>();

            var viewportImage = Create<Image>("Viewport", uIScrollView.transform);
            viewportImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
            viewportImage.type = Image.Type.Sliced;
            viewportImage.raycastTarget = true;
            viewportImage.maskable = true;

            var viewport = viewportImage.transform as RectTransform;
            viewport.AddComponent<RectMask2D>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = new Vector2(0, 1);
            viewport.sizeDelta = Vector2.zero;
            viewport.anchoredPosition = Vector2.zero;

            scrollRect.viewport = viewport;

            var content = Create<RectTransform>("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0, 1);
            content.sizeDelta = new Vector2(0, 300);
            content.anchoredPosition = Vector2.zero;

            scrollRect.content = content;

            uIScrollView.m_ScrollRect = scrollRect;
            uIScrollView.m_Content = content;
            scrollRect.gameObject.SetLayerRecursively(Layer.UI);
            return scrollRect;
        }

        [MenuItem("GameObject/UI/UIScrollView - Horizontal")]
        public static void CreateScrollViewHorizontal()
        {
            var scrollRect = CreateScrollView();

            scrollRect.horizontalScrollbar = CreateScrollBar("Scrollbar Horizontal", scrollRect.transform);
            scrollRect.horizontalScrollbar.direction = Scrollbar.Direction.LeftToRight;
            scrollRect.verticalScrollbarVisibility = scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = scrollRect.horizontalScrollbarSpacing = -3;

            var uiScrollView = scrollRect.GetComponent<UIScrollView>();
            uiScrollView.m_AxisType = RectTransform.Axis.Horizontal;
            uiScrollView.m_AlignType = AlignType.Left;

            scrollRect.gameObject.SetLayerRecursively(Layer.UI);
        }

        [MenuItem("GameObject/UI/UIScrollView - Vertical")]
        public static void CreateScrollViewVertical()
        {
            var scrollRect = CreateScrollView();

            scrollRect.verticalScrollbar = CreateScrollBar("Scrollbar Vertical", scrollRect.transform);
            var verticalRect = scrollRect.verticalScrollbar.transform as RectTransform;
            verticalRect.anchorMin = new Vector2(1, 0);
            verticalRect.anchorMax = new Vector2(1, 1);
            verticalRect.pivot = new Vector2(1, 1);
            verticalRect.sizeDelta = new Vector2(20, 0);
            verticalRect.anchoredPosition = Vector2.zero;
            scrollRect.verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;

            scrollRect.verticalScrollbarVisibility = scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = scrollRect.horizontalScrollbarSpacing = -3;

            scrollRect.gameObject.SetLayerRecursively(Layer.UI);
        }

        [MenuItem("GameObject/UI/UIScrollView - All")]
        public static void CreateScrollViewAll()
        {
            var scrollRect = CreateScrollView();

            scrollRect.horizontalScrollbar = CreateScrollBar("Scrollbar Horizontal", scrollRect.transform);
            scrollRect.horizontalScrollbar.direction = Scrollbar.Direction.LeftToRight;

            scrollRect.verticalScrollbar = CreateScrollBar("Scrollbar Vertical", scrollRect.transform);
            var verticalRect = scrollRect.verticalScrollbar.transform as RectTransform;
            verticalRect.anchorMin = new Vector2(1, 0);
            verticalRect.anchorMax = new Vector2(1, 1);
            verticalRect.pivot = new Vector2(1, 1);
            verticalRect.sizeDelta = new Vector2(20, 0);
            verticalRect.anchoredPosition = Vector2.zero;
            scrollRect.verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;

            scrollRect.verticalScrollbarVisibility = scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = scrollRect.horizontalScrollbarSpacing = -3;

            scrollRect.gameObject.SetLayerRecursively(Layer.UI);
        }

        public static Scrollbar CreateScrollBar(string name, Transform parent)
        {
            var verticalScorllImage = Create<Image>(name, parent);
            verticalScorllImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            verticalScorllImage.type = Image.Type.Sliced;
            var verticalScroll = verticalScorllImage.AddComponent<Scrollbar>();
            var verticalRect = verticalScroll.transform as RectTransform;
            verticalRect.anchorMin = Vector2.zero;
            verticalRect.anchorMax = new Vector2(1, 0);
            verticalRect.pivot = Vector2.zero;
            verticalRect.sizeDelta = new Vector2(0, 20);
            verticalRect.anchoredPosition = Vector2.zero;

            var handle = Create<Image>("Handle", verticalRect.transform);
            handle.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            handle.type = Image.Type.Sliced;
            handle.rectTransform.anchorMin = Vector2.zero;
            handle.rectTransform.anchorMax = Vector2.one;
            handle.rectTransform.sizeDelta = Vector2.zero;
            handle.rectTransform.anchoredPosition = Vector2.zero;

            verticalScroll.handleRect = handle.rectTransform;

            return verticalScroll;
        }
        #endregion

        [MenuItem("GameObject/UI/Slider", true)]
        public static void IgnoreSlider()
        {
        }

        [MenuItem("GameObject/UI/Slider.")]
        public static void CreateSlider()
        {
            var image = Create<Image>("Slider");
            image.color = Color.grey;
            image.rectTransform.sizeDelta = new Vector2(160, 20);
            var slider = image.AddComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            var fill = Create<Image>("Fill", slider.transform);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.sizeDelta = Vector2.zero;
            fill.rectTransform.anchoredPosition = Vector2.zero;
            slider.fillRect = fill.rectTransform;

            var handle = Create<Image>("Handle", slider.transform);
            handle.rectTransform.anchorMin = Vector2.zero;
            handle.rectTransform.anchorMax = new Vector2(0, 1);
            handle.rectTransform.sizeDelta = new Vector2(20, 0);
            handle.rectTransform.anchoredPosition = Vector2.zero;
            handle.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            slider.handleRect = handle.rectTransform;

            image.gameObject.SetLayerRecursively(Layer.UI);
        }

        public static T Create<T>(string name = null, Transform parent = null) where T : Component
        {
            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name;
            GameObject go = new GameObject(name);
            if (parent == null)
            {
                go.transform.SetParent(Selection.activeTransform);
                Selection.activeGameObject = go;
            }
            else
            {
                go.transform.SetParent(parent);
            }
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            return go.AddComponent<T>();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static UnityEngine.RectTransform;
using DG.Tweening;

namespace SkierFramework
{
    public enum AlignType
    {
        Left,
        Right,
        Top,
        Bottom,
        Center,
    }

    public class UIScrollView : MonoBehaviour, IEndDragHandler
    {
        public ScrollRect m_ScrollRect;
        public RectTransform m_Content;
        /// <summary>
        /// 水平移动/垂直移动
        /// </summary>
        public Axis m_AxisType;
        /// <summary>
        /// 布局
        /// </summary>
        public AlignType m_AlignType;
        /// <summary>
        /// 子物体的中心点
        /// </summary>
        public PivotPresets m_ItemPivot;
        /// <summary>
        /// 开始间隔
        /// </summary>
        public int m_HorizontalStartSpace;
        /// <summary>
        /// 开始间隔
        /// </summary>
        public int m_VerticalStartSpace;
        /// <summary>
        /// 水平间隔
        /// </summary>
        public int m_HorizontalSpace;
        /// <summary>
        /// 垂直间隔
        /// </summary>
        public int m_VerticalSpace;
        /// <summary>
        /// 另一个方向上物品的个数，水平移动-表示列个数，垂直移动-表示行个数
        /// </summary>
        public int m_CountOfOtherAxis = 1;
        /// <summary>
        /// 是否分页
        /// </summary>
        public bool m_IsPaging;

        private IList m_Datas;
        private PrefabPool m_PrefabPool;
        private List<UILoopItem> m_LoopItems;
        private int m_SelectIndex = -1;

        private int m_HorizontalCount;
        private int m_VerticalCount;
        private float m_ChildWidth;
        private float m_ChildHeight;
        private int m_CurrentIndex;

        private Type m_ItemType;
        private object m_UserData;

        private Tweener m_Tweener;

        public Action<int> OnSelectChanged;
        public int SelectIndex => m_SelectIndex;
        public List<UILoopItem> LoopItems => m_LoopItems;
        public int CurrentIndex => m_CurrentIndex;

        private Rect parentRect;

        private void Awake()
        {
            m_LoopItems = new List<UILoopItem>();
            m_ScrollRect.onValueChanged.AddListener(OnValueChanged);
            if (m_AxisType != Axis.Horizontal)
                m_ScrollRect.horizontal = false;
            if (m_AxisType != Axis.Vertical)
                m_ScrollRect.vertical = false;
        }

        /// <summary>
        /// 刷新整个scrollview
        /// </summary>
        /// <param name="dataList">数据</param>
        /// <param name="prefab">预制体</param>
        /// <param name="type">类型</param>
        /// <param name="isPaging">是否分页</param>
        /// <param name="userData">用户数据</param>
        public void UpdateList(IList dataList, GameObject prefab, Type type, bool isKeepPos = false, object userData = null)
        {
            if (dataList == null || prefab == null)
            {
                Release();
                return;
            }

            if (m_PrefabPool != null && m_PrefabPool.Prefab != prefab)
            {
                m_PrefabPool.Destroy();
                m_PrefabPool = null;
            }
            if (m_PrefabPool == null)
            {
                m_PrefabPool = PrefabPool.Create(prefab);
            }
            prefab.SetActive(false);
            m_PrefabPool.RecycleUseList();
            m_LoopItems.Clear();
            m_SelectIndex = -1;

            RectTransform rect = prefab.GetComponent<RectTransform>();
            m_ChildWidth = rect.rect.width * rect.transform.localScale.x;
            m_ChildHeight = rect.rect.height * rect.transform.localScale.y;

            var parent = m_ScrollRect.transform as RectTransform;
            parentRect = parent.rect;
            m_HorizontalCount = Mathf.CeilToInt((parentRect.width - m_HorizontalStartSpace) / (m_ChildWidth + m_HorizontalSpace));
            m_VerticalCount = Mathf.CeilToInt((parentRect.height - m_VerticalStartSpace) / (m_ChildHeight + m_VerticalSpace));

            m_ItemType = type;
            m_Datas = dataList;
            m_UserData = userData;

            m_Content.SetPivot(m_ItemPivot);
            Vector2 oldPos = m_Content.anchoredPosition;

            if (m_Tweener != null)
            {
                m_Tweener.Kill();
                m_Tweener = null;
            }

            if (m_CountOfOtherAxis == 0)
            {
                if (m_AxisType == Axis.Horizontal)
                    m_CountOfOtherAxis = Mathf.FloorToInt((parentRect.height - m_VerticalStartSpace) / (m_ChildHeight + m_VerticalSpace));
                else
                    m_CountOfOtherAxis = Mathf.FloorToInt((parentRect.width - m_HorizontalStartSpace) / (m_ChildWidth + m_HorizontalSpace));

                m_CountOfOtherAxis = Math.Max(1, m_CountOfOtherAxis);
            }

            int axisCount = Mathf.CeilToInt(dataList.Count * 1.0f / m_CountOfOtherAxis);
            switch (m_AxisType)
            {
                case Axis.Horizontal:
                    if (m_AlignType == AlignType.Right)
                    {
                        m_Content.SetAnchor(AnchorPresets.VertStretchRight);
                    }
                    else
                    {
                        m_Content.SetAnchor(AnchorPresets.VertStretchLeft);
                    }
                    m_Content.sizeDelta = new Vector2(axisCount * m_ChildWidth + (axisCount - 1) * m_HorizontalSpace + m_HorizontalStartSpace * 2, 0);
                    if (m_AlignType == AlignType.Center)
                    {
                        var viewPort = m_Content.parent as RectTransform;
                        viewPort.anchorMin = new Vector2(0.5f, 0.5f);
                        viewPort.anchorMax = new Vector2(0.5f, 0.5f);
                        viewPort.pivot = new Vector2(0.5f, 0.5f);
                        viewPort.anchoredPosition = Vector2.zero;
                        viewPort.sizeDelta = new Vector2(m_Content.sizeDelta.x, parentRect.height);
                        int verCount = Mathf.FloorToInt((parentRect.height - m_VerticalStartSpace) / (m_ChildHeight + m_VerticalSpace));
                        if (verCount > m_Datas.Count)
                        {
                            viewPort.sizeDelta = new Vector2(m_Content.sizeDelta.x, (m_ChildHeight + m_VerticalSpace) * m_Datas.Count - m_VerticalSpace + m_VerticalStartSpace * 2);
                        }
                    }
                    break;
                case Axis.Vertical:
                    if (m_AlignType == AlignType.Bottom)
                    {
                        m_Content.SetAnchor(AnchorPresets.BottomStretch);
                    }
                    else
                    {
                        m_Content.SetAnchor(AnchorPresets.HorStretchTop);
                    }
                    m_Content.sizeDelta = new Vector2(0, axisCount * m_ChildHeight + (axisCount - 1) * m_VerticalSpace + m_VerticalStartSpace * 2);
                    if (m_AlignType == AlignType.Center)
                    {
                        var viewPort = m_Content.parent as RectTransform;
                        viewPort.anchorMin = new Vector2(0.5f, 0.5f);
                        viewPort.anchorMax = new Vector2(0.5f, 0.5f);
                        viewPort.pivot = new Vector2(0.5f, 0.5f);
                        viewPort.anchoredPosition = Vector2.zero;
                        viewPort.sizeDelta = new Vector2(parentRect.width, m_Content.sizeDelta.y);
                        int horCount = Mathf.CeilToInt((parentRect.width - m_HorizontalStartSpace) / (m_ChildWidth + m_HorizontalSpace));
                        if (horCount > m_Datas.Count)
                        {
                            viewPort.sizeDelta = new Vector2((m_ChildWidth + m_HorizontalSpace) * m_Datas.Count - m_HorizontalSpace + m_HorizontalStartSpace * 2, m_Content.sizeDelta.y);
                        }
                    }
                    break;
            }

            if (isKeepPos)
            {
                m_Content.anchoredPosition = new Vector2(Mathf.Min(oldPos.x, m_Content.sizeDelta.x), Mathf.Min(oldPos.y, m_Content.sizeDelta.y));
            }
            else
            {
                m_Content.anchoredPosition = Vector2.zero;
            }
            m_CurrentIndex = GetCurrentItemIndex();
            UpdateContent(m_CurrentIndex);
        }

        public void GetPos(int index, out int x, out int y)
        {
            if (m_AxisType == Axis.Horizontal)
            {
                x = index / m_CountOfOtherAxis;
                y = index % m_CountOfOtherAxis;
            }
            else
            {
                x = index % m_CountOfOtherAxis;
                y = index / m_CountOfOtherAxis;
            }
        }

        public int GetIndex(int x, int y)
        {
            if (x < 0 || y < 0) return -1;

            if (m_AxisType == Axis.Horizontal)
            {
                if (y >= m_CountOfOtherAxis) return -1;

                return x * m_CountOfOtherAxis + y;
            }
            else
            {
                if (x >= m_CountOfOtherAxis) return -1;

                return y * m_CountOfOtherAxis + x;
            }
        }

        public void UpdateContent(int index = 0)
        {
            if (m_Datas == null) return;

            int maxCount = 0;
            switch (m_AxisType)
            {
                case Axis.Horizontal:
                    maxCount = (m_HorizontalCount + 2) * m_CountOfOtherAxis;
                    break;
                case Axis.Vertical:
                    maxCount = (m_VerticalCount + 2) * m_CountOfOtherAxis;
                    break;
            }

            // 这里排序loopItem为了让item尽量重复用同一个，而不是在拖拽时不断变化
            //if (m_LoopItems.Count > 0)
            //{
            //    int dis = m_LoopItems[0].Index - index;
            //    List<UILoopItem> temp = ListPool<UILoopItem>.Get();
            //    if (dis > 0)
            //    {
            //        for (int i = m_LoopItems.Count - 1; i >= 0; i--)
            //        {
            //            int newIndex = i + dis;
            //            if (newIndex >= 0 && newIndex < m_LoopItems.Count)
            //            {
            //                m_LoopItems[newIndex] = m_LoopItems[i];
            //                m_LoopItems[i] = null;
            //            }
            //            else
            //            {
            //                temp.Add(m_LoopItems[i]);
            //            }
            //        }
            //    }
            //    else if (dis < 0)
            //    {
            //        for (int i = 0; i < m_LoopItems.Count; i++)
            //        {
            //            int newIndex = i + dis;
            //            if (newIndex >= 0 && newIndex < m_LoopItems.Count)
            //            {
            //                m_LoopItems[newIndex] = m_LoopItems[i];
            //                m_LoopItems[i] = null;
            //            }
            //            else
            //            {
            //                temp.Add(m_LoopItems[i]);
            //            }
            //        }
            //    }
            //    for (int i = 0; i < m_LoopItems.Count; i++)
            //    {
            //        if (m_LoopItems[i] == null)
            //        {
            //            m_LoopItems[i] = temp[0];
            //            temp.RemoveAt(0);
            //        }
            //    }
            //    ListPool<UILoopItem>.Release(temp);
            //}

            for (int i = 0; i < maxCount; i++)
            {
                int listIndex = index + i;
                if (m_Datas.Count > listIndex)
                {
                    if (m_LoopItems.Count > i)
                    {
                        m_LoopItems[i].UpdateData(m_Datas, listIndex, m_UserData);
                    }
                    else
                    {
                        var go = m_PrefabPool.Get();
                        RectTransform rectTransform = go.transform as RectTransform;
                        rectTransform.SetPivot(m_ItemPivot);
                        switch (m_ItemPivot)
                        {
                            case PivotPresets.TopLeft:
                            case PivotPresets.TopCenter:
                                rectTransform.SetAnchor(AnchorPresets.TopLeft);
                                break;
                            case PivotPresets.TopRight:
                                rectTransform.SetAnchor(AnchorPresets.TopRight);
                                break;
                            case PivotPresets.MiddleLeft:
                            case PivotPresets.MiddleCenter:
                                rectTransform.SetAnchor(AnchorPresets.MiddleLeft);
                                break;
                            case PivotPresets.MiddleRight:
                                rectTransform.SetAnchor(AnchorPresets.MiddleRight);
                                break;
                            case PivotPresets.BottomLeft:
                            case PivotPresets.BottomCenter:
                                rectTransform.SetAnchor(AnchorPresets.BottomLeft);
                                break;
                            case PivotPresets.BottomRight:
                                rectTransform.SetAnchor(AnchorPresets.BottomRight);
                                break;
                            default:
                                break;
                        }
                        rectTransform.SetParent(m_Content);
                        rectTransform.localScale = m_PrefabPool.Prefab.transform.localScale;
                        UILoopItem loopItem = go.GetOrAddComponent(m_ItemType) as UILoopItem;
                        loopItem.UIScrollView = this;
                        m_LoopItems.Add(loopItem);
                        //if (m_LoopItems.Count > listIndex)
                        //else
                        //    m_LoopItems.Insert(listIndex, loopItem);
                        loopItem.UpdateData(m_Datas, listIndex, m_UserData);
                    }
                }
                else if (m_LoopItems.Count > i)
                {
                    m_LoopItems[i].transform.localPosition = new Vector3(-10000, -10000);
                }
            }
            while (m_LoopItems.Count > maxCount)
            {
                UILoopItem loopItem = m_LoopItems[m_LoopItems.Count - 1];
                m_PrefabPool.Recycle(loopItem.gameObject);
                m_LoopItems.RemoveAt(m_LoopItems.Count - 1);
            }
        }

        public Vector3 GetLocalPositionByIndex(int index)
        {
            float x, y, z;
            x = y = z = 0.0f;

            int remain = index % m_CountOfOtherAxis;
            index /= m_CountOfOtherAxis;
            switch (m_AxisType)
            {
                case Axis.Horizontal:
                    y = -m_VerticalStartSpace - remain * (m_ChildHeight + m_VerticalSpace);
                    switch (m_AlignType)
                    {
                        case AlignType.Center:
                        case AlignType.Left:
                        case AlignType.Top:
                            x = m_HorizontalStartSpace + index * (m_ChildWidth + m_HorizontalSpace);
                            break;
                        case AlignType.Right:
                        case AlignType.Bottom:
                            x = m_HorizontalStartSpace - index * (m_ChildWidth + m_HorizontalSpace);
                            break;
                        default:
                            break;
                    }
                    break;
                case Axis.Vertical:
                    x = m_HorizontalStartSpace + remain * (m_ChildWidth + m_HorizontalSpace);
                    switch (m_AlignType)
                    {
                        case AlignType.Center:
                        case AlignType.Left:
                        case AlignType.Top:
                            y = -m_VerticalStartSpace - index * (m_ChildHeight + m_VerticalSpace);
                            break;
                        case AlignType.Right:
                        case AlignType.Bottom:
                            y = m_VerticalStartSpace + index * (m_ChildHeight + m_VerticalSpace);
                            break;
                        default:
                            break;
                    }
                    break;
            }
            return new Vector3(x, y, z);
        }

        private void OnValueChanged(Vector2 vec)
        {
            int index = GetCurrentItemIndex();
            if (m_CurrentIndex != index)
            {
                m_CurrentIndex = index;
                UpdateContent(index);
            }
        }

        private int GetCurrentItemIndex()
        {
            int index = 0;
            switch (m_AxisType)
            {
                case Axis.Horizontal:
                    if (m_AlignType == AlignType.Left && m_Content.anchoredPosition.x >= 0) return 0;
                    if (m_AlignType == AlignType.Right && m_Content.anchoredPosition.x <= 0) return 0;
                    index = Mathf.FloorToInt((Mathf.Abs(m_Content.anchoredPosition.x) - m_HorizontalStartSpace) / (m_ChildWidth + m_HorizontalSpace)) * m_CountOfOtherAxis;
                    break;
                case Axis.Vertical:
                    if (m_AlignType == AlignType.Bottom && m_Content.anchoredPosition.y >= 0) return 0;
                    if (m_AlignType == AlignType.Top && m_Content.anchoredPosition.y <= 0) return 0;
                    index = Mathf.FloorToInt((Mathf.Abs(m_Content.anchoredPosition.y) - m_VerticalStartSpace) / (m_ChildHeight + m_VerticalSpace)) * m_CountOfOtherAxis;
                    break;
            }
            return Mathf.Max(0, index);
        }

        public void Select(int index)
        {
            if (m_Datas == null) return;

            if (m_SelectIndex == index) return;

            m_SelectIndex = index;

            foreach (var item in m_LoopItems)
            {
                item.CheckSelect(index);
            }

            if (index >= 0)
            {
                int maxCount = m_AxisType == Axis.Horizontal ? m_HorizontalCount : m_VerticalCount;
                int other = m_AxisType == Axis.Horizontal ? m_VerticalCount : m_HorizontalCount;
                MoveTo(index - (maxCount - 1) * other / 2);
            }

            OnSelectChanged?.Invoke(index);
        }

        /// <summary>
        /// 移动
        /// </summary>
        public void MoveTo(int index, float duration = 0.3f)
        {
            index = Mathf.Clamp(index, 0, m_Datas.Count - 1);

            int xIndex = 0, yIndex = 0;

            if (m_AxisType == Axis.Horizontal)
            {
                yIndex = index % m_CountOfOtherAxis;
                xIndex = index / m_CountOfOtherAxis;
            }
            else
            {
                xIndex = index % m_CountOfOtherAxis;
                yIndex = index / m_CountOfOtherAxis;
            }

            m_ScrollRect.StopMovement();

            // x
            float pWidth = (m_Content.transform.parent as RectTransform).rect.width;
            float sWidth = m_Content.rect.width;
            float x = m_HorizontalStartSpace + (m_ChildWidth + m_HorizontalSpace) * xIndex;
            float limit = 0;
            if (sWidth > pWidth)
            {
                limit = sWidth - pWidth;
            }

            if (m_AlignType == AlignType.Left)
                x = Mathf.Clamp(-x, -limit, limit);
            else
                x = Mathf.Clamp(x, -limit, limit);

            // y
            float pHeight = (m_Content.transform.parent as RectTransform).rect.height;
            float sHeight = m_Content.rect.height;
            float y = m_VerticalStartSpace + (m_ChildHeight + m_VerticalSpace) * yIndex;
            limit = 0;
            if (sHeight > pHeight)
            {
                limit = sHeight - pHeight;
            }

            if (m_AlignType == AlignType.Top)
                y = Mathf.Clamp(y, -limit, limit);
            else
                y = Mathf.Clamp(-y, -limit, limit);

            if (m_Tweener != null)
            {
                UpdateContent(GetCurrentItemIndex());
                m_Tweener.Kill();
                m_Tweener = null;
            }

            if (duration > 0 && Vector2.Distance(new Vector2(x, y), m_Content.anchoredPosition) > 1f)
            {
                m_Tweener = m_Content.DOAnchorPos(new Vector2(x, y), duration);
            }
            else
            {
                m_Content.anchoredPosition = new Vector2(x, y);
            }

            if (m_Tweener != null)
            {
                m_Tweener.onComplete += () =>
                {
                    UpdateContent(GetCurrentItemIndex());
                };
            }
            else
            {
                UpdateContent(GetCurrentItemIndex());
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!m_IsPaging) return;

            // 计算最近的一页 并设置
            MoveTo(GetCurrentItemIndex());
        }

        private void Update()
        {
            if (m_Datas == null || m_Datas.Count == 0 || m_PrefabPool == null || m_ItemType == null) return;

            var parent = (m_ScrollRect.transform as RectTransform);
            if (parentRect != parent.rect)
            {
                UpdateList(m_Datas, m_PrefabPool.Prefab, m_ItemType, true, m_UserData);
            }
        }

        public void Release()
        {
            m_LoopItems.Clear();
            if (m_PrefabPool != null)
                m_PrefabPool.RecycleUseList();
        }
    }
}

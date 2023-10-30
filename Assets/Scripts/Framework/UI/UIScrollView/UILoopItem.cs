using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkierFramework
{
    public class UILoopItem : UISubView
    {
        protected int m_Index;
        protected RectTransform m_RectTransform;
        public int Index => m_Index;
        public UIScrollView UIScrollView { get; set; }

        public override void OnInit()
        {
            base.OnInit();
            m_RectTransform = transform as RectTransform;
        }

        public void UpdateData(IList dataList, int index, object userData)
        {
            if (!isInit)
            {
                OnInit();
            }
            m_Index = index;
            m_RectTransform.localPosition = Vector3.zero;
            m_RectTransform.anchoredPosition = UIScrollView.GetLocalPositionByIndex(index);
            CheckSelect(UIScrollView.SelectIndex);
            OnUpdateData(dataList, index, userData);
        }

        /// <summary>
        /// 选中切换时
        /// </summary>
        public virtual void CheckSelect(int index)
        {

        }

        /// <summary>
        /// Item的宽高
        /// </summary>
        public virtual Vector2 GetRect()
        {
            return new Vector2(m_RectTransform.rect.width * m_RectTransform.localScale.x, m_RectTransform.rect.height * m_RectTransform.localScale.y);
        }

        /// <summary>
        /// 刷新数据时
        /// </summary>
        protected virtual void OnUpdateData(IList dataList, int index, object userData)
        {
            
        }
    }
}

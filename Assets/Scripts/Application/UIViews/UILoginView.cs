using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System.Collections;

namespace SkierFramework
{
    public class UITestItem : UILoopItem
    {
        #region 控件绑定变量声明，自动生成请勿手改
#pragma warning disable 0649
        [ControlBinding]
        private TextMeshProUGUI Text;
        [ControlBinding]
        private Button Button;
        [ControlBinding]
        private GameObject Select;

#pragma warning restore 0649
        #endregion

        public override void OnInit()
        {
            base.OnInit();
            Button.AddClick(() =>
            {
                UIScrollView.Select(Index);
            });
        }

        public override void CheckSelect(int index)
        {
            base.CheckSelect(index);
            Select.SetActive(index == Index);
        }

        protected override void OnUpdateData(IList dataList, int index, object userData)
        {
            base.OnUpdateData(dataList, index, userData);

            Text.text = dataList[index].ToString();
        }
    }

    public class UILoginView : UIView
    {
        #region 控件绑定变量声明，自动生成请勿手改
#pragma warning disable 0649
        [ControlBinding]
        private Button ButtonStart;
        [ControlBinding]
        private Button ButtonSetting;
        [ControlBinding]
        private UIScrollView UIScrollView;
        [ControlBinding]
        private GameObject Item;

#pragma warning restore 0649
        #endregion

        public override void OnInit(UIControlData uIControlData, UIViewController controller)
        {
            base.OnInit(uIControlData, controller);

            ButtonStart.AddClick(() =>
            {
                UIManager.Instance.Open(UIType.UIMessageBoxView, ObjectPool<MessageBoxData>.Get().Set("提示", "测试弹窗。", () =>
                {
                    Debug.Log("确认");
                }));
            });
            ButtonSetting.AddClick(() =>
            {
                UIManager.Instance.Open(UIType.UITestView);
            });

            UIScrollView.OnSelectChanged += (index) =>
            {
                Debug.Log("选中了：" + index);
            };
        }

        public override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            // 模拟100个数据
            List<int> list = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                list.Add(i);
            }
            UIScrollView.UpdateList(list, Item, typeof(UITestItem));
            UIScrollView.Select(10);
        }

        public override void OnAddListener()
        {
            base.OnAddListener();
        }

        public override void OnRemoveListener()
        {
            base.OnRemoveListener();
        }

        public override void OnClose()
        {
            base.OnClose();
        }
    }
}

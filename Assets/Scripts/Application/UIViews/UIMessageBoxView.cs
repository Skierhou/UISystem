using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace SkierFramework
{
    public enum MessageBoxType
    {
        TwoButton,
        OneButton,
    }

    public class MessageBoxData
    {
        const string DefaultConfirmName = "确认";
        const string DefaultCancelName = "取消";

        public string title;
        public string content;
        public Action confirm;
        public Action cancel;
        public string confirmName;
        public string cancelName;
        public MessageBoxType type;

        public MessageBoxData Set(string title, string content, Action confirm, Action cancel = null
            , string confirmName = DefaultConfirmName, string cancelName = DefaultCancelName)
        {
            this.title = title;
            this.content = content;
            this.confirm = confirm;
            this.cancel = cancel;
            this.confirmName = confirmName;
            this.cancelName = cancelName;
            this.type = MessageBoxType.TwoButton;
            return this;
        }

        public MessageBoxData SetOneButton(string title, string content, Action confirm, Action cancel = null
            , string confirmName = DefaultConfirmName)
        {
            this.title = title;
            this.content = content;
            this.confirm = confirm;
            this.cancel = cancel;
            this.confirmName = confirmName;
            this.cancelName = null;
            this.type = MessageBoxType.OneButton;
            return this;
        }
    }

    public class UIMessageBoxView : UIView
    {
        #region 控件绑定变量声明，自动生成请勿手改
#pragma warning disable 0649
        [ControlBinding]
        protected Button ButtonConfirm;
        [ControlBinding]
        protected TextMeshProUGUI TextTitle;
        [ControlBinding]
        protected TextMeshProUGUI TextContent;
        [ControlBinding]
        protected Button[] ButtonCloses;
        [ControlBinding]
        protected TextMeshProUGUI TextConfirm;
        [ControlBinding]
        protected TextMeshProUGUI TextCancel;

#pragma warning restore 0649
        #endregion

        MessageBoxData data;
        protected UIType uiType = UIType.UIMessageBoxView;

        public override void OnInit(UIControlData uIControlData, UIViewController controller)
        {
            base.OnInit(uIControlData, controller);

            foreach (var button in ButtonCloses)
            {
                button.AddClick(() =>
                {
                    data.cancel?.Invoke();
                    UIManager.Instance.Close(Controller.uiType);
                });
            }
            ButtonConfirm.AddClick(() =>
            {
                data.confirm?.Invoke();
                UIManager.Instance.Close(Controller.uiType);
            });
        }

        public override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            data = userData as MessageBoxData;

            TextTitle.text = data.title;
            TextContent.text = data.content;
            TextConfirm.text = data.confirmName;
            TextCancel.text = data.cancelName;

            LayoutRebuilder.ForceRebuildLayoutImmediate(TextContent.rectTransform);

            ButtonCloses[1].gameObject.SetActive(data.type == MessageBoxType.TwoButton);
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
            ObjectPool<MessageBoxData>.Release(data);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkierFramework
{
    public class UISubView : MonoBehaviour, IBindableUI
    {
        private bool isBind = false;

        private void Awake()
        {
            OnInit();
            OnAddListener();
        }

        private void OnEnable()
        {
            OnOpen();
        }

        private void OnDisable()
        {
            OnClose();
        }

        private void OnDestroy()
        {
            OnRelease();
            OnRemoveListener();
        }

        private void Bind()
        {
            if (isBind) return;
            var uIControlData = GetComponent<UIControlData>();
            if (uIControlData != null)
                uIControlData.BindDataTo(this);
            isBind = true;
        }

        public virtual void OnInit() { Bind(); }

        public virtual void OnAddListener() { }

        public virtual void OnRemoveListener() { }

        public virtual void OnOpen() { Bind(); }

        public virtual void OnClose() { }

        public virtual void OnRelease() { }
    }
}

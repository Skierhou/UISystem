using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace SkierFramework
{
    public class UILoadingView : UIView
    {
        #region 控件绑定变量声明，自动生成请勿手改
#pragma warning disable 0649
        [ControlBinding]
        private Slider Slider;
        [ControlBinding]
        private TextMeshProUGUI TextDes;
        [ControlBinding]
        private TextMeshProUGUI TextValue;

#pragma warning restore 0649
        #endregion

        public override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            Reset();
            Slider.onValueChanged.AddListener((value) =>
            {
                TextValue.text = string.Format("{0:F0}%", value * 100);
            });
        }

        public void SetLoading(float value, string desc)
        {
            Slider.DOValue(value, 0.3f);
            TextDes.text = desc;
        }

        public override void OnClose()
        {
            base.OnClose();
            Reset();
        }

        public void Reset()
        {
            Slider.value = 0;
            TextDes.text = "";
            TextValue.text = "0%";
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkierFramework
{
    public enum UIAppearType
    {
        None,
        Animation,
        Alpha,
        AlphaAndAnimation,
        Scale,
        ScaleAndAlpha,
    }

    public class UIViewAnim : MonoBehaviour
    {
        public UIAppearType openType = UIAppearType.None;
        public UIAppearType closeType = UIAppearType.None;

        public Animation animtion;
        public Transform target;
        public float animTime = 0.2f;

        public float playRate;
        private float duration;
        private Action callback;
        private CanvasGroup canvasGroup;
        private Transform Target => target != null ? target : transform;
        private enum ViewType
        {
            None,
            Open,
            Close,
        }
        private ViewType viewType;

        private void Start()
        {
            canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
            playRate = 0;
        }

        public void Open(Action callback = null)
        {
            playRate = 0;
            if (viewType == ViewType.Close)
            {
                this.callback?.Invoke();
            }
            if (!gameObject.activeSelf)
            {
                callback?.Invoke();
                return;
            }
            viewType = ViewType.Open;
            this.callback = callback;
            duration = (1 - playRate) * animTime;

            if (openType == UIAppearType.Animation || openType == UIAppearType.AlphaAndAnimation)
            {
                if (animtion != null && animtion.clip != null)
                {
                    var state = animtion[animtion.clip.name];

                    if (state != null)
                    {
                        state.normalizedTime = playRate;
                        state.speed = state.length / animTime;
                    }
                    animtion.Play();
                }
                else if(openType == UIAppearType.Animation)
                {
                    playRate = 1;
                    Finish();
                }
            }
        }

        public void Close(Action callback = null)
        {
            if (viewType == ViewType.Open)
            {
                this.callback?.Invoke();
            }
            if (!gameObject.activeSelf)
            {
                callback?.Invoke();
                return;
            }

            viewType = ViewType.Close;
            this.callback = callback;
            duration = playRate * animTime;

            if (closeType == UIAppearType.Animation || closeType == UIAppearType.AlphaAndAnimation)
            {
                if (animtion != null && animtion.clip != null)
                {
                    var state = animtion[animtion.clip.name];

                    if (state != null)
                    {
                        state.normalizedTime = 1 - playRate;
                        state.speed = -1 * state.length / animTime;
                    }
                    animtion.Play();
                }
                else if (openType == UIAppearType.Animation)
                {
                    playRate = 0;
                    Finish();
                }
            }
        }

        private void LateUpdate()
        {
            switch (viewType)
            {
                case ViewType.Open:
                    PlayAlphaAndScale(openType, false);
                    break;
                case ViewType.Close:
                    PlayAlphaAndScale(closeType, true);
                    break;
                default:
                    break;
            }
        }
        
        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="back">是否倒放</param>
        private void PlayAlphaAndScale(UIAppearType type,bool back)
        {
            duration -= Time.deltaTime;
            if (duration <= 0)
            {
                playRate = back ? 0 : 1;
                Finish();
            }
            else
            {
                playRate += Time.deltaTime / animTime * (back ? -1 : 1);
            }

            if (type == UIAppearType.Alpha || type == UIAppearType.AlphaAndAnimation || type == UIAppearType.ScaleAndAlpha)
            {
                canvasGroup.alpha = Mathf.Lerp(0, 1, playRate);
            }
            if (type == UIAppearType.Scale || type == UIAppearType.ScaleAndAlpha)
            {
                Target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, playRate);
            }
        }
        private void Finish()
        {
            callback?.Invoke();
            callback = null;
            viewType = ViewType.None;
        }
    }
}

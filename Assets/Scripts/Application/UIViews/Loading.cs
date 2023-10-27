using System;
using System.Collections;
using UnityEngine;

namespace SkierFramework
{
    public class LoadingData
    {
        public LoadingFunc loadingFunc;
        public bool isCleanupAsset = false;
    }

    public delegate IEnumerator LoadingFunc(Action<float, string> loadingRefresh);

    /// <summary>
    /// 实际游戏中的loading
    /// </summary>
    public class Loading : SingletonMono<Loading>
    {
        private LoadingData _loadingData;
        private Coroutine _cor;

        public void StartLoading(LoadingFunc loadingFunc, bool isCleanupAsset = false)
        {
            StartLoading(new LoadingData { loadingFunc = loadingFunc, isCleanupAsset = isCleanupAsset });
        }

        public void StartLoading(LoadingData loadingData)
        {
            //开启UI
            UIManager.Instance.Open(UIType.UILoadingView);

            if (loadingData.loadingFunc != null)
            {
                _loadingData = loadingData;

                if (_cor != null)
                {
                    StopCoroutine(_cor);
                }
                _cor = StartCoroutine(CorLoading());
            }
            else
            {
                Debug.LogError("加载错误,没有参数LoadingData！");
            }
        }

        private IEnumerator CorLoading()
        {
            yield return StartCoroutine(_loadingData.loadingFunc(RefreshLoading));

            if (_loadingData != null && _loadingData.isCleanupAsset)
            {
                yield return ResourceManager.Instance.CleanupAsync();
                yield return Resources.UnloadUnusedAssets();
            }

            Pool.ReleaseAll();
            yield return null;

            GC.Collect();
            yield return null;

            Exit();

            _cor = null;
        }

        private void RefreshLoading(float loading, string desc)
        {
            // 刷新
            var view = UIManager.Instance.GetView<UILoadingView>(UIType.UILoadingView);
            if (view != null)
            {
                view.SetLoading(loading, desc);
            }
            if (!string.IsNullOrEmpty(desc))
            {
                Debug.Log(desc);
            }
        }

        private void Exit()
        {
            // 关闭UI
            UIManager.Instance.Close(UIType.UILoadingView);

            ObjectPool<LoadingData>.Release(_loadingData);
            _loadingData = null;
        }
    }
}

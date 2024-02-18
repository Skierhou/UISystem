using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using static UnityEngine.AddressableAssets.Addressables;

namespace SkierFramework
{
    public class ResourceManager : Singleton<ResourceManager>
    {
        /// <summary>
        /// 已加载/正在加载中资源名对应的句柄
        /// </summary>
        private Dictionary<string, AsyncOperationHandle> _handleCaches = new Dictionary<string, AsyncOperationHandle>();
        /// <summary>
        /// 正在进行加载状态中的资源的数量
        /// </summary>
        private int _loadingAssetCount = 0;
        /// <summary>
        /// 常驻内存中的资源路径哈希集
        /// </summary>
        private HashSet<string> _residentAssetsHashSet = new HashSet<string>();
        /// <summary>
        /// 调用清除时使用
        /// </summary>
        private HashSet<string> _clearAssetsSet = new HashSet<string>();
        /// <summary>
        /// 资源的引用个数
        /// </summary>
        private Dictionary<string, int> _loadedAssetInstanceCountDic = new Dictionary<string, int>();
        /// <summary>
        /// 已实例化对象对应的Key
        /// key: instanceId
        /// value: path
        /// </summary>
        private Dictionary<int, string> _objectInstanceIdKeyDic = new Dictionary<int, string>();
        /// <summary>
        /// instancePool
        /// </summary>
        private InstancePool _instancePool;
        /// <summary>
        /// 更新列表
        /// </summary>
        private List<string> updateKeys;

        /// <summary>
        /// 是否正在加载资源
        /// </summary>
        public bool IsProcessLoading
        {
            get => _loadingAssetCount > 0;
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            _instancePool = new InstancePool();
        }

        #region 初始化/清除
        /// <summary>
        /// 初始化
        /// </summary>
        public IEnumerator InitializeAsync()
        {
            yield return Addressables.InitializeAsync();
        }

        /// <summary>
        /// 检查更新
        /// </summary>
        public IEnumerator CheckUpdateCor(Action<long> callback = null)
        {
            // 1. 检查更新
            AsyncOperationHandle<List<string>> updateHandle = Addressables.CheckForCatalogUpdates(false);
            yield return updateHandle;

            List<string> updateList = null;
            if (updateHandle.Status == AsyncOperationStatus.Succeeded)
            {
                updateList = updateHandle.Result;
            }

            if (updateList == null || updateList.Count == 0)
            {
                callback?.Invoke(0);
                yield break;
            }

            // 2.开始更新
            AsyncOperationHandle<List<IResourceLocator>> updateHandler = Addressables.UpdateCatalogs(updateList, false);
            yield return updateHandler;

            // 3.获取更新资源的key
            updateKeys = new List<string>();
            foreach (IResourceLocator locator in updateHandler.Result)
            {
                if (locator is ResourceLocationMap map)
                {
                    foreach (var item in map.Locations)
                    {
                        if (item.Value.Count == 0) continue;
                        string key = item.Key.ToString();
                        if (int.TryParse(key, out int resKey)) continue;

                        if (!updateKeys.Contains(key))
                            updateKeys.Add(key);
                    }
                }
            }

            // 4.判断下载资源大小
            AsyncOperationHandle<long> downLoadSizeTotal = Addressables.GetDownloadSizeAsync(updateKeys);
            yield return downLoadSizeTotal;

            long updateSize = downLoadSizeTotal.Result;

            // 6.清除
            Addressables.Release(updateHandle);
            Addressables.Release(updateHandler);
            Addressables.Release(downLoadSizeTotal);

            callback?.Invoke(updateSize);
        }

        public IEnumerator DownLoadCor(Action<long, long> callback = null)
        {
            // 一个一个下载
            //for (int i = 0; i < updateKeys.Count; i++)
            //{
            //    string key = updateKeys[i];

            //    AsyncOperationHandle downLoad = Addressables.DownloadDependenciesAsync(key);
            //    float lastSize = downloadSize;
            //    while (!downLoad.IsDone)
            //    {
            //        downloadSize = lastSize + downLoad.GetDownloadStatus().DownloadedBytes / 1024.0f / 1024.0f;
            //        refreshUI?.Invoke(i * 1.0f / updateKeys.Count, string.Format("已下载：{0:f2}/MB  需下载：{1:f2}/MB", downloadSize, updateSizeMB));
            //        yield return null;
            //    }
            //    Addressables.Release(downLoad);
            //}

            // 直接全部下载
            AsyncOperationHandle downLoad = Addressables.DownloadDependenciesAsync(updateKeys, MergeMode.None);
            long totalSize = 0;
            while (!downLoad.IsDone)
            {
                var status = downLoad.GetDownloadStatus();
                totalSize = status.TotalBytes;
                callback?.Invoke(status.DownloadedBytes, status.TotalBytes);
                yield return null;
            }
            Addressables.Release(downLoad);
        }


        /// <summary>
        /// 清除所有常驻资源之外的资源
        /// </summary>
        public IEnumerable CleanupAsync()
        {
            yield return new WaitUntil(() => {
                return !IsProcessLoading;
            });

            Cleanup();
        }

        /// <summary>
        /// 清除所有常驻资源之外的资源
        /// </summary>
        public void Cleanup()
        {
            foreach (var item in _handleCaches)
            {
                if (!_residentAssetsHashSet.Contains(item.Key))
                {
                    _clearAssetsSet.Add(item.Key);
                    Addressables.Release(item.Value);
                }
            }
            foreach (var key in _clearAssetsSet)
            {
                if (_spriteCache.TryGetValue(key, out SpriteAtlas spriteAtlas))
                {
                    spriteAtlas.Cleanup();
                    _spriteCache.Remove(key);
                }

                _handleCaches.Remove(key);
                _loadedAssetInstanceCountDic.Remove(key);
                _instancePool.Clear(key);
            }
            _clearAssetsSet.Clear();
        }

        /// <summary>
        /// 增加常驻资源
        /// </summary>
        public void AddResidentAsset(string key)
        {
            _residentAssetsHashSet.Add(key);
        }
        #endregion

        #region 实例化和回收对象
        public AsyncOperationHandle InstantiateAsync(string path, Action<UnityEngine.GameObject> callback, bool active = true)
        {
            AsyncOperationHandle operationHandle = default;

            if (!_handleCaches.ContainsKey(path))
            {
                //未加载过此资源
                operationHandle = LoadAssetAsync<GameObject>(path, (obj) =>
                {
                    //_loadedAssetInstanceCountDic[path]--;
                    if (obj != null)
                    {
                        InternalInstantiate(path, callback, active);
                    }
                    else
                    {
                        callback?.Invoke(null);
                    }
                });
            }
            else
            {
                operationHandle = _handleCaches[path];
                //已加载此资源且加载完成
                if (operationHandle.IsDone)
                {
                    InternalInstantiate(path, callback, active);
                }
                else
                {//正在加载
                    operationHandle.Completed += (result) =>
                    {
                        InternalInstantiate(path, callback, active);
                    };
                }
            }

            return operationHandle;
        }

        public IEnumerable CoInstantiateAsync(string path, Action<UnityEngine.GameObject> callback, bool active = true)
        {
            if (!_handleCaches.ContainsKey(path))
            {
                //未加载过此资源
                yield return LoadAssetAsync<GameObject>(path, null);
                InternalInstantiate(path, callback, active);
            }
            else
            {
                var handle = _handleCaches[path];
                //已加载此资源且加载完成
                if (handle.IsDone)
                {
                    InternalInstantiate(path, callback, active);
                }
                else
                {//正在加载
                    yield return handle;
                    InternalInstantiate(path, callback, active);
                }
            }
        }

        public void Recycle(UnityEngine.GameObject instanceObject, bool forceDestroy = false)
        {
            if (instanceObject == null)
            {
                return;
            }

            int id = instanceObject.GetInstanceID();
            if (_objectInstanceIdKeyDic.ContainsKey(id))
            {
                _instancePool.Recycle(_objectInstanceIdKeyDic[id], instanceObject, forceDestroy);
                _loadedAssetInstanceCountDic[_objectInstanceIdKeyDic[id]]--;
                _objectInstanceIdKeyDic.Remove(id);
            }
            else
            {
                Debug.LogErrorFormat("此模块不回收不是从这个模块实例化出去的对象：{0}", instanceObject.name);
                GameObject.Destroy(instanceObject);
            }
        }

        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        private void InternalInstantiate(string path, Action<UnityEngine.GameObject> callback, bool active = true)
        {
            GameObject result = _instancePool.Get(path);
            GameObject invokeResult = null;

            if (result == null)
            {
                if (_handleCaches[path].Result != null)
                {
                    invokeResult = _handleCaches[path].Result as GameObject;
                    invokeResult = GameObject.Instantiate(invokeResult);
                }
            }
            else
            {
                invokeResult = result;
            }

            if (invokeResult != null)
            {
                _instancePool.InitInst(invokeResult, active);
                _objectInstanceIdKeyDic[invokeResult.GetInstanceID()] = path;
                _loadedAssetInstanceCountDic[path]++;
            }
            callback?.Invoke(invokeResult);
        }
        #endregion

        #region 资源加载/卸载   
        /// <summary>
        /// 异步加载
        /// </summary>
        public AsyncOperationHandle LoadAssetAsync<T>(string path, Action<T> onComplete, bool autoUnload = false) where T : class
        {
            if (string.IsNullOrEmpty(path))
            {
                onComplete?.Invoke(null);
                return default;
            }
            AsyncOperationHandle handle;
            //已加载或在加载中
            if (_handleCaches.TryGetValue(path, out handle))
            {
                if (handle.IsDone)
                {
                    onComplete?.Invoke(_handleCaches[path].Result as T);
                }
                else
                {
                    handle.Completed += (result) =>
                    {
                        if (result.Status == AsyncOperationStatus.Succeeded)
                        {
                            onComplete?.Invoke(result.Result as T);
                            if (autoUnload)
                            {
                                UnLoadAsset(path);
                            }
                        }
                        else
                        {
                            Debug.LogErrorFormat("[LoadAssetAsync] {0} 加载失败！", path);
                            onComplete?.Invoke(null);
                        }
                    };
                }
                return handle;
            }
            else //未加载过
            {
                _loadingAssetCount++;
                _loadedAssetInstanceCountDic.Add(path, 1);
                //处理异步加载
                handle = Addressables.LoadAssetAsync<T>(path);
                handle.Completed += (op) =>
                {
                    _loadingAssetCount--;
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        onComplete?.Invoke(op.Result as T);
                        if (autoUnload)
                        {
                            UnLoadAsset(path);
                        }
                    }
                    else
                    {
                        //Debug.LogErrorFormat("[LoadAssetAsync] {0} 加载失败！", path);
                        onComplete?.Invoke(null);
                    }
                };
                _handleCaches.Add(path, handle);
                return handle;
            }
        }

        public AsyncOperationHandle LoadAssetAsync<T, T1>(string path, Action<T, T1> onComplete, T1 data1, bool autoUnload = false) where T : class
        {
            return LoadAssetAsync<T>(path, (asset) =>
            {
                onComplete?.Invoke(asset, data1);
            }, autoUnload);
        }

        /// <summary>
        /// 直接卸载资源
        /// </summary>
        public void UnLoadAsset(string path)
        {
            //判断卸载是否是一个常驻资源
            if (_residentAssetsHashSet.Contains(path))
            {
                Debug.LogErrorFormat("[UnLoadAsset] 禁止卸载常驻资源：{0} ！", path);
                return;
            }

            AsyncOperationHandle handle;
            if (_handleCaches.TryGetValue(path, out handle))
            {
                if (!handle.IsDone)
                {
                    Debug.LogErrorFormat("[UnLoadAsset] 卸载了一个未加载完成的资源：{0} ！", path);
                }
                Debug.Log(string.Format("[UnLoadAsset] 卸载资源：{0} ！", path));

                if (_spriteCache.TryGetValue(path, out SpriteAtlas spriteAtlas))
                {
                    spriteAtlas.Cleanup();
                    _spriteCache.Remove(path);
                }
                _handleCaches.Remove(path);
                _loadedAssetInstanceCountDic.Remove(path);
                Addressables.Release(handle);
            }
            else
            {
                Debug.LogErrorFormat("[UnLoadAsset] 卸载未加载资源：{0} ！", path);
            }
        }

        /// <summary>
        /// 释放资源引用，当引用数为0时 自动卸载
        /// </summary>
        public void ReleaseRef(string path)
        {
            if (_loadedAssetInstanceCountDic.TryGetValue(path, out int count))
            {
                _loadedAssetInstanceCountDic[path] = --count;
                if (count <= 0)
                {
                    UnLoadAsset(path);
                }
            }
        }
        #endregion

        #region 预加载/缓存
        public AsyncOperationHandle PreLoadAssetAsync<T>(string path, Action<T> callback = null) where T : class
        {
            return LoadAssetAsync<T>(path, (obj) =>
            {
                callback?.Invoke(obj);
            });
        }

        public IEnumerator CoPreLoadAsset<T>(string path) where T : class
        {
            yield return LoadAssetAsync<T>(path, null);
        }

        public IEnumerator CoPreLoadAssetsByLabelAsync(string label)
        {
            List<string> keyList = TryGetKeysListByLabel(label);
            List<AsyncOperationHandle> handleList = ListPool<AsyncOperationHandle>.Get();
            foreach (var key in keyList)
            {
                var handle = LoadAssetAsync<UnityEngine.Object>(key, null);
                handleList.Add(handle);
            }
            WaitUntil waitUntil = new WaitUntil(() => {
                foreach (var handle in handleList)
                {
                    if (!handle.IsDone)
                        return false;
                }
                return true;
            });
            yield return waitUntil;
            ListPool<AsyncOperationHandle>.Release(handleList);
            ListPool<string>.Release(keyList);
        }

        private List<string> TryGetKeysListByLabel(string lblName)
        {
            var t = Addressables.ResourceLocators;
            List<string> result = ListPool<string>.Get();
            foreach (var locator in t)
            {
                if (!(locator is ResourceLocationMap))
                {
                    continue;
                }

                ResourceLocationMap locationMap = locator as ResourceLocationMap;
                locationMap.Locate(lblName, typeof(object), out var locationList);
                if (locationList == null)
                {
                    break;
                }

                foreach (var location in locationList)
                {
                    result.Add(location.PrimaryKey);
                }
                break;
            }

            return result;
        }

        public bool TryGetAsset<T>(string path, out T target) where T : class
        {
            target = null;
            bool result = false;
            AsyncOperationHandle handle;
            if (_handleCaches.TryGetValue(path, out handle))
            {
                if (handle.IsDone)
                {
                    target = handle.Result as T;
                    result = true;
                }
            }
            return result;
        }
        #endregion

        #region 图片加载
        /// <summary>
        /// SpriteAtlas.GetSprite()会clone一份，不会重复使用 因此需要缓存
        /// </summary>
        private class SpriteAtlas
        {
            public UnityEngine.U2D.SpriteAtlas spriteAtlas;
            private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

            public Sprite Get(string name)
            {
                if (!_spriteCache.TryGetValue(name, out Sprite sprite))
                {
                    sprite = spriteAtlas.GetSprite(name);
                    _spriteCache.Add(name, sprite);
                }
                return sprite;
            }

            public void Cleanup()
            {
                foreach (var sprite in _spriteCache.Values)
                {
                    GameObject.Destroy(sprite);
                }
                _spriteCache.Clear();
            }
        }
        private Dictionary<string, SpriteAtlas> _spriteCache = new Dictionary<string, SpriteAtlas>();
        public void LoadSpriteAsync(string atlasPath, string spriteName, Action<UnityEngine.Sprite> callback)
        {
            if (string.IsNullOrEmpty(atlasPath) || string.IsNullOrEmpty(spriteName))
            {
                Debug.LogErrorFormat("[LoadSpriteAsync] error：atlasPath = {0}, spriteName = {1}！", atlasPath, spriteName);
                callback.Invoke(null);
                return;
            }

            if (_spriteCache.TryGetValue(atlasPath, out SpriteAtlas atlas))
            {
                callback?.Invoke(atlas.Get(spriteName));
            }
            else
            {
                LoadAssetAsync<UnityEngine.U2D.SpriteAtlas>(atlasPath, (obj) =>
                {
                    if (obj == null)
                    {
                        Debug.LogErrorFormat("[LoadSpriteAsync] load failed：atlasPath = {0}！", atlasPath);
                        return;
                    }
                    if (_spriteCache.TryGetValue(atlasPath, out SpriteAtlas atlas))
                    {
                        callback?.Invoke(atlas.Get(spriteName));
                        return;
                    }
                    atlas = new SpriteAtlas { spriteAtlas = obj };
                    _spriteCache.Add(atlasPath, atlas);
                    callback?.Invoke(atlas.Get(spriteName));
                });
            }
        }
        #endregion

        #region 场景加载
        public AsyncOperationHandle LoadSceneAsync(string name, LoadSceneMode loadMode = LoadSceneMode.Single, Action<SceneInstance> callback = null)
        {
            var handle = Addressables.LoadSceneAsync(name, loadMode);
            handle.Completed += (op) =>
            {
                callback?.Invoke(op.Result);
            };
            return handle;
        }

        public void UnloadSceneAsync(SceneInstance sceneInstance, Action callback)
        {
            Addressables.UnloadSceneAsync(sceneInstance).Completed += (op) =>
            {
                callback?.Invoke();
            };
        }

        public IEnumerator CoUnloadSceneAsync(SceneInstance sceneInstance, Action callback)
        {
            var handle = Addressables.UnloadSceneAsync(sceneInstance);
            handle.Completed += (op) =>
            {
                callback?.Invoke();
            };
            yield return handle;
        }
        #endregion

        #region Text读取
        public IEnumerator<AsyncOperationHandle> CoReadTextStringAsync(string path, Action<string> callback)
        {
            yield return LoadAssetAsync<UnityEngine.TextAsset>(path, (obj) =>
            {
                if (obj == null)
                {
                    Debug.LogErrorFormat("[ReadTextStreamAsync] load failed：path = {0}！", path);
                    callback?.Invoke(string.Empty);
                    return;
                }

                callback?.Invoke(obj.text);
            });
        }

        public void ReadTextStringAsync(string path, Action<string> callback)
        {
            LoadAssetAsync<UnityEngine.TextAsset>(path, (obj) =>
            {
                if (obj == null)
                {
                    Debug.LogErrorFormat("[ReadTextStreamAsync] load failed：path = {0}！", path);
                    callback?.Invoke(string.Empty);
                    return;
                }

                callback?.Invoke(obj.text);
            });
        }

        public void ReadTextBytesAsync(string path, Action<byte[], object[]> callback, params object[] userData)
        {
            LoadAssetAsync<UnityEngine.TextAsset>(path, (obj) =>
            {
                if (obj == null)
                {
                    Debug.LogErrorFormat("[ReadTextStreamAsync] load failed：path = {0}！", path);
                    callback?.Invoke(null, userData);
                    return;
                }

                callback?.Invoke(obj.bytes, userData);
            }, true);
        }

        public IEnumerator<AsyncOperationHandle> CoReadTextBytesAsync(string path, Action<byte[]> callback)
        {
            yield return LoadAssetAsync<UnityEngine.TextAsset>(path, (obj) =>
            {
                if (obj == null)
                {
                    Debug.LogErrorFormat("[ReadTextStreamAsync] load failed：path = {0}！", path);
                    callback?.Invoke(null);
                    return;
                }

                callback?.Invoke(obj.bytes);
            }, true);
        }

        public byte[] ReadTextBytes(string path)
        {
            byte[] result = null;
            var handle = LoadAssetAsync<TextAsset>(path,
                (text) => {
                    if (text != null)
                    {
                        result = text.bytes;
                    }
                },
                true);
            handle.WaitForCompletion();
            return result;
        }
        #endregion

        #region Debug
        public void PrintState()
        {
            foreach (var item in _loadedAssetInstanceCountDic)
            {
                Debug.LogFormat("Asset Key: {0}, Count: {1}", item.Key, item.Value);
            }
        }
        #endregion
    }
}

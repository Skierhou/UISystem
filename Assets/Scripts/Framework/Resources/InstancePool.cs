using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkierFramework
{
    public class InstancePool
    {
        private static string GameObjectName = "GameObjectPool";
        private static string RecycleName = "RecyclePool";
        private Dictionary<string, Stack<GameObject>> _allInstances = new Dictionary<string, Stack<GameObject>>();
        private Transform _instancePoolTransRoot = null;
        private Transform _recyclePoolTransRoot = null;

        public InstancePool()
        {
            var go = new GameObject(GameObjectName);
            GameObject.DontDestroyOnLoad(go);

            _instancePoolTransRoot = go.transform;
            go.SetActive(true);

            _recyclePoolTransRoot = _instancePoolTransRoot.Find(RecycleName);
            if (_recyclePoolTransRoot == null)
            {
                _recyclePoolTransRoot = new GameObject(RecycleName).transform;
                _recyclePoolTransRoot.SetParent(_instancePoolTransRoot);
            }
            _recyclePoolTransRoot.gameObject.SetActive(false);
        }

        public GameObject Get(string key)
        {
            Stack<GameObject> objects = null;
            if (!_allInstances.TryGetValue(key, out objects))
            {
                return null;
            }
            else
            {
                if (objects == null || objects.Count == 0)
                {
                    return null;
                }

                return objects.Pop();
            }
        }

        public void Recycle(string key, GameObject obj, bool forceDestroy = false)
        {
            //强制删除
            if (forceDestroy)
            {
                if (Application.isPlaying)
                {
                    GameObject.Destroy(obj);
                }
                else
                {
                    GameObject.DestroyImmediate(obj);
                }
                return;
            }

            Stack<GameObject> objects = null;
            if (!_allInstances.TryGetValue(key, out objects))
            {
                objects = new Stack<GameObject>();
                _allInstances[key] = objects;
            }

            InitInst(obj, false);
            objects.Push(obj);
        }

        public void Clear(string key)
        {
            Stack<GameObject> objects = null;
            if (_allInstances.TryGetValue(key, out objects))
            {
                while (objects.Count > 0)
                {
                    GameObject objToDestroy = objects.Pop();
                    UnityEngine.AddressableAssets.Addressables.ReleaseInstance(objToDestroy);
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(objToDestroy);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(objToDestroy);
                    }
                }
            }
        }

        public void ClearAll()
        {
            foreach (var item in _allInstances)
            {
                while (item.Value.Count > 0)
                {
                    GameObject objToDestroy = item.Value.Pop();
                    UnityEngine.AddressableAssets.Addressables.ReleaseInstance(objToDestroy);
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(objToDestroy);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(objToDestroy);
                    }
                }
            }
            _allInstances.Clear();
        }

        public void InitInst(GameObject inst, bool active = true)
        {
            if (inst != null)
            {
                if (active)
                {
                    inst.transform.SetParent(_instancePoolTransRoot, true);
                }
                else
                {
                    inst.transform.SetParent(_recyclePoolTransRoot, true);
                }
            }
        }
    }
}

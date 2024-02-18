using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkierFramework
{
    public class PrefabPool
    {
        /// <summary>
        /// 如果名字一样，则使用同一个池子
        /// </summary>
        public static Dictionary<string, PrefabPool> s_Pools = new Dictionary<string, PrefabPool>();

        private string _poolName;
        private GameObject _prefab;
        private List<GameObject> _pool;
        private List<GameObject> _useList;

        public GameObject Prefab => _prefab;
        public List<GameObject> UseList => _useList;

        private PrefabPool() { }

        private void Init(string poolName, GameObject prefab)
        {
            _pool = ListPool<GameObject>.Get();
            _useList = ListPool<GameObject>.Get();
            _prefab = prefab;
            _poolName = poolName;
        }

        public static PrefabPool Get(string poolName)
        {
            if (!string.IsNullOrEmpty(poolName))
            {
                if (s_Pools.TryGetValue(poolName, out var prefabPool))
                {
                    return prefabPool;
                }
            }
            return null;
        }

        public static PrefabPool Create(GameObject prefab, string poolName = null)
        {
            if (prefab == null) return null;
            if (!string.IsNullOrEmpty(poolName))
            {
                if (s_Pools.TryGetValue(poolName, out var prefabPool))
                {
                    return prefabPool;
                }
            }
            var pool = new PrefabPool();
            pool.Init(poolName, prefab);
            if (!string.IsNullOrEmpty(poolName))
            {
                s_Pools.Add(poolName, pool);
            }
            return pool;
        }

        public GameObject Get(Transform parent = null)
        {
            if (_prefab == null) return null;

            GameObject go = null;
            if (_pool.Count > 0)
            {
                go = _pool[0];
                _pool.RemoveAt(0);
            }
            else
            {
                go = GameObject.Instantiate(_prefab);
            }
            go.transform.parent = parent;
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(true);
            go.transform.SetAsLastSibling();
            _useList.Add(go);
            return go;
        }

        public void Recycle(GameObject go)
        {
            if (go != null)
            {
                go.SetActive(false);
                _pool.Add(go);
                _useList.Remove(go);
            }
        }

        public void RecycleUseList()
        {
            foreach (var go in _useList)
            {
                if (go != null)
                {
                    go.SetActive(false);
                    _pool.Add(go);
                }
            }
            _useList.Clear();
        }

        public void Destroy()
        {
            foreach (var go in _pool)
            {
                if (go != null)
                {
                    GameObject.Destroy(go);
                }
            }
            _pool.Clear();
            foreach (var go in _useList)
            {
                if (go != null)
                {
                    GameObject.Destroy(go);
                }
            }
            _useList.Clear();

            ListPool<GameObject>.Release(_pool);
            ListPool<GameObject>.Release(_useList);

            s_Pools.Remove(_poolName);

            _pool = null;
            _useList = null;
            _prefab = null;
            _poolName = null;
        }
    }
}

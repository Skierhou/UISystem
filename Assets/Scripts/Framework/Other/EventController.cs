using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkierFramework
{
    public class EventController<Key>
    {
        private Dictionary<Key, List<Delegate>> m_Events = new Dictionary<Key, List<Delegate>>();

        #region AddListener
        private void OnAddListener(Key key, Delegate action)
        {
            if (!m_Events.TryGetValue(key, out List<Delegate> list))
            {
                list = new List<Delegate>();
                m_Events.Add(key, list);
            }
            if (list.Count > 0)
            {
                if (list[0].GetType() != action.GetType())
                {
                    Debug.LogError(string.Format("当前EvnetID：{0}, {1}和{2}参数不一致！", key.ToString(), action.GetType().ToString(), list[0].GetType().ToString()));
                }
                if (list.Contains(action))
                {
                    Debug.LogError(string.Format("当前EventID：{0}, 该{1}重复注册！", key.ToString(), action.GetType().ToString()));
                }
            }
            list.Add(action);
        }
        public void AddListener(Key key, Delegate action)
        {
            OnAddListener(key, action);
        }
        public void AddListener<T1, T2, T3, T4, T5, T6>(Key key, Action<T1, T2, T3, T4, T5, T6> action)
        {
            OnAddListener(key, action);
        }
        public void AddListener<T1, T2, T3, T4, T5>(Key key, Action<T1, T2, T3, T4, T5> action)
        {
            OnAddListener(key, action);
        }
        public void AddListener<T1, T2, T3, T4>(Key key, Action<T1, T2, T3, T4> action)
        {
            OnAddListener(key, action);
        }
        public void AddListener<T1, T2, T3>(Key key, Action<T1, T2, T3> action)
        {
            OnAddListener(key, action);
        }
        public void AddListener<T1, T2>(Key key, Action<T1, T2> action)
        {
            OnAddListener(key, action);
        }
        public void AddListener<T1>(Key key, Action<T1> action)
        {
            OnAddListener(key, action);
        }
        public void AddListener(Key key, Action action)
        {
            OnAddListener(key, action);
        }
        #endregion

        #region RemoveListener
        private void OnRemoveListener(Key key, Delegate action)
        {
            if (m_Events.TryGetValue(key, out List<Delegate> list))
            {
                list.Remove(action);
            }
        }
        public void RemoveListener(Key key, Delegate action)
        {
            OnRemoveListener(key, action);
        }
        public void RemoveListener<T1, T2, T3, T4, T5, T6>(Key key, Action<T1, T2, T3, T4, T5, T6> action)
        {
            OnRemoveListener(key, action);
        }
        public void RemoveListener<T1, T2, T3, T4, T5>(Key key, Action<T1, T2, T3, T4, T5> action)
        {
            OnRemoveListener(key, action);
        }
        public void RemoveListener<T1, T2, T3, T4>(Key key, Action<T1, T2, T3, T4> action)
        {
            OnRemoveListener(key, action);
        }
        public void RemoveListener<T1, T2, T3>(Key key, Action<T1, T2, T3> action)
        {
            OnRemoveListener(key, action);
        }
        public void RemoveListener<T1, T2>(Key key, Action<T1, T2> action)
        {
            OnRemoveListener(key, action);
        }
        public void RemoveListener<T1>(Key key, Action<T1> action)
        {
            OnRemoveListener(key, action);
        }
        public void RemoveListener(Key key, Action action)
        {
            OnRemoveListener(key, action);
        }
        #endregion

        #region TriggerEvent

        public void TriggerEvent<T1, T2, T3, T4, T5, T6>(Key key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)
        {
            if (m_Events.TryGetValue(key, out List<Delegate> list))
            {
                foreach (var @delegate in list)
                {
                    Action<T1, T2, T3, T4, T5, T6> action = @delegate as Action<T1, T2, T3, T4, T5, T6>;
                    if (@delegate != null && action == null)
                    {
                        Debug.LogError("Event参数异常：" + key.ToString());
                    }
                    action?.Invoke(param1, param2, param3, param4, param5, param6);
                }
            }
        }
        public void TriggerEvent<T1, T2, T3, T4, T5>(Key key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            if (m_Events.TryGetValue(key, out List<Delegate> list))
            {
                foreach (var @delegate in list)
                {
                    System.Action<T1, T2, T3, T4, T5> action = @delegate as Action<T1, T2, T3, T4, T5>;
                    if (@delegate != null && action == null)
                    {
                        Debug.LogError("Event参数异常：" + key.ToString());
                    }
                    action?.Invoke(param1, param2, param3, param4, param5);
                }
            }
        }
        public void TriggerEvent<T1, T2, T3, T4>(Key key, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            if (m_Events.TryGetValue(key, out List<Delegate> list))
            {
                foreach (var @delegate in list)
                {
                    Action<T1, T2, T3, T4> action = @delegate as Action<T1, T2, T3, T4>;
                    if (@delegate != null && action == null)
                    {
                        Debug.LogError("Event参数异常：" + key.ToString());
                    }
                    action?.Invoke(param1, param2, param3, param4);
                }
            }
        }
        public void TriggerEvent<T1, T2, T3>(Key key, T1 param1, T2 param2, T3 param3)
        {
            if (m_Events.TryGetValue(key, out List<Delegate> list))
            {
                foreach (var @delegate in list)
                {
                    Action<T1, T2, T3> action = @delegate as Action<T1, T2, T3>;
                    if (@delegate != null && action == null)
                    {
                        Debug.LogError("Event参数异常：" + key.ToString());
                    }
                    action?.Invoke(param1, param2, param3);
                }
            }
        }
        public void TriggerEvent<T1, T2>(Key key, T1 param1, T2 param2)
        {
            if (m_Events.TryGetValue(key, out List<Delegate> list))
            {
                foreach (var @delegate in list)
                {
                    Action<T1, T2> action = @delegate as Action<T1, T2>;
                    if (@delegate != null && action == null)
                    {
                        Debug.LogError("Event参数异常：" + key.ToString());
                    }
                    action?.Invoke(param1, param2);
                }
            }
        }
        public void TriggerEvent<T1>(Key key, T1 param1)
        {
            if (m_Events.TryGetValue(key, out List<Delegate> list))
            {
                foreach (var @delegate in list)
                {
                    Action<T1> action = @delegate as Action<T1>;
                    if (@delegate != null && action == null)
                    {
                        Debug.LogError("Event参数异常：" + key.ToString());
                    }
                    action?.Invoke(param1);
                }
            }
        }
        public void TriggerEvent(Key key)
        {
            if (m_Events.TryGetValue(key, out List<Delegate> list))
            {
                foreach (var @delegate in list)
                {
                    Action action = @delegate as Action;
                    if (@delegate != null && action == null)
                    {
                        Debug.LogError("Event参数异常：" + key.ToString());
                    }
                    action?.Invoke();
                }
            }
        }
        #endregion

        /// <summary>
        /// 清空
        /// </summary>
        public void Cleanup()
        {
            foreach (var list in m_Events.Values)
            {
                list.Clear();
            }
            m_Events.Clear();
        }
    }
}

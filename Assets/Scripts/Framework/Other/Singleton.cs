using UnityEngine;

public abstract class Singleton<T> where T : new()
{
    private static T _ServiceContext;
    private readonly static object lockObj = new object();

    /// <summary>
    /// 禁止外部进行实例化
    /// </summary>
    protected Singleton()
    {
        OnInitialize();
    }

    /// <summary>
    /// 获取唯一实例，双锁定防止多线程并发时重复创建实例
    /// </summary>
    /// <returns></returns>
    public static T Instance
    {
        get
        {
            if (_ServiceContext == null)
            {
                lock (lockObj)
                {
                    if (_ServiceContext == null)
                    {
                        _ServiceContext = new T();
                    }
                }
            }
            return _ServiceContext;
        }
    }
    public virtual void OnInitialize() { }

    public void Init() { }
}


public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = (T)FindObjectOfType(typeof(T));
                if (_instance == null)
                {
                    GameObject singleton = new GameObject();
                    _instance = singleton.AddComponent<T>();
                    singleton.name = typeof(T).ToString();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    public virtual void OnDestroy()
    {
        _instance = null;
    }
}


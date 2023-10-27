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
        OnInitilize();
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
    public virtual void OnInitilize() { }

    public void Init() { }
}

public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    private static object _lock = new object();

    public static T Instance
    {
        get
        {
            //单例在结束运行会自动回收,如果外界在Destory再回收一次会新建一个单例回收不了
            if (applicationIsQuitting)
            {
                Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                                 "' already destroyed on application quit." +
                                 " Won't create again - returning null.");
                return null;
            }
            //线程中的互斥锁,确保当执行该代码快时，该代码块会执行完成		
            lock (_lock)	//好比多个单例需要初始化化,当一个单例执行到该语句，其他单例就会先暂停等待其执行完成
            {
                if (_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));

                    if (FindObjectsOfType(typeof(T)).Length > 1)
                    {
                        Debug.LogError("[Singleton] Something went really wrong " +
                                       " - there should never be more than 1 singleton!" +
                                       " Reopenning the scene might fix it.");
                        return _instance;
                    }

                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();


                        _instance = singleton.AddComponent<T>();
                        singleton.name = "(singleton) " + typeof(T).ToString();

                        DontDestroyOnLoad(singleton);

                        Debug.Log("[Singleton] An instance of " + typeof(T) +
                                  " is needed in the scene, so '" + singleton +
                                  "' was created with DontDestroyOnLoad.");
                    }
                    else
                    {
                        Debug.Log("[Singleton] Using instance already created: " +
                                  _instance.gameObject.name);
                    }
                }

                return _instance;
            }
        }
    }
    private static bool applicationIsQuitting = false;

    public virtual void OnDestroy()
    {
        applicationIsQuitting = true;
    }
}


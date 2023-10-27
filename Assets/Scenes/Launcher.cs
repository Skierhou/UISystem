using SkierFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    public GameObject Splash;

    void Start()
    {
        if (Splash == null)
        {
            Splash = GameObject.Find(nameof(Splash));
        }

        StartCoroutine(StartCor());
    }

    IEnumerator StartCor()
    {
        yield return StartCoroutine(ResourceManager.Instance.InitializeAsync());

        yield return UIManager.Instance.InitUIConfig();

        yield return UIManager.Instance.Preload(UIType.UILoadingView);

        Loading.Instance.StartLoading(EnterGameCor);

        if (Splash != null)
        {
            Splash.SetActive(false);
        }
    }

    IEnumerator EnterGameCor(Action<float, string> loadingRefresh)
    {
        loadingRefresh?.Invoke(0.3f, "loading..........1");
        yield return new WaitForSeconds(0.5f);

        loadingRefresh?.Invoke(0.6f, "loading..........2");
        yield return new WaitForSeconds(0.5f);

        loadingRefresh?.Invoke(1, "loading..........3");
        yield return new WaitForSeconds(0.5f);

        UIManager.Instance.Open(UIType.UILoginView);
    }
}

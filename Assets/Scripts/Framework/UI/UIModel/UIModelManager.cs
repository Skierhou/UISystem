using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkierFramework
{
    public class UIModelManager : Singleton<UIModelManager>
    {
        private Stack<Camera> m_CameraPool = new Stack<Camera>();
        private Stack<int> m_IndexPool = new Stack<int>();
        private int m_PoolCount = 0;
        private Camera m_UICamera;

        private Transform m_UIModelRoot;
        private Light m_UIModelLight;

        public Camera UICamera => m_UICamera;

        public override void OnInitialize()
        {
            base.OnInitialize();

            //m_UICamera = GameObject.Find("UICamera").GetOrAddComponent<Camera>();

            m_UIModelRoot = new GameObject("UIModelRoot").transform;
            m_UIModelRoot.SetParentEx(null);
            GameObject.DontDestroyOnLoad(m_UIModelRoot);

            m_UIModelLight = new GameObject("UIModelLight").GetOrAddComponent<Light>();
            m_UIModelLight.transform.SetParentEx(m_UIModelRoot);

            m_UIModelLight.transform.localEulerAngles = new Vector3(36, 30, 0);
            m_UIModelLight.cookieSize = 10;
            m_UIModelLight.type = LightType.Directional;
            m_UIModelLight.cullingMask = 1 << Layer.UI | 1 << Layer.UIRenderToTarget;
            m_UIModelLight.shadows = LightShadows.None;

            m_UIModelLight.gameObject.SetActive(false);
        }

        /// <summary>
        /// 加载一个模型到一张RawImage上
        /// </summary>
        public void LoadModelToRawImage(string path, RawImage rawImage, bool canDrag = true, Vector3 offset = default,
            Quaternion rot = default, Vector3 scale = default, bool isOrth = true, float orthSizeOrFOV = 1, Action<UIRenderToTexture, GameObject> callback = null)
        {
            if (rawImage == null)
            {
                Debug.LogError("RawImage Is Null!");
                return;
            }

            rawImage.enabled = false;
            ResourceManager.Instance.InstantiateAsync(path, (go) =>
            {
                UnLoadModelByRawImage(rawImage);
                rawImage.enabled = true;
                LoadModelToRawImage(go, rawImage, canDrag, offset, rot, scale, isOrth, orthSizeOrFOV, callback);
            });
        }

        public void LoadModelToRawImage(GameObject go, RawImage rawImage, bool canDrag, Vector3 offset = default,
            Quaternion rot = default, Vector3 scale = default, bool isOrth = true, float orthSizeOrFOV = 1, Action<UIRenderToTexture, GameObject> callback = null)
        {
            if (go != null)
            {
                UIRenderToTexture renderToTexture = rawImage.GetOrAddComponent<UIRenderToTexture>();
                Vector3 pos;
                int index = 0;
                if (renderToTexture.Targets == null || renderToTexture.Targets.Count == 0)
                {
                    index = m_IndexPool.Count > 0 ? m_IndexPool.Pop() : m_PoolCount++;
                }
                else
                {
                    index = renderToTexture.Index;
                }

                pos = new Vector3(100 * index, -10000, 0); 
                renderToTexture.Init(pos, isOrth, orthSizeOrFOV, index);
                UpdateLight();

                go.SetLayerRecursively(Layer.UIRenderToTarget);
                go.transform.SetParent(m_UIModelRoot);
                go.transform.localPosition = pos + offset;
                go.transform.localScale = scale == default ? Vector3.one : scale;
                go.transform.rotation = rot;
                renderToTexture.AddTarget(go, canDrag);
                callback?.Invoke(renderToTexture, go);
            }
        }

        /// <summary>
        /// 卸载单个
        /// </summary>
        public void UnLoadModelByRawImage(RawImage rawImage, GameObject go)
        {
            if (rawImage != null && go != null)
            {
                UIRenderToTexture renderToTexture = rawImage.GetComponent<UIRenderToTexture>();
                int id = go.GetInstanceID();
                if (renderToTexture != null && renderToTexture.Targets != null && renderToTexture.Targets.ContainsKey(id))
                {
                    renderToTexture.Targets.Remove(id);
                    ResourceManager.Instance.Recycle(go);
                    if (renderToTexture.Targets.Count == 0)
                    {
                        m_IndexPool.Push(renderToTexture.Index);
                        renderToTexture.ResetTarget();
                    }
                    UpdateLight();
                }
            }
        }

        /// <summary>
        /// 卸载所有
        /// </summary>
        public void UnLoadModelByRawImage(RawImage rawImage, bool recycleGo = true)
        {
            if (rawImage != null)
            {
                UIRenderToTexture renderToTexture = rawImage.GetComponent<UIRenderToTexture>();
                if (renderToTexture != null && renderToTexture.Targets != null && renderToTexture.Targets.Count > 0)
                {
                    if (recycleGo)
                    {
                        foreach (var target in renderToTexture.Targets.Values)
                        {
                            ResourceManager.Instance.Recycle(target.gameObject);
                        }
                    }
                    m_IndexPool.Push(renderToTexture.Index);
                    renderToTexture.ResetTarget();
                    UpdateLight();
                }
            }
        }

        private void UpdateLight()
        {
            m_UIModelLight.gameObject.SetActive(m_CameraPool.Count < m_PoolCount);
        }

        public Camera SpawnCamera()
        {
            if (m_CameraPool.Count > 0)
            {
                Camera camera = m_CameraPool.Pop();
                camera.gameObject.SetActive(true);
                return camera;
            }
            else
            {
                GameObject go = new GameObject("CameraRTT");
                go.transform.SetParentEx(m_UIModelRoot);
                Camera camera = go.GetOrAddComponent<Camera>();
                camera.fieldOfView = 30;
                camera.allowHDR = false;
                camera.backgroundColor = Color.clear;
                camera.useOcclusionCulling = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.cullingMask = 1 << Layer.UIRenderToTarget;
                camera.farClipPlane = 30;
                camera.orthographicSize = 1;

                return camera;
            }
        }

        public void RecyleCamera(Camera camera)
        {
            if (camera != null)
            {
                camera.targetTexture = null;
                camera.gameObject.SetActive(false);
                m_CameraPool.Push(camera);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace SkierFramework
{

    public class UIRenderToTexture : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public struct Target
        {
            public GameObject gameObject;
            public bool canDrag;
        }

        private Camera m_Camera;
        private RawImage m_RawImage;
        private RenderTexture m_RenderTexture;
        private Dictionary<int, Target> m_Targets = new Dictionary<int, Target>();
        private int m_Index = -1;
        private Transform m_DragTarget;
        private float m_orthographicSize = 1;

        public Camera Camera => m_Camera;
        public Dictionary<int, Target> Targets => m_Targets;
        public int Index => m_Index;
        /// <summary>
        /// 目标被点击
        /// </summary>
        public event Action<GameObject> OnTargetClick;

        void OnDestroy()
        {
            if (m_Camera != null)
                UIModelManager.Instance.RecyleCamera(m_Camera);
            ReleaseRenderTexture();
        }


        public void Init(Vector3 vec, bool isOrth, float orth, int index)
        {
            InitCamera(isOrth, orth);
            m_Index = index;
            if (m_Camera)
            {
                m_Camera.transform.localPosition = vec + new Vector3(0, 0, 20);
                m_Camera.transform.localEulerAngles = new Vector3(0, 180, 0);
            }

            if (m_RawImage == null)
                m_RawImage = GetComponent<RawImage>();

            if (m_RenderTexture == null)
            {
                m_RenderTexture = RenderTexture.GetTemporary((int)m_RawImage.rectTransform.rect.width, (int)m_RawImage.rectTransform.rect.height, 1, RenderTextureFormat.ARGB32);
                m_RenderTexture.name = "UIRenderToTexture";
            }
            m_Camera.targetTexture = m_RenderTexture;
            m_RawImage.texture = m_RenderTexture;
        }

        private void InitCamera(bool isOrth, float orth)
        {
            if (m_Camera == null)
            {
                m_Camera = UIModelManager.Instance.SpawnCamera();
            }
            else
            {
                m_Camera.gameObject.SetActive(true);
            }
            m_Camera.orthographic = isOrth;
            m_Camera.orthographicSize = orth;
            m_Camera.fieldOfView = orth;
            m_orthographicSize = orth;
        }

        private void ReleaseRenderTexture()
        {
            if (m_RenderTexture != null)
            {
                RenderTexture.ReleaseTemporary(m_RenderTexture);
                m_RenderTexture = null;
                m_RawImage.texture = null;
            }
        }
        public bool AddTarget(GameObject target, bool canDrag)
        {
            if (target)
            {
                if (m_Targets == null)
                    m_Targets = new Dictionary<int, Target>();

                int id = target.GetInstanceID();
                if (!m_Targets.ContainsKey(id))
                {
                    m_Targets.Add(id, new Target { gameObject = target, canDrag = canDrag });
                    return true;
                }
            }
            return false;
        }

        public void ResetTarget()
        {
            m_Targets.Clear();
            if (m_Camera != null)
            {
                UIModelManager.Instance.RecyleCamera(m_Camera);
                m_Camera = null;
            }
            m_Index = -1;
            ReleaseRenderTexture();
        }

        private void Update()
        {
            if (m_Targets != null)
            {
                foreach (var target in m_Targets.Values)
                {
                    if (m_DragTarget == null || m_DragTarget.gameObject != target.gameObject)
                        target.gameObject.transform.rotation = Quaternion.Lerp(target.gameObject.transform.rotation, Quaternion.identity, Time.deltaTime * 5);
                }
            }
        }

        private void SetClickTarget(Vector2 clickPos)
        {
            if (m_Targets.Count == 1)
            {
                foreach (var item in m_Targets.Values)
                {
                    m_DragTarget = item.gameObject.transform;
                }
                return;
            }
            // 通过射线检测当前点击的模型
            // 拿到点击位置与UI位置的偏移百分比
            Camera uiCamera = UIModelManager.Instance.UICamera;
            float width = m_RawImage.rectTransform.rect.width;
            float height = m_RawImage.rectTransform.rect.height;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, transform.position);
            screenPos += new Vector2(width * (0.5f - m_RawImage.rectTransform.pivot.x), height * (0.5f - m_RawImage.rectTransform.pivot.y));
            Vector2 offset = clickPos - screenPos;
            offset = new Vector2(offset.x / width, offset.y / height);
            if (m_Camera.orthographic)
            {
                // 正交相机的渲染大小height=size*2， width = 宽高比*height
                float screenHeight = m_Camera.orthographicSize * 2;
                float screenWidth = m_Camera.pixelWidth * 1.0f / m_Camera.pixelHeight * screenHeight;
                Vector3 startPoint = m_Camera.transform.position + m_Camera.transform.right * offset.x * screenWidth + m_Camera.transform.up * offset.y * screenHeight;

                Debug.DrawLine(startPoint, startPoint + m_Camera.transform.forward * 100, Color.red, 5f);
                if (Physics.Raycast(startPoint, m_Camera.transform.forward, out RaycastHit hit, 100, 1 << Layer.UIRenderToTarget))
                {
                    m_DragTarget = hit.transform;
                }
            }
            else
            {
                // 透视相机通过FOV和near可以求height， width同正交相机
                float screenHeight = Mathf.Tan(m_Camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * m_Camera.nearClipPlane * 2;
                float screenWidth = m_Camera.pixelWidth * 1.0f / m_Camera.pixelHeight * screenHeight;
                Vector3 endPoint = m_Camera.transform.position + m_Camera.transform.forward * m_Camera.nearClipPlane
                    + m_Camera.transform.right * offset.x * screenWidth + m_Camera.transform.up * offset.y * screenHeight;
                Vector3 dir = (endPoint - m_Camera.transform.position).normalized;

                Debug.DrawLine(m_Camera.transform.position, endPoint, Color.red, 5f);
                if (Physics.Raycast(m_Camera.transform.position, dir, out RaycastHit hit, m_Camera.farClipPlane, 1 << Layer.UIRenderToTarget))
                {
                    m_DragTarget = hit.transform;
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            m_DragTarget = null;
            SetClickTarget(eventData.position);

            // 不能拖拽时置空
            if (m_DragTarget != null && m_Targets.TryGetValue(m_DragTarget.GetInstanceID(), out Target target) && !target.canDrag)
            {
                m_DragTarget = null;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_DragTarget != null)
                m_DragTarget.localEulerAngles -= new Vector3(0, eventData.delta.x, 0);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            m_DragTarget = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            m_DragTarget = null;
            SetClickTarget(eventData.position);

            if (m_DragTarget != null)
            {
                // 点中模型，发送事件或者其他操作
                OnTargetClick?.Invoke(m_DragTarget.gameObject);
            }
        }
    }
}

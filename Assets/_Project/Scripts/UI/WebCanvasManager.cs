using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Fotbool.UI
{
    /// <summary>
    /// </summary>
    public sealed class WebCanvasManager : MonoBehaviour
    {
        [Header("الإعدادات")]
        public Canvas mainCanvas;
        public CanvasScaler canvasScaler;
        public RectTransform safeAreaRect;

        private void Awake()
        {
            if (mainCanvas == null)
                mainCanvas = GetComponent<Canvas>();

            if (canvasScaler == null)
                canvasScaler = GetComponent<CanvasScaler>();

            SetupCanvasForWeb();
            SetupSafeArea();
        }

        private void SetupCanvasForWeb()
        {
            if (mainCanvas != null)
            {
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                mainCanvas.sortingOrder = 0;
            }

            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1080, 1920);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 1f;
            }
        }

        private void SetupSafeArea()
        {
            if (safeAreaRect == null) return;

            // منطقة آمنة للموبايل (لتجنب Notch و Home Indicator)
            if (Application.isMobilePlatform)
            {
                var safe = Screen.safeArea;
                var anchorMin = safe.position;
                var anchorMax = safe.position + safe.size;

                anchorMin.x /= Screen.width;
                anchorMin.y /= Screen.height;
                anchorMax.x /= Screen.width;
                anchorMax.y /= Screen.height;

                safeAreaRect.anchorMin = anchorMin;
                safeAreaRect.anchorMax = anchorMax;
            }
        }

        private void Start()
        {
            // التأكد من دقة العرض في المتصفح
            if (Application.isWebGL)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Debug.Log("[Fotbool] WebGL initialized - Running in browser");
            }
        }
    }
}

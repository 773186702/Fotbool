using UnityEngine;
using UnityEngine.SceneManagement;
using Fotbool.UI;
using Fotbool.Inputs;

namespace Fotbool.Core
{
    /// <summary>
    /// مهيئ اللعبة للمتصفح - يُطلق الشاشة الافتتاحية الحركية ثم الترحيب
    /// </summary>
    public sealed class WebGameInitializer : MonoBehaviour
    {
        [Header("اللوحات")]
        public GameObject kineticIntroPanel;
        public GameObject welcomePanel;
        public WebCanvasManager canvasManager;

        private void Awake()
        {
            // لا تُدمّر هذا الكائن بين المشاهد
            DontDestroyOnLoad(gameObject);

            // إضافة نظام اللمس إذا لم يكن موجوداً
            if (FindObjectOfType<WebTouchInput>() == null)
            {
                var touchGO = new GameObject("WebTouchInput");
                touchGO.AddComponent<WebTouchInput>();
            }

            // إضافة مدير Canvas إذا لم يكن موجوداً
            if (canvasManager == null && FindObjectOfType<WebCanvasManager>() == null)
            {
                var canvasGO = new GameObject("WebCanvasManager");
                canvasGO.AddComponent<WebCanvasManager>();
            }
        }

        private void Start()
        {
            if (Application.isWebGL)
            {
                Debug.Log("[Fotbool] Running in WebGL browser");
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            // بدء الشاشة الافتتاحية
            if (kineticIntroPanel != null)
                kineticIntroPanel.SetActive(true);

            if (welcomePanel != null)
                welcomePanel.SetActive(false);
        }

        public void StartGame()
        {
            // تحميل مشهد المباراة
            SceneManager.LoadScene("MainMatch");
        }
    }
}

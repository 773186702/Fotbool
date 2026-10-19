using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Fotbool.UI
{
    public sealed class GameIntroductionScreen : MonoBehaviour
    {
        [Header("اللوحات")]
        public GameObject introductionPanel;
        public GameObject featuresPanel;
        public GameObject controlsPanel;
        public GameObject aboutPanel;

        [Header("النصوص")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI featureText;
        public TextMeshProUGUI controlsText;
        public TextMeshProUGUI aboutText;
        public TextMeshProUGUI pageIndicatorText;

        [Header("الأزرار")]
        public Button btnPrev;
        public Button btnNext;
        public Button btnClose;
        public Button btnFeatures;
        public Button btnControls;
        public Button btnAbout;
        public Button btnBack;

        [Header("إعدادات التنقل")]
        public int currentPage = 0;
        public int totalPages = 4;
        public float pageTransitionDuration = 0.3f;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (btnPrev != null) btnPrev.onClick.AddListener(OnPrevClicked);
            if (btnNext != null) btnNext.onClick.AddListener(OnNextClicked);
            if (btnClose != null) btnClose.onClick.AddListener(OnCloseClicked);
            if (btnFeatures != null) btnFeatures.onClick.AddListener(OnFeaturesClicked);
            if (btnControls != null) btnControls.onClick.AddListener(OnControlsClicked);
            if (btnAbout != null) btnAbout.onClick.AddListener(OnAboutClicked);
            if (btnBack != null) btnBack.onClick.AddListener(OnBackClicked);

            HideAllPanels();
        }

        private void Start()
        {
            ShowIntroduction();
            UpdatePageIndicator();
        }

        private void HideAllPanels()
        {
            if (introductionPanel != null) introductionPanel.SetActive(false);
            if (featuresPanel != null) featuresPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (aboutPanel != null) aboutPanel.SetActive(false);
        }

        public void ShowIntroduction()
        {
            HideAllPanels();
            if (introductionPanel != null) introductionPanel.SetActive(true);
            currentPage = 0;
            UpdateContent();
            UpdatePageIndicator();
        }

        private void UpdateContent()
        {
            switch (currentPage)
            {
                case 0:
                    if (titleText != null) titleText.text = "مرحباً بك في Fotbool";
                    if (descriptionText != null) descriptionText.text = "لعبة كرة القدم المحمولة التي توفر لك تجربة واقعية وممتعة.";
                    break;
                case 1:
                    if (titleText != null) titleText.text = "أطوار المباراة";
                    if (descriptionText != null) descriptionText.text = "الشوط الأول، الاستراحة، الشوط الثاني، الوقت الإضافي، وركلات الترجيح.";
                    break;
                case 2:
                    if (titleText != null) titleText.text = "أنماط اللعب";
                    if (descriptionText != null) descriptionText.text = "المهنة، البطولات، الدوري، والكؤوس بمifferent أنواعها.";
                    break;
                case 3:
                    if (titleText != null) titleText.text = "نظام الجوائز";
                    if (descriptionText != null) descriptionText.text = "اكسب الجوائز من الفوز، البطولات، والإنجازات الفردية.";
                    break;
            }
        }

        private void UpdatePageIndicator()
        {
            if (pageIndicatorText != null)
                pageIndicatorText.text = $"الصفحة {currentPage + 1} من {totalPages}";

            if (btnPrev != null) btnPrev.interactable = currentPage > 0;
            if (btnNext != null) btnNext.interactable = currentPage < totalPages - 1;
        }

        private void OnPrevClicked()
        {
            if (currentPage > 0)
            {
                currentPage--;
                UpdateContent();
                UpdatePageIndicator();
            }
        }

        private void OnNextClicked()
        {
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                UpdateContent();
                UpdatePageIndicator();
            }
        }

        private void OnCloseClicked()
        {
            introductionPanel.SetActive(false);
            UIManager.Instance?.ShowMainMenu();
        }

        private void OnFeaturesClicked()
        {
            HideAllPanels();
            if (featuresPanel != null) featuresPanel.SetActive(true);
            if (featureText != null)
                featureText.text = "⚽ أطوار مباراة كاملة (الشوطان، الاستراحة، الوقت الإضافي، ركلات الترجيح)\n" +
                    "🏆 بطولات وكؤوس بمختلف الأنظمة\n" +
                    "💰 نظام جوائز ومكافآت\n" +
                    "📊 مسيرة مهنية مع إدارة النادي واللاعبين";
        }

        private void OnControlsClicked()
        {
            HideAllPanels();
            if (controlsPanel != null) controlsPanel.SetActive(true);
            if (controlsText != null)
                controlsText.text = "🎮 التحكم باللمس: اسحب للتحريك، انقر للتسديد\n" +
                    "🥅 زر التمرير، التسديد، والتدخل\n" +
                    "⏸️ زر الإيقاف المؤقت في أعلى الشاشة";
        }

        private void OnAboutClicked()
        {
            HideAllPanels();
            if (aboutPanel != null) aboutPanel.SetActive(true);
            if (aboutText != null)
                aboutText.text = "Fotbool - لعبة كرة القدم المحمولة\nالإصدار: 1.0.0\nالمطور: فريق Fotbool\nجميع الحقوق محفوظة © 2026";
        }

        private void OnBackClicked()
        {
            ShowIntroduction();
        }
    }
}
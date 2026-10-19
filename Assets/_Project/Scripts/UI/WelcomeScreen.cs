using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fotbool.Core;
using Fotbool.Management;

namespace Fotbool.UI
{
    public sealed class WelcomeScreen : MonoBehaviour
    {
        [Header("اللوحات")]
        public GameObject welcomePanel;
        public GameObject careerPanel;
        public GameObject settingsPanel;

        [Header("النصوص والعناوين")]
        public TextMeshProUGUI gameTitleText;
        public TextMeshProUGUI gameSubtitleText;
        public TextMeshProUGUI welcomeMessageText;

        [Header("الأزرار")]
        public Button btnNewGame;
        public Button btnContinueCareer;
        public Button btnSettings;
        public Button btnExit;

        [Header("التأثيرات البصرية")]
        public Animator titleAnimator;
        public Animator buttonsAnimator;
        public float fadeDuration = 0.5f;

        private CanvasGroup canvasGroup;
        private bool isInitialized = false;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (btnNewGame != null) btnNewGame.onClick.AddListener(OnNewGameClicked);
            if (btnContinueCareer != null) btnContinueCareer.onClick.AddListener(OnContinueCareerClicked);
            if (btnSettings != null) btnSettings.onClick.AddListener(OnSettingsClicked);
            if (btnExit != null) btnExit.onClick.AddListener(OnExitClicked);

            if (careerPanel != null) careerPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            isInitialized = true;
        }

        private void Start()
        {
            ShowWelcome();
            PlayIntroAnimation();
        }

        private void PlayIntroAnimation()
        {
            if (titleAnimator != null) titleAnimator.SetTrigger("Intro");
            if (buttonsAnimator != null) buttonsAnimator.SetTrigger("Appear");
        }

        public void ShowWelcome()
        {
            if (!isInitialized) return;
            HideAllPanels();
            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (welcomeMessageText != null)
            {
                string greeting = GetGreetingByTime();
                welcomeMessageText.text = $"{greeting}! أهلاً بك في لعبة Fotbool";
            }

            if (gameTitleText != null)
                gameTitleText.text = "Fotbool";

            if (gameSubtitleText != null)
                gameSubtitleText.text = "تجربة كرة القدم الحقيقية على هاتفك";
        }

        private string GetGreetingByTime()
        {
            int hour = System.DateTime.Now.Hour;
            if (hour < 12) return "صباح الخير";
            if (hour < 17) return "مساء الخير";
            return "مساء الخير";
        }

        private void HideAllPanels()
        {
            if (welcomePanel != null) welcomePanel.SetActive(false);
            if (careerPanel != null) careerPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void OnNewGameClicked()
        {
            PlayButtonClickEffect();
            HideAllPanels();
            if (careerPanel != null) careerPanel.SetActive(true);

            var career = FindObjectOfType<CareerManager>();
            if (career != null)
            {
                if (career.HasSavedCareer())
                    career.LoadCareer();
                else
                    career.NewCareer("ناديي", "ملعبنا", Color.white, Color.red, 0);
            }

            UIManager.Instance?.ShowMainMenu();
        }

        private void OnContinueCareerClicked()
        {
            PlayButtonClickEffect();
            var career = FindObjectOfType<CareerManager>();
            if (career != null && career.HasSavedCareer())
            {
                career.LoadCareer();
                HideAllPanels();
                UIManager.Instance?.ShowMainMenu();
            }
            else
            {
                OnNewGameClicked();
            }
        }

        private void OnSettingsClicked()
        {
            PlayButtonClickEffect();
            HideAllPanels();
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        private void OnExitClicked()
        {
            PlayButtonClickEffect();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void PlayButtonClickEffect()
        {
            var sound = FindObjectOfType<Fotbool.Audio.SoundManager>();
        }

        public IEnumerator FadeIn()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public IEnumerator FadeOut(System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            onComplete?.Invoke();
        }
    }
}
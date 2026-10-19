using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Fotbool.UI
{
    public sealed class KineticIntroScreen : MonoBehaviour
    {
        [Header("اللوحات")]
        public GameObject introPanel;
        public GameObject welcomePanel;

        [Header("عناصر النص المتحركة")]
        public TextMeshProUGUI gameTitleText;
        public TextMeshProUGUI gameSubtitleText;
        public TextMeshProUGUI loadingText;

        [Header("الصور والشعارات")]
        public Image gameLogoImage;
        public Image backgroundImage;

        [Header("إعدادات الحركة")]
        public float titleFadeInDuration = 1f;
        public float subtitleSlideDuration = 1f;
        public float logoScaleDuration = 1f;
        public float totalIntroDuration = 5f;
        public float skipAfterDuration = 3f;

        [Header("الأزرار")]
        public Button btnSkip;
        public Button btnContinue;

        private CanvasGroup canvasGroup;
        private float elapsedTime = 0f;
        private bool isSkipped = false;
        private bool isCompleted = false;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (btnSkip != null) btnSkip.onClick.AddListener(OnSkipClicked);
            if (btnContinue != null) btnContinue.onClick.AddListener(OnContinueClicked);

            if (btnSkip != null) btnSkip.gameObject.SetActive(false);
            if (btnContinue != null) btnContinue.gameObject.SetActive(false);

            if (introPanel != null) introPanel.SetActive(true);
            if (welcomePanel != null) welcomePanel.SetActive(false);
        }

        private void Start()
        {
            StartCoroutine(PlayKineticIntro());
        }

        private IEnumerator PlayKineticIntro()
        {
            if (gameTitleText != null) gameTitleText.alpha = 0f;
            if (gameSubtitleText != null) gameSubtitleText.alpha = 0f;
            if (gameLogoImage != null) gameLogoImage.transform.localScale = Vector3.zero;
            if (loadingText != null) loadingText.text = "جاري التحميل...";

            float logoTimer = 0f;
            while (logoTimer < logoScaleDuration && !isSkipped)
            {
                logoTimer += Time.unscaledDeltaTime;
                float t = logoTimer / logoScaleDuration;
                if (gameLogoImage != null)
                    gameLogoImage.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                yield return null;
            }

            float titleTimer = 0f;
            while (titleTimer < titleFadeInDuration && !isSkipped)
            {
                titleTimer += Time.unscaledDeltaTime;
                float t = titleTimer / titleFadeInDuration;
                if (gameTitleText != null) gameTitleText.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            if (gameTitleText != null)
                gameTitleText.text = "Fotbool";

            float subtitleTimer = 0f;
            Vector3 subtitleStartPos = gameSubtitleText != null ? gameSubtitleText.transform.localPosition : Vector3.zero;
            Vector3 subtitleTargetPos = subtitleStartPos + Vector3.down * 50f;

            while (subtitleTimer < subtitleSlideDuration && !isSkipped)
            {
                subtitleTimer += Time.unscaledDeltaTime;
                float t = subtitleTimer / subtitleSlideDuration;
                if (gameSubtitleText != null)
                {
                    gameSubtitleText.alpha = Mathf.Lerp(0f, 1f, t);
                    gameSubtitleText.transform.localPosition = Vector3.Lerp(subtitleStartPos, subtitleTargetPos, t);
                }
                yield return null;
            }

            if (gameSubtitleText != null)
                gameSubtitleText.text = "تجربة كرة القدم الحقيقية على هاتفك";

            yield return new WaitForSecondsRealtime(skipAfterDuration);
            if (!isSkipped && btnSkip != null)
                btnSkip.gameObject.SetActive(true);

            while (elapsedTime < totalIntroDuration && !isSkipped && !isCompleted)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            CompleteIntro();
        }

        private void CompleteIntro()
        {
            if (isCompleted) return;
            isCompleted = true;

            if (introPanel != null) introPanel.SetActive(false);
            if (welcomePanel != null) welcomePanel.SetActive(true);

            if (btnContinue != null) btnContinue.gameObject.SetActive(true);
            if (btnSkip != null) btnSkip.gameObject.SetActive(false);
        }

        private void OnSkipClicked()
        {
            isSkipped = true;
            CompleteIntro();
        }

        private void OnContinueClicked()
        {
            if (welcomePanel != null) welcomePanel.SetActive(false);
            UIManager.Instance?.ShowMainMenu();
        }

        public void RestartIntro()
        {
            isSkipped = false;
            isCompleted = false;
            elapsedTime = 0f;
            if (introPanel != null) introPanel.SetActive(true);
            if (welcomePanel != null) welcomePanel.SetActive(false);
            if (btnSkip != null) btnSkip.gameObject.SetActive(false);
            if (btnContinue != null) btnContinue.gameObject.SetActive(false);
            StartCoroutine(PlayKineticIntro());
        }
    }
}
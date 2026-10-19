using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fotbool.Core;
using Fotbool.Gameplay;

namespace Fotbool.UI
{
    /// <summary>
    /// مدير واجهة المستخدم المركزي. يديرlou الشاشات (القائمة الرئيسية، المباراة، الإيقاف، ما بعد المباراة، المهن).
    /// يتطلب TextMeshPro و Canvas في المشهد.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        public GameObject mainMenuPanel;
        public GameObject matchHUDPanel;
        public GameObject pausePanel;
        public GameObject matchResultPanel;
        public GameObject careerPanel;

        [Header("Intro & Welcome Panels")]
        public GameObject kineticIntroPanel;
        public GameObject welcomePanel;
        public GameObject gameIntroductionPanel;

        [Header("Match HUD")]
        public TextMeshProUGUI homeScoreText;
        public TextMeshProUGUI awayScoreText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI homeTeamNameText;
        public TextMeshProUGUI awayTeamNameText;
        public Image homeTeamLogo;
        public Image awayTeamLogo;
        public Slider possessionSlider;

        [Header("Match Result")]
        public TextMeshProUGUI resultHomeScoreText;
        public TextMeshProUGUI resultAwayScoreText;
        public TextMeshProUGUI resultWinnerText;
        public TextMeshProUGUI resultManOfMatchText;

        [Header("Career")]
        public TextMeshProUGUI careerClubNameText;
        public TextMeshProUGUI careerBudgetText;
        public TextMeshProUGUI careerWeekText;
        public TextMeshProUGUI careerSeasonText;

        [Header("Penalty Shootout")]
        public GameObject penaltyShootoutPanel;
        public TextMeshProUGUI penaltyHomeScoreText;
        public TextMeshProUGUI penaltyAwayScoreText;
        public TextMeshProUGUI penaltyHomeTakenText;
        public TextMeshProUGUI penaltyAwayTakenText;
        public TextMeshProUGUI penaltyCurrentKickerText;
        public TextMeshProUGUI penaltyStatusText;

        [Header("Tournament")]
        public GameObject tournamentPanel;
        public TextMeshProUGUI tournamentNameText;
        public TextMeshProUGUI tournamentRoundText;
        public TextMeshProUGUI tournamentStandingsText;

        [Header("Prize")]
        public GameObject prizePanel;
        public TextMeshProUGUI prizeText;

        private MatchManager match;
        private bool isPaused;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ShowMainMenu();
        }

        public void Initialize(MatchManager matchManager)
        {
            match = matchManager;
            ShowMatchHUD();
        }

        private void Update()
        {
            if (match == null || !matchHUDPanel.activeSelf) return;

            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();

            timerText.text = FormatTime(match.Elapsed);
            homeScoreText.text = match.HomeGoals.ToString();
            awayScoreText.text = match.AwayGoals.ToString();

            if (possessionSlider != null)
            {
                float possession = CalculatePossession();
                possessionSlider.value = possession;
            }
        }

        public void ShowMainMenu()
        {
            HideAll();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        public void ShowMatchHUD()
        {
            HideAll();
            if (matchHUDPanel != null) matchHUDPanel.SetActive(true);
            isPaused = false;
            Time.timeScale = 1f;
        }

        public void ShowPause()
        {
            HideAll();
            if (pausePanel != null) pausePanel.SetActive(true);
            isPaused = true;
            Time.timeScale = 0f;
        }

        public void ShowMatchResult(int homeGoals, int awayGoals)
        {
            HideAll();
            if (matchResultPanel != null) matchResultPanel.SetActive(true);
            if (resultHomeScoreText != null) resultHomeScoreText.text = homeGoals.ToString();
            if (resultAwayScoreText != null) resultAwayScoreText.text = awayGoals.ToString();
            string winner = homeGoals > awayGoals ? "Home Wins" : homeGoals < awayGoals ? "Away Wins" : "Draw";
            if (resultWinnerText != null) resultWinnerText.text = winner;
            Time.timeScale = 0f;
        }

        public void ShowCareer()
        {
            HideAll();
            if (careerPanel != null) careerPanel.SetActive(true);
            RefreshCareerUI();
        }

        public void HideAll()
        {
            foreach (var panel in new[] { mainMenuPanel, matchHUDPanel, pausePanel, matchResultPanel, careerPanel })
                if (panel != null) panel.SetActive(false);
        }

        public void TogglePause()
        {
            if (isPaused) ShowMatchHUD(); else ShowPause();
        }

        public void ResumeMatch() => ShowMatchHUD();

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            ShowMainMenu();
            if (match != null) match.SetPhase(MatchPhase.Finished);
        }

        private string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m:00}:{s:00}";
        }

        private float CalculatePossession()
        {
            if (match == null || match.Ball == null) return 50f;
            int homeNear = 0, awayNear = 0;
            foreach (var p in match.Players)
            {
                float d = Vector3.Distance(p.transform.position, match.Ball.transform.position);
                if (d < 10f)
                {
                    if (p.Side == TeamSide.Home) homeNear++; else awayNear++;
                }
            }
            int total = homeNear + awayNear;
            if (total == 0) return 50f;
            return (float)homeNear / total * 100f;
        }

        private void RefreshCareerUI()
        {
            if (careerClubNameText != null && match != null)
                careerClubNameText.text = "My Club";
            if (careerBudgetText != null)
                careerBudgetText.text = "$0";
            if (careerWeekText != null)
                careerWeekText.text = "Week 0";
            if (careerSeasonText != null)
                careerSeasonText.text = "Season 1";
        }

        public void ShowPenaltyShootout()
        {
            HideAll();
            if (penaltyShootoutPanel != null) penaltyShootoutPanel.SetActive(true);
            RefreshPenaltyUI();
        }

        public void HidePenaltyShootout()
        {
            if (penaltyShootoutPanel != null) penaltyShootoutPanel.SetActive(false);
        }

        public void RefreshPenaltyUI()
        {
            if (penaltyHomeScoreText != null) penaltyHomeScoreText.text = Match.HomePenaltyGoals.ToString();
            if (penaltyAwayScoreText != null) penaltyAwayScoreText.text = Match.AwayPenaltyGoals.ToString();
            if (penaltyHomeTakenText != null) penaltyHomeTakenText.text = Match.HomePenaltyGoals.ToString();
            if (penaltyAwayTakenText != null) penaltyAwayTakenText.text = Match.AwayPenaltyGoals.ToString();
            if (penaltyStatusText != null)
                penaltyStatusText.text = Match.IsHomeTurn ? "دور المضيف" : "دور الضيف";
        }

        public void ShowTournament()
        {
            HideAll();
            if (tournamentPanel != null) tournamentPanel.SetActive(true);
            RefreshTournamentUI();
        }

        public void HideTournament()
        {
            if (tournamentPanel != null) tournamentPanel.SetActive(false);
        }

        public void RefreshTournamentUI()
        {
            if (tournamentNameText != null) tournamentNameText.text = Match.TournamentName;
            if (tournamentRoundText != null) tournamentRoundText.text = Match.RoundName;
        }

        public void ShowPrize(long amount, string description)
        {
            HideAll();
            if (prizePanel != null) prizePanel.SetActive(true);
            if (prizeText != null) prizeText.text = $"🏆 جائزة: {amount:N0}\n{description}";
            Time.timeScale = 0f;
        }

        public void HidePrize()
        {
            if (prizePanel != null) prizePanel.SetActive(false);
            Time.timeScale = 1f;
        }

        #region Intro & Welcome Panels

        public void ShowKineticIntro()
        {
            HideAll();
            if (kineticIntroPanel != null) kineticIntroPanel.SetActive(true);
        }

        public void HideKineticIntro()
        {
            if (kineticIntroPanel != null) kineticIntroPanel.SetActive(false);
        }

        public void ShowWelcomeScreen()
        {
            HideAll();
            if (welcomePanel != null) welcomePanel.SetActive(true);
            var welcomeScreen = welcomePanel.GetComponent<WelcomeScreen>();
            if (welcomeScreen != null) welcomeScreen.ShowWelcome();
        }

        public void HideWelcomeScreen()
        {
            if (welcomePanel != null) welcomePanel.SetActive(false);
        }

        public void ShowGameIntroduction()
        {
            HideAll();
            if (gameIntroductionPanel != null) gameIntroductionPanel.SetActive(true);
        }

        public void HideGameIntroduction()
        {
            if (gameIntroductionPanel != null) gameIntroductionPanel.SetActive(false);
        }

        public void ShowMainMenu()
        {
            HideAll();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        public void HideMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }

        #endregion

        private void HideAll()
        {
            foreach (var panel in new[]
            {
                mainMenuPanel, matchHUDPanel, pausePanel, matchResultPanel, careerPanel,
                kineticIntroPanel, welcomePanel, gameIntroductionPanel,
                penaltyShootoutPanel, tournamentPanel, prizePanel
            })
                if (panel != null) panel.SetActive(false);
        }
    }
}
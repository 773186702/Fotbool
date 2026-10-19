using System;
using System.Collections.Generic;
using Fotbool.Core;
using Fotbool.Audio;
using UnityEngine;

namespace Fotbool.Gameplay
{
    /// <summary>
    /// مدير ركلات الترجيح (Penalty Shootout) - يُستخدم عند انتهاء المباراة بالتعادل
    /// في البطولات الإقصائية بعد الوقت الإضافي.
    /// </summary>
    [RequireComponent(typeof(MatchManager))]
    public sealed class PenaltyShootoutManager : MonoBehaviour
    {
        public MatchManager Match { get; private set; }

        [Header("الإعدادات")]
        public int MaxInitialPenalties = 5;
        public float PenaltyTimeLimit = 15f;
        public Transform PenaltySpotHome;
        public Transform PenaltySpotAway;
        public Transform GoalCenterHome;
        public Transform GoalCenterAway;

        private int homePenaltiesTaken = 0;
        private int awayPenaltiesTaken = 0;
        private int homeScore = 0;
        private int awayScore = 0;
        private bool homeTurn = true;
        private bool shootoutActive = false;
        private int currentKickerIndex = 0;
        private List<PlayerController> homeKickers = new List<PlayerController>();
        private List<PlayerController> awayKickers = new List<PlayerController>();
        private PlayerController currentKicker;
        private PlayerController currentGoalkeeper;

        public event Action<int, int> OnPenaltyScored;
        public event Action<int, int> OnPenaltyMissed;
        public event Action<PlayerController> OnKickerSelected;
        public event Action<bool> OnShootoutCompleted;

        private void Awake()
        {
            Match = GetComponent<MatchManager>();
            if (Match == null) Match = FindObjectOfType<MatchManager>();
        }

        private void OnEnable()
        {
            if (Match != null) Match.MatchFinished += OnMatchFinished;
        }

        private void OnDisable()
        {
            if (Match != null) Match.MatchFinished -= OnMatchFinished;
        }

        private void OnMatchFinished(MatchOutcome outcome)
        {
        }

        public void StartShootout()
        {
            if (shootoutActive) return;
            shootoutActive = true;
            homePenaltiesTaken = 0;
            awayPenaltiesTaken = 0;
            homeScore = 0;
            awayScore = 0;
            homeTurn = true;
            currentKickerIndex = 0;
            homeKickers.Clear();
            awayKickers.Clear();

            foreach (var p in Match.Players)
            {
                if (p.IsSentOff) continue;
                if (p.Side == TeamSide.Home) homeKickers.Add(p);
                else awayKickers.Add(p);
            }

            SoundManager.Instance?.PlayWhistleFullTime();
            SelectNextKicker();
            Debug.Log("[PenaltyShootoutManager] بدأت ركلات الترجيح");
        }

        private void SelectNextKicker()
        {
            if (!shootoutActive) return;

            if (IsShootoutOver())
            {
                CompleteShootout();
                return;
            }

            List<PlayerController> kickers = homeTurn ? homeKickers : awayKickers;
            int taken = homeTurn ? homePenaltiesTaken : awayPenaltiesTaken;

            if (taken >= kickers.Count)
            {
                if (homeTurn) homeTurn = false;
                else homeTurn = true;
                currentKickerIndex = 0;
                SelectNextKicker();
                return;
            }

            currentKicker = kickers[Mathf.Clamp(currentKickerIndex, 0, kickers.Count - 1)];
            currentGoalkeeper = SelectGoalkeeper(homeTurn ? TeamSide.Away : TeamSide.Home);

            OnKickerSelected?.Invoke(currentKicker);

            Vector3 spot = homeTurn ? (PenaltySpotHome != null ? PenaltySpotHome.position : new Vector3(0, 0.2f, 38f * Match.HomeAttackSign)) :
                                      (PenaltySpotAway != null ? PenaltySpotAway.position : new Vector3(0, 0.2f, -38f * Match.HomeAttackSign));

            currentKicker.ResetPosition(spot, Quaternion.LookRotation(-spot.normalized));
            Match.Ball.ResetBall(spot + Vector3.up * 0.22f);

            Match.SetPhase(MatchPhase.PenaltyShootout);
        }

        private PlayerController SelectGoalkeeper(TeamSide defendingSide)
        {
            foreach (var p in Match.Players)
            {
                if (p.Side == defendingSide && p.Profile != null && p.Profile.position == PlayerPosition.GK)
                    return p;
            }
            return Match.Nearest(defendingSide, Vector3.zero);
        }

        public void ExecutePenalty(Vector3 direction, float power)
        {
            if (!shootoutActive || currentKicker == null) return;

            currentKicker.Shoot(direction.normalized * power, 0f);
            Invoke(nameof(EvaluatePenaltyResult), 1.5f);
        }

        private void EvaluatePenaltyResult()
        {
            if (!shootoutActive || Match.Ball == null) return;

            Vector3 ballPos = Match.Ball.transform.position;
            bool isGoal = false;

            if (homeTurn)
            {
                if (ballPos.z < -52f && Mathf.Abs(ballPos.x) < 3.66f && ballPos.y < 2.44f)
                    isGoal = true;
            }
            else
            {
                if (ballPos.z > 52f && Mathf.Abs(ballPos.x) < 3.66f && ballPos.y < 2.44f)
                    isGoal = true;
            }

            if (isGoal)
            {
                if (homeTurn) homeScore++; else awayScore++;
                OnPenaltyScored?.Invoke(homeScore, awayScore);
                SoundManager.Instance?.PlayGoal();
                SoundManager.Instance?.PlayCrowdCheer();
            }
            else
            {
                OnPenaltyMissed?.Invoke(homeScore, awayScore);
                SoundManager.Instance?.PlayWhistleFoul();
                SoundManager.Instance?.PlayCrowdBoo();
            }

            if (homeTurn) homePenaltiesTaken++; else awayPenaltiesTaken++;
            Match.RecordPenaltyGoal(homeTurn ? TeamSide.Home : TeamSide.Away);

            homeTurn = !homeTurn;
            currentKickerIndex++;
            SelectNextKicker();
        }

        private bool IsShootoutOver()
        {
            if (homePenaltiesTaken >= MaxInitialPenalties && awayPenaltiesTaken >= MaxInitialPenalties)
                return homeScore != awayScore;
            return false;
        }

        private void CompleteShootout()
        {
            shootoutActive = false;
            bool homeWon = homeScore > awayScore;
            Match.HomePenaltyGoals = homeScore;
            Match.AwayPenaltyGoals = awayScore;
            OnShootoutCompleted?.Invoke(homeWon);

            var outcome = new MatchOutcome
            {
                HomeGoals = Match.HomeGoals,
                AwayGoals = Match.AwayGoals,
                HomePenaltyScore = homeScore,
                AwayPenaltyScore = awayScore,
                WentToPenalties = true,
                Competition = Match.Competition,
                TournamentName = Match.TournamentName,
                RoundName = Match.RoundName
            };

            Match.MatchFinished?.Invoke(outcome);
            SoundManager.Instance?.PlayWhistleFullTime();
            Debug.Log($"[PenaltyShootoutManager] انتهت ركلات الترجيح: {homeScore}-{awayScore} الفائز: {(homeWon ? "المضيف" : "الضيف")}");
        }

        public bool IsActive => shootoutActive;
        public int HomeScore => homeScore;
        public int AwayScore => awayScore;
        public int HomeTaken => homePenaltiesTaken;
        public int AwayTaken => awayPenaltiesTaken;
        public bool IsHomeTurn => homeTurn;
        public PlayerController CurrentKicker => currentKicker;
    }
}
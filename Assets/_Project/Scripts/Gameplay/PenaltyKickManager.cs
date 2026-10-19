using System;
using Fotbool.Core;
using UnityEngine;

namespace Fotbool.Gameplay
{
    /// <summary>
    /// مدير ركلة الجزاء أثناء المباراة (Penalty Kick) - يُحتسب عندما يحدث خطأ داخل منطقة الجزاء.
    /// اللاعب ينفذ ركلة جزاء واحدة ضد حارس المرمى.
    /// </summary>
    [RequireComponent(typeof(MatchManager))]
    public sealed class PenaltyKickManager : MonoBehaviour
    {
        public MatchManager Match { get; private set; }

        [Header("إعدادات ركلة الجزاء")]
        public Transform PenaltySpot;
        public float KickTimeLimit = 10f;

        private bool penaltyActive = false;
        private PlayerController kicker;
        private PlayerController goalkeeper;
        private float kickTimer;
        private Action<bool> onPenaltyCompleted;

        public event Action<PlayerController> OnPenaltyStarted;
        public event Action<bool> OnPenaltyCompleted; // true = goal

        private void Awake()
        {
            Match = GetComponent<MatchManager>();
            if (Match == null) Match = FindObjectOfType<MatchManager>();
        }

        private void Update()
        {
            if (!penaltyActive) return;

            kickTimer -= Time.deltaTime;
            if (kickTimer <= 0f)
            {
                CompletePenalty(false);
            }
        }

        /// <summary>بدء ركلة جزاء لللاعب المحدد</summary>
        public void StartPenalty(PlayerController penaltyKicker, Action<bool> callback = null)
        {
            if (penaltyActive) return;
            penaltyActive = true;
            kicker = penaltyKicker;
            kickTimer = KickTimeLimit;
            onPenaltyCompleted = callback;

            // اختيار حارس المرمى المنافس
            TeamSide defendingSide = kicker.Side == TeamSide.Home ? TeamSide.Away : TeamSide.Home;
            foreach (var p in Match.Players)
            {
                if (p.Side == defendingSide && p.Profile != null && p.Profile.position == PlayerPosition.GK)
                {
                    goalkeeper = p;
                    break;
                }
            }
            if (goalkeeper == null) goalkeeper = Match.Nearest(defendingSide, Vector3.zero);

            // وضع الكرة واللاعب على نقطة الجزاء
            Vector3 spot = PenaltySpot != null ? PenaltySpot.position : new Vector3(0, 0.2f, 18f * kicker.AttackSign);
            kicker.ResetPosition(spot + new Vector3(0, 0, -2f * kicker.AttackSign), Quaternion.LookRotation(Vector3.forward * kicker.AttackSign));
            Match.Ball.ResetBall(spot + Vector3.up * 0.22f);

            Match.SetPhase(MatchPhase.Kickoff); // طور مؤقت لركلة الجزاء
            OnPenaltyStarted?.Invoke(kicker);
            Debug.Log($"[PenaltyKickManager] بدأت ركلة جزاء لـ {kicker.Profile.fullName}");
        }

        /// <summary>تنفيذ ركلة الجزاء (يُستدعى من واجهة المستخدم أو AI)</summary>
        public void ExecutePenalty(Vector3 direction, float power)
        {
            if (!penaltyActive || kicker == null) return;

            kicker.Shoot(direction.normalized * power, 0f);
            Invoke(nameof(CompletePenalty), 1.5f);
        }

        private void CompletePenalty(bool isGoal = false)
        {
            if (!penaltyActive) return;
            penaltyActive = false;

            if (isGoal)
            {
                TeamSide scorer = kicker.Side;
                if (scorer == TeamSide.Home) Match.HomeGoals++; else Match.AwayGoals++;
                Match.GoalScored?.Invoke(scorer);
                SoundManager.Instance?.PlayGoal();
                SoundManager.Instance?.PlayCrowdCheer();
            }
            else
            {
                SoundManager.Instance?.PlayWhistleFoul();
            }

            Match.ResetKickoff();
            onPenaltyCompleted?.Invoke(isGoal);
            OnPenaltyCompleted?.Invoke(isGoal);
            Debug.Log($"[PenaltyKickManager] انتهت ركلة الجزاء: {(isGoal ? "هدف" : "إهدار")}");
        }

        public bool IsActive => penaltyActive;
        public PlayerController Kicker => kicker;
        public PlayerController Goalkeeper => goalkeeper;
    }
}
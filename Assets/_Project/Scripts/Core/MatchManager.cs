using System;
using System.Collections.Generic;
using Fotbool.Audio;
using Fotbool.Data;
using Fotbool.Gameplay;
using Fotbool.Management;
using UnityEngine;

namespace Fotbool.Core
{
    /// <summary>جميع أطوار المباراة الرسمية حسب قوانين كرة القدم.</summary>
    public enum MatchPhase
    {
        Kickoff,
        FirstHalf,
        Halftime,
        SecondHalf,
        ExtraTimeFirstHalf,
        ExtraTimeHalftime,
        ExtraTimeSecondHalf,
        PenaltyShootout,
        Finished,
        Paused,
        Replay
    }

    /// <summary>نوع المنافسة</summary>
    public enum CompetitionType { League, Cup, ChampionsLeague, Friendly }

    /// <summary>نتيجة المباراة المفصلة</summary>
    [Serializable]
    public struct MatchOutcome
    {
        public int HomeGoals;
        public int AwayGoals;
        public bool HomeWon => HomeGoals > AwayGoals;
        public bool AwayWon => AwayGoals > HomeGoals;
        public bool IsDraw => HomeGoals == AwayGoals;
        public bool WentToExtraTime;
        public bool WentToPenalties;
        public int HomePenaltyScore;
        public int AwayPenaltyScore;
        public CompetitionType Competition;
        public string TournamentName;
        public string RoundName;
        public long PrizeMoney;
    }

    public sealed class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        public readonly List<PlayerController> Players = new List<PlayerController>(22);
        public BallPhysics Ball { get; private set; }

        [Header("إعدادات المنافسة")]
        public CompetitionType Competition = CompetitionType.Friendly;
        public string TournamentName = "Fotbool Cup";
        public string RoundName = "مباراة ودية";

        [Header("مدة المباراة (ثواني)")]
        public float FirstHalfDuration = 60f;
        public float SecondHalfDuration = 60f;
        public float HalftimeDuration = 10f;
        public float ExtraTimeDuration = 30f;
        public float ExtraTimeHalftimeDuration = 5f;
        public float PenaltyShootoutTimeLimit = 30f;

        public MatchPhase Phase { get; private set; } = MatchPhase.Kickoff;
        public int HomeGoals { get; private set; }
        public int AwayGoals { get; private set; }
        public int HomePenaltyGoals { get; private set; }
        public int AwayPenaltyGoals { get; private set; }
        public float Elapsed { get; private set; }
        public float PhaseDuration { get; private set; }
        public float Progress => PhaseDuration > 0 ? Mathf.Clamp01(Elapsed / PhaseDuration) : 0f;

        public int HomeAttackSign { get; private set; } = 1;
        public bool IsSecondHalf => Phase == MatchPhase.SecondHalf;
        public bool IsExtraTime => Phase == MatchPhase.ExtraTimeFirstHalf || Phase == MatchPhase.ExtraTimeSecondHalf;
        public bool IsPenaltyShootout => Phase == MatchPhase.PenaltyShootout;
        public bool IsHalftime => Phase == MatchPhase.Halftime || Phase == MatchPhase.ExtraTimeHalftime;

        public event Action<TeamSide> GoalScored;
        public event Action<MatchPhase> PhaseChanged;
        public event Action<MatchOutcome> MatchFinished;
        public event Action<int, int> PenaltyScored;

        private Vector3[] startPositions;
        private Quaternion[] startRotations;
        private bool secondHalf;
        private bool extraTimeFirstHalf;
        private float oldTimeScale = 1f;
        private float phaseTimer;
        private MatchOutcome currentOutcome;

        public void Initialize(BallPhysics ball)
        {
            Ball = ball;
            startPositions = new Vector3[Players.Count];
            startRotations = new Quaternion[Players.Count];
            for (int i = 0; i < Players.Count; i++)
            {
                startPositions[i] = Players[i].transform.position;
                startRotations[i] = Players[i].transform.rotation;
            }
            StartMatch();
        }

        private void StartMatch()
        {
            SetPhase(MatchPhase.Kickoff);
            HomeGoals = 0;
            AwayGoals = 0;
            HomePenaltyGoals = 0;
            AwayPenaltyGoals = 0;
            Elapsed = 0f;
            secondHalf = false;
            extraTimeFirstHalf = false;
            HomeAttackSign = 1;
            currentOutcome = new MatchOutcome
            {
                Competition = Competition,
                TournamentName = TournamentName,
                RoundName = RoundName
            };
            SoundManager.Instance?.PlayWhistleStart();
        }

        private void Update()
        {
            if (Ball == null) return;

            switch (Phase)
            {
                case MatchPhase.FirstHalf:
                case MatchPhase.SecondHalf:
                case MatchPhase.ExtraTimeFirstHalf:
                case MatchPhase.ExtraTimeSecondHalf:
                    UpdatePlayingPhase();
                    break;

                case MatchPhase.Halftime:
                case MatchPhase.ExtraTimeHalftime:
                    UpdateBreakPhase();
                    break;

                case MatchPhase.PenaltyShootout:
                    break;
            }
        }

        private void UpdatePlayingPhase()
        {
            Elapsed += Time.deltaTime;
            phaseTimer += Time.deltaTime;

            if (phaseTimer >= PhaseDuration)
            {
                AdvanceToNextPhase();
                return;
            }

            Vector3 p = Ball.transform.position;
            if (Mathf.Abs(p.z) > 52.61f)
            {
                if (Mathf.Abs(p.x) < 3.55f && p.y < 2.33f)
                {
                    TeamSide scorer = Mathf.Sign(p.z) == HomeAttackSign ? TeamSide.Home : TeamSide.Away;
                    if (scorer == TeamSide.Home) HomeGoals++; else AwayGoals++;
                    GoalScored?.Invoke(scorer);
                    SoundManager.Instance?.PlayGoal();
                    SoundManager.Instance?.PlayCrowdCheer();
                }
                ResetKickoff();
            }
            else if (Mathf.Abs(p.x) > 34.11f)
            {
                ResetKickoff();
            }
        }

        private void UpdateBreakPhase()
        {
            phaseTimer += Time.deltaTime;
            if (phaseTimer >= PhaseDuration)
            {
                AdvanceToNextPhase();
            }
        }

        private void AdvanceToNextPhase()
        {
            switch (Phase)
            {
                case MatchPhase.Kickoff:
                    SetPhase(MatchPhase.FirstHalf);
                    break;

                case MatchPhase.FirstHalf:
                    SetPhase(MatchPhase.Halftime);
                    SoundManager.Instance?.PlayWhistleHalfTime();
                    break;

                case MatchPhase.Halftime:
                    secondHalf = true;
                    HomeAttackSign = -1;
                    foreach (var player in Players) player.AttackSign *= -1;
                    SetPhase(MatchPhase.SecondHalf);
                    ResetKickoff();
                    break;

                case MatchPhase.SecondHalf:
                    if (Competition != CompetitionType.Friendly && HomeGoals == AwayGoals)
                    {
                        extraTimeFirstHalf = true;
                        SetPhase(MatchPhase.ExtraTimeFirstHalf);
                    }
                    else
                    {
                        FinishMatch();
                    }
                    break;

                case MatchPhase.ExtraTimeFirstHalf:
                    SetPhase(MatchPhase.ExtraTimeHalftime);
                    SoundManager.Instance?.PlayWhistleHalfTime();
                    break;

                case MatchPhase.ExtraTimeHalftime:
                    HomeAttackSign = -HomeAttackSign;
                    foreach (var player in Players) player.AttackSign *= -1;
                    SetPhase(MatchPhase.ExtraTimeSecondHalf);
                    ResetKickoff();
                    break;

                case MatchPhase.ExtraTimeSecondHalf:
                    if (Competition != CompetitionType.Friendly && HomeGoals == AwayGoals)
                    {
                        SetPhase(MatchPhase.PenaltyShootout);
                        SoundManager.Instance?.PlayWhistleFullTime();
                    }
                    else
                    {
                        FinishMatch();
                    }
                    break;

                case MatchPhase.PenaltyShootout:
                    FinishMatch();
                    break;
            }
        }

        public PlayerController Nearest(TeamSide side, Vector3 point, PlayerController except = null)
        {
            PlayerController best = null;
            float distance = float.PositiveInfinity;
            foreach (var player in Players)
            {
                if (player == except || player.Side != side) continue;
                float sqr = (player.transform.position - point).sqrMagnitude;
                if (sqr < distance) { distance = sqr; best = player; }
            }
            return best;
        }

        public PlayerController PassTarget(PlayerController sender)
        {
            PlayerController best = null;
            float bestScore = float.NegativeInfinity;
            foreach (var candidate in Players)
            {
                if (candidate == sender || candidate.Side != sender.Side) continue;
                Vector3 delta = candidate.transform.position - sender.transform.position;
                float distance = delta.magnitude;
                if (distance < 3f || distance > 35f) continue;
                float score = delta.z * sender.AttackSign - distance * 0.3f;
                foreach (var opponent in Players)
                {
                    if (opponent.Side == sender.Side) continue;
                    float t = Mathf.Clamp01(Vector3.Dot(opponent.transform.position - sender.transform.position, delta) / delta.sqrMagnitude);
                    if (Vector3.Distance(opponent.transform.position, sender.transform.position + delta * t) < 2f) score -= 20f;
                }
                if (score > bestScore) { bestScore = score; best = candidate; }
            }
            return best;
        }

        public void ResetKickoff()
        {
            Ball.ResetBall(new Vector3(0, 0.2f, 0));
            for (int i = 0; i < Players.Count; i++)
            {
                Vector3 position = startPositions[i];
                Quaternion rotation = startRotations[i];
                if (secondHalf) { position.x *= -1; position.z *= -1; rotation = Quaternion.Euler(0, 180, 0) * rotation; }
                Players[i].ResetPosition(position, rotation);
            }
        }

        public void SetPhase(MatchPhase phase)
        {
            if (Phase == phase) return;
            if (Phase == MatchPhase.FirstHalf || Phase == MatchPhase.SecondHalf ||
                Phase == MatchPhase.ExtraTimeFirstHalf || Phase == MatchPhase.ExtraTimeSecondHalf)
                oldTimeScale = Time.timeScale;

            Phase = phase;
            phaseTimer = 0f;
            Elapsed = 0f;

            PhaseDuration = phase switch
            {
                MatchPhase.FirstHalf => FirstHalfDuration,
                MatchPhase.SecondHalf => SecondHalfDuration,
                MatchPhase.Halftime => HalftimeDuration,
                MatchPhase.ExtraTimeFirstHalf => ExtraTimeDuration,
                MatchPhase.ExtraTimeSecondHalf => ExtraTimeDuration,
                MatchPhase.ExtraTimeHalftime => ExtraTimeHalftimeDuration,
                MatchPhase.PenaltyShootout => PenaltyShootoutTimeLimit,
                _ => 0f
            };

            Time.timeScale = phase == MatchPhase.FirstHalf || phase == MatchPhase.SecondHalf ||
                             phase == MatchPhase.ExtraTimeFirstHalf || phase == MatchPhase.ExtraTimeSecondHalf
                ? oldTimeScale : 0f;

            foreach (var player in Players) player.SetMove(Vector3.zero, false);
            PhaseChanged?.Invoke(phase);
        }

        public void StartKickoff()
        {
            SetPhase(MatchPhase.FirstHalf);
            ResetKickoff();
        }

        public void ResetKickoff()
        {
            Ball.ResetBall(new Vector3(0, 0.2f, 0));
            for (int i = 0; i < Players.Count; i++)
            {
                Vector3 position = startPositions[i];
                Quaternion rotation = startRotations[i];
                if (secondHalf || extraTimeFirstHalf)
                {
                    position.x *= -1;
                    position.z *= -1;
                    rotation = Quaternion.Euler(0, 180, 0) * rotation;
                }
                Players[i].ResetPosition(position, rotation);
            }
        }

        public void RecordPenaltyGoal(TeamSide side)
        {
            if (side == TeamSide.Home) HomePenaltyGoals++; else AwayPenaltyGoals++;
            PenaltyScored?.Invoke(HomePenaltyGoals, AwayPenaltyGoals);
            SoundManager.Instance?.PlayGoal();
        }

        private void FinishMatch()
        {
            SetPhase(MatchPhase.Finished);
            currentOutcome.HomeGoals = HomeGoals;
            currentOutcome.AwayGoals = AwayGoals;
            currentOutcome.HomePenaltyScore = HomePenaltyGoals;
            currentOutcome.AwayPenaltyScore = AwayPenaltyGoals;
            currentOutcome.WentToExtraTime = IsExtraTime || Phase == MatchPhase.PenaltyShootout;
            currentOutcome.WentToPenalties = Phase == MatchPhase.PenaltyShootout;
            currentOutcome.PrizeMoney = CalculatePrizeMoney();

            MatchFinished?.Invoke(currentOutcome);

            CareerManager.Instance?.RecordMatch(HomeGoals, AwayGoals, true, currentOutcome);
            TournamentManager.Instance?.RecordTournamentMatch(
                TournamentName, RoundName, HomeGoals, AwayGoals,
                HomePenaltyGoals, AwayPenaltyGoals, currentOutcome.WentToPenalties);

            SoundManager.Instance?.PlayWhistleFullTime();
            if (HomeGoals > AwayGoals) SoundManager.Instance?.PlayCrowdCheer();
            else if (HomeGoals < AwayGoals) SoundManager.Instance?.PlayCrowdBoo();
            else SoundManager.Instance?.PlayCrowdCheer();

            Debug.Log($"[MatchManager] انتهت المباراة: {HomeGoals}-{AwayGoals}" +
                      (currentOutcome.WentToPenalties ? $" (ركلات الترجيح: {HomePenaltyGoals}-{AwayPenaltyGoals})" : "") +
                      $" الجائزة: {currentOutcome.PrizeMoney}");
        }

        private long CalculatePrizeMoney()
        {
            long basePrize = 50000;
            long winBonus = HomeGoals > AwayGoals ? 100000 : HomeGoals == AwayGoals ? 25000 : 0;
            long goalBonus = (HomeGoals + AwayGoals) * 5000;
            long penaltyBonus = currentOutcome.WentToPenalties ? 50000 : 0;
            return basePrize + winBonus + goalBonus + penaltyBonus;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = oldTimeScale;
        }
    }
}

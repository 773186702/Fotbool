using System;
using System.Collections.Generic;
using System.Linq;
using Fotbool.Core;
using UnityEngine;

namespace Fotbool.Management
{
    /// <summary>
    /// مدير بطولات الكأس (Cup Tournaments) - نظام خروج المغلوب.
    /// </summary>
    public sealed class CupTournamentManager : MonoBehaviour
    {
        public static CupTournamentManager Instance { get; private set; }

        [Header("إعدادات الكأس")]
        public string TournamentName = "كأس Fotbool";
        public int NumberOfTeams = 16;
        public List<TeamProfile> Teams = new List<TeamProfile>();

        [Serializable]
        public class CupRound
        {
            public string RoundName;
            public List<CupMatch> Matches = new List<CupMatch>();
            public bool Completed;
        }

        [Serializable]
        public class CupMatch
        {
            public TeamProfile HomeTeam;
            public TeamProfile AwayTeam;
            public int HomeGoals;
            public int AwayGoals;
            public int HomePenaltyGoals;
            public int AwayPenaltyGoals;
            public bool WentToExtraTime;
            public bool WentToPenalties;
            public bool Completed;
            public TeamProfile Winner => HomeGoals > AwayGoals ? HomeTeam : (AwayGoals > HomeGoals ? AwayTeam : null);
        }

        public List<CupRound> Rounds = new List<CupRound>();
        public CupRound CurrentRound => Rounds.FirstOrDefault(r => !r.Completed);
        public TeamProfile Champion { get; private set; }

        public event Action<CupMatch> OnMatchCompleted;
        public event Action<CupRound> OnRoundCompleted;
        public event Action<TeamProfile> OnChampionCrowned;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterTeam(TeamProfile team)
        {
            if (team == null || Teams.Contains(team)) return;
            Teams.Add(team);
            Debug.Log($"[CupTournamentManager] تم تسجيل الفريق: {team.name}");
        }

        public void GenerateTournament()
        {
            if (Teams.Count < 2)
            {
                Debug.LogError("[CupTournamentManager] يجب تسجيل فريقين على الأقل");
                return;
            }

            Rounds.Clear();
            var shuffled = Teams.OrderBy(x => Guid.NewGuid()).ToList();

            int teamCount = shuffled.Count;
            int roundsNeeded = (int)Mathf.Ceil(Mathf.Log(teamCount, 2));

            int currentTeams = teamCount;
            for (int r = 0; r < roundsNeeded; r++)
            {
                var round = new CupRound
                {
                    RoundName = GetRoundName(r, roundsNeeded)
                };

                int matchesInRound = currentTeams / 2;
                for (int m = 0; m < matchesInRound; m++)
                {
                    round.Matches.Add(new CupMatch
                    {
                        HomeTeam = shuffled[m * 2],
                        AwayTeam = shuffled[m * 2 + 1]
                    });
                }

                Rounds.Add(round);
                currentTeams = matchesInRound;
            }

            Debug.Log($"[CupTournamentManager] تم إنشاء البطولة: {Rounds.Count} أدوار، {Teams.Count} فريق");
        }

        private string GetRoundName(int roundIndex, int totalRounds)
        {
            if (roundIndex == totalRounds - 1) return "النهائي";
            if (roundIndex == totalRounds - 2) return "نصف النهائي";
            if (roundIndex == totalRounds - 3) return "ربع النهائي";
            return $"الدور {totalRounds - roundIndex - 1}";
        }

        public void RecordCupMatch(TeamProfile home, TeamProfile away, int homeGoals, int awayGoals,
            int homePenaltyGoals = 0, int awayPenaltyGoals = 0, bool wentToExtraTime = false, bool wentToPenalties = false)
        {
            var current = CurrentRound;
            if (current == null) return;

            var match = current.Matches.FirstOrDefault(m =>
                (m.HomeTeam == home && m.AwayTeam == away) ||
                (m.HomeTeam == away && m.AwayTeam == home));

            if (match == null || match.Completed) return;

            match.HomeGoals = homeGoals;
            match.AwayGoals = awayGoals;
            match.HomePenaltyGoals = homePenaltyGoals;
            match.AwayPenaltyGoals = awayPenaltyGoals;
            match.WentToExtraTime = wentToExtraTime;
            match.WentToPenalties = wentToPenalties;
            match.Completed = true;

            OnMatchCompleted?.Invoke(match);

            if (current.Matches.All(m => m.Completed))
                CompleteRound(current);
        }

        private void CompleteRound(CupRound round)
        {
            round.Completed = true;
            OnRoundCompleted?.Invoke(round);

            var winners = round.Matches.Select(m => m.Winner).Where(w => w != null).ToList();

            if (round == Rounds.LastOrDefault())
            {
                Champion = winners.FirstOrDefault();
                OnChampionCrowned?.Invoke(Champion);
                PrizeManager.Instance?.AwardTournamentPrize(TournamentName, "الأول", CupTournamentPrize);
                Debug.Log($"[CupTournamentManager] البطل: {Champion?.name}");
            }
            else
            {
                int nextIndex = Rounds.IndexOf(round) + 1;
                if (nextIndex < Rounds.Count)
                {
                    var nextRound = Rounds[nextIndex];
                    for (int i = 0; i < winners.Count && i < nextRound.Matches.Count * 2; i += 2)
                    {
                        if (i + 1 < winners.Count)
                        {
                            nextRound.Matches[i / 2].HomeTeam = winners[i];
                            nextRound.Matches[i / 2].AwayTeam = winners[i + 1];
                        }
                    }
                }
            }
        }

        [Header("جوائز الكأس")]
        public long CupTournamentPrize = 3000000L;
        public long RunnerUpPrize = 1000000L;

        public List<CupMatch> GetAllMatches() => Rounds.SelectMany(r => r.Matches).ToList();
        public int GetCompletedRounds() => Rounds.Count(r => r.Completed);
    }
}
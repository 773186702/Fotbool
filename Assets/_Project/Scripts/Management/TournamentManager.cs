using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Fotbool.Core;
using Fotbool.Data;

namespace Fotbool.Management
{
    public sealed class TournamentManager : MonoBehaviour
    {
        public static TournamentManager Instance { get; private set; }

        [Header("Tournament Settings")]
        public string TournamentName = "Fotbool League";
        public int NumberOfTeams = 10;
        public int MatchesPerTeam = 18;
        public int PointsForWin = 3;
        public int PointsForDraw = 1;
        public int PointsForLoss = 0;

        public List<TeamProfile> Teams = new List<TeamProfile>();
        public List<MatchResult> Fixtures = new List<MatchResult>();
        public Dictionary<string, int> TeamPoints = new Dictionary<string, int>();
        public Dictionary<string, int> TeamPlayed = new Dictionary<string, int>();
        public Dictionary<string, int> TeamWon = new Dictionary<string, int>();
        public Dictionary<string, int> TeamDrawn = new Dictionary<string, int>();
        public Dictionary<string, int> TeamLost = new Dictionary<string, int>();
        public Dictionary<string, int> TeamGoalsFor = new Dictionary<string, int>();
        public Dictionary<string, int> TeamGoalsAgainst = new Dictionary<string, int>();

        public event Action OnStandingsUpdated;
        public event Action<MatchResult> OnMatchCompleted;

        private int currentMatchIndex = 0;

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
            TeamPoints[team.id] = 0;
            TeamPlayed[team.id] = 0;
            TeamWon[team.id] = 0;
            TeamDrawn[team.id] = 0;
            TeamLost[team.id] = 0;
            TeamGoalsFor[team.id] = 0;
            TeamGoalsAgainst[team.id] = 0;
        }

        public void GenerateFixtures()
        {
            Fixtures.Clear();
            if (Teams.Count < 2) return;
            for (int round = 0; round < 2; round++)
            {
                for (int i = 0; i < Teams.Count; i++)
                {
                    for (int j = i + 1; j < Teams.Count; j++)
                    {
                        Fixtures.Add(new MatchResult
                        {
                            Season = 1,
                            Week = Fixtures.Count + 1,
                            HomeTeamIsClub = false,
                            homeGoals = 0,
                            awayGoals = 0,
                            date = System.DateTime.Now.ToString("yyyy-MM-dd")
                        });
                    }
                }
            }
            Debug.Log($"[TournamentManager] Generated {Fixtures.Count} fixtures.");
        }

        public void RecordMatchResult(TeamProfile home, TeamProfile away, int homeGoals, int awayGoals)
        {
            if (home == null || away == null) return;
            RegisterTeam(home);
            RegisterTeam(away);
            TeamPlayed[home.id]++;
            TeamPlayed[away.id]++;
            TeamGoalsFor[home.id] += homeGoals;
            TeamGoalsAgainst[home.id] += awayGoals;
            TeamGoalsFor[away.id] += awayGoals;
            TeamGoalsAgainst[away.id] += homeGoals;
            if (homeGoals > awayGoals)
            {
                TeamWon[home.id]++; TeamLost[away.id]++;
                TeamPoints[home.id] += PointsForWin;
                TeamPoints[away.id] += PointsForLoss;
            }
            else if (homeGoals < awayGoals)
            {
                TeamWon[away.id]++; TeamLost[home.id]++;
                TeamPoints[away.id] += PointsForWin;
                TeamPoints[home.id] += PointsForLoss;
            }
            else
            {
                TeamDrawn[home.id]++; TeamDrawn[away.id]++;
                TeamPoints[home.id] += PointsForDraw;
                TeamPoints[away.id] += PointsForDraw;
            }
            var result = new MatchResult
            {
                Week = TeamPlayed[home.id],
                Season = 1,
                homeGoals = homeGoals,
                awayGoals = awayGoals,
                HomeTeamIsClub = home.id == CareerManager.Instance?.Club?.id,
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };
            Fixtures.Add(result);
            OnMatchCompleted?.Invoke(result);
            OnStandingsUpdated?.Invoke();
        }

        public List<TeamProfile> GetStandings()
        {
            var sorted = new List<TeamProfile>(Teams);
            sorted.Sort((a, b) =>
            {
                int ptsB = TeamPoints.TryGetValue(b.id, out var pb) ? pb : 0;
                int ptsA = TeamPoints.TryGetValue(a.id, out var pa) ? pa : 0;
                if (ptsB != ptsA) return ptsB.CompareTo(ptsA);
                int gdB = (TeamGoalsFor.TryGetValue(b.id, out var gfB) ? gfB : 0) - (TeamGoalsAgainst.TryGetValue(b.id, out var gaB) ? gaB : 0);
                int gdA = (TeamGoalsFor.TryGetValue(a.id, out var gfA) ? gfA : 0) - (TeamGoalsAgainst.TryGetValue(a.id, out var gaA) ? gaA : 0);
                if (gdB != gdA) return gdB.CompareTo(gdA);
                return (TeamGoalsFor.TryGetValue(b.id, out var gf2) ? gf2 : 0).CompareTo(TeamGoalsFor.TryGetValue(a.id, out var gf1) ? gf1 : 0);
            });
            return sorted;
        }

        public string GetStandingsString()
        {
            var standings = GetStandings();
            var sb = new StringBuilder();
            sb.AppendLine("POS  TEAM                 P  W  D  L  GF  GA  GD  PTS");
            int pos = 1;
            foreach (var t in standings)
            {
                sb.AppendLine($"{pos,-3} {t.name,-20} {TeamPlayed[t.id],2} {TeamWon[t.id],2} {TeamDrawn[t.id],2} {TeamLost[t.id],2} {TeamGoalsFor[t.id],3} {TeamGoalsAgainst[t.id],3} {(TeamGoalsFor[t.id] - TeamGoalsAgainst[t.id]),3} {TeamPoints[t.id],3}");
                pos++;
            }
            return sb.ToString();
        }

        /// <summary>تسجيل نتيجة مباراة في بطولة (دوري أو كأس)</summary>
        public void RecordTournamentMatch(string tournamentName, string roundName, int homeGoals, int awayGoals, int homePenaltyGoals, int awayPenaltyGoals, bool wentToPenalties)
        {
            Debug.Log($"[TournamentManager] مباراة مسجلة: {tournamentName} - {roundName}: {homeGoals}-{awayGoals}" +
                      (wentToPenalties ? $" (ركلات الترجيح: {homePenaltyGoals}-{awayPenaltyGoals})" : ""));
            // يمكن 확장 هنا لتسجيل risultati في بطولات محددة
        }

        /// <summary>نوع البطولة</summary>
        public enum TournamentFormat { League, SingleElimination, DoubleElimination, GroupStage }
    }
}
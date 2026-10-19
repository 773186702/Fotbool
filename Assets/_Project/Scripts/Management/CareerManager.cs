using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Fotbool.Core;
using Fotbool.Data;
using UnityEngine;

namespace Fotbool.Management
{
    public sealed class CareerManager : MonoBehaviour
    {
        public static CareerManager Instance { get; private set; }

        [Header("Save Settings")]
        public string SaveFileName = "fotbool_career.json";
        public bool AutoSave = true;

        public TeamProfile Club { get; private set; }
        public ClubManager ClubManager { get; private set; }
        public int CurrentWeek { get; private set; } = 0;
        public int Season { get; private set; } = 1;
        public int MatchesPlayed { get; private set; } = 0;
        public int Wins { get; private set; } = 0;
        public int Draws { get; private set; } = 0;
        public int Losses { get; private set; } = 0;
        public long TotalEarnings { get; private set; } = 0;

        private readonly List<MatchResult> matchHistory = new List<MatchResult>();
        private string savePath;

        public event Action OnCareerChanged;
        public event Action<MatchResult> OnMatchRecorded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
            ClubManager = FindObjectOfType<ClubManager>();
            if (ClubManager == null)
            {
                var go = new GameObject("ClubManager");
                ClubManager = go.AddComponent<ClubManager>();
            }
        }

        private void Start()
        {
            if (AutoSave && File.Exists(savePath)) LoadCareer();
        }

        public void NewCareer(string clubName, string stadium, Color primary, Color secondary, int crestStyle)
        {
            ClubManager.CreateClub(clubName, stadium, primary, secondary, crestStyle);
            Club = ClubManager.Club;
            CurrentWeek = 0;
            Season = 1;
            MatchesPlayed = 0;
            Wins = Draws = Losses = 0;
            TotalEarnings = 0;
            matchHistory.Clear();
            SaveCareer();
            OnCareerChanged?.Invoke();
        }

        public void RecordMatch(int homeGoals, int awayGoals, bool homeTeamIsClub)
        {
            RecordMatch(homeGoals, awayGoals, homeTeamIsClub, new MatchOutcome());
        }

        public void RecordMatch(int homeGoals, int awayGoals, bool homeTeamIsClub, MatchOutcome outcome)
        {
            if (Club == null) return;
            MatchesPlayed++;
            if (homeGoals > awayGoals && homeTeamIsClub) Wins++;
            else if (homeGoals < awayGoals && !homeTeamIsClub) Wins++;
            else if (homeGoals == awayGoals) Draws++;
            else Losses++;

            long prize = outcome.PrizeMoney > 0 ? outcome.PrizeMoney : (homeGoals * 5000L + awayGoals * 1000L + (Wins * 10000L));
            Club.budget += prize;
            TotalEarnings += prize;

            var result = new MatchResult
            {
                Week = CurrentWeek,
                Season = Season,
                HomeGoals = homeGoals,
                AwayGoals = awayGoals,
                HomeTeamIsClub = homeTeamIsClub,
                Date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };
            matchHistory.Add(result);

            if (CurrentWeek % 38 == 0) Season++;

            if (AutoSave) SaveCareer();
            OnMatchRecorded?.Invoke(result);
            OnCareerChanged?.Invoke();
        }

        public void AdvanceWeek()
        {
            if (ClubManager != null) ClubManager.AdvanceWeek();
            CurrentWeek++;
            if (AutoSave) SaveCareer();
            OnCareerChanged?.Invoke();
        }

        public void SaveCareer()
        {
            if (Club == null) return;
            try
            {
                var data = new CareerSaveData
                {
                    club = ClubManager.Club,
                    week = CurrentWeek,
                    season = Season,
                    matchesPlayed = MatchesPlayed,
                    wins = Wins,
                    draws = Draws,
                    losses = Losses,
                    totalEarnings = TotalEarnings,
                    matchHistory = matchHistory.ToArray()
                };
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(savePath, json, Encoding.UTF8);
            }
            catch (Exception e) { Debug.LogError($"[CareerManager] Save failed: {e.Message}"); }
        }

        public void LoadCareer()
        {
            try
            {
                if (!File.Exists(savePath)) return;
                string json = File.ReadAllText(savePath, Encoding.UTF8);
                var data = JsonUtility.FromJson<CareerSaveData>(json);

                if (ClubManager != null && data.club != null)
                {
                    ClubManager.Club = data.club;
                    Club = data.club;
                }
                CurrentWeek = data.week;
                Season = data.season;
                MatchesPlayed = data.matchesPlayed;
                Wins = data.wins;
                Draws = data.draws;
                Losses = data.losses;
                TotalEarnings = data.totalEarnings;
                matchHistory.Clear();
                if (data.matchHistory != null) matchHistory.AddRange(data.matchHistory);

                OnCareerChanged?.Invoke();
                Debug.Log("[CareerManager] Career loaded successfully.");
            }
            catch (Exception e) { Debug.LogError($"[CareerManager] Load failed: {e.Message}"); }
        }

        public void ClearSave()
        {
            matchHistory.Clear();
            if (File.Exists(savePath)) File.Delete(savePath);
            OnCareerChanged?.Invoke();
        }

        public string GetSavePath() => savePath;
        public IReadOnlyList<MatchResult> GetMatchHistory() => matchHistory;
    }

    [Serializable]
    public class CareerSaveData
    {
        public TeamProfile club;
        public int week;
        public int season;
        public int matchesPlayed;
        public int wins;
        public int draws;
        public int losses;
        public long totalEarnings;
        public MatchResult[] matchHistory;
    }

    [Serializable]
    public class MatchResult
    {
        public int week;
        public int season;
        public int homeGoals;
        public int awayGoals;
        public bool homeTeamIsClub;
        public string date;
    }
}
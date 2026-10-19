using System;
using System.Collections.Generic;
using Fotbool.Core;
using UnityEngine;

namespace Fotbool.Management
{
    /// <summary>
    /// مدير الجوائز والمكافآت - يدير جوائز البطولات، الكؤوس، والجوائز الفردية (الهداف، أفضل لاعب، إلخ).
    /// </summary>
    public sealed class PrizeManager : MonoBehaviour
    {
        public static PrizeManager Instance { get; private set; }

        [Header("جوائز المباريات")]
        public long WinPrize = 100000L;
        public long DrawPrize = 25000L;
        public long LossPrize = 5000L;
        public long GoalBonus = 5000L;

        [Header("جوائز البطولات")]
        public long LeagueChampionPrize = 5000000L;
        public long LeagueRunnerUpPrize = 2000000L;
        public long CupChampionPrize = 3000000L;
        public long CupRunnerUpPrize = 1000000L;
        public long ChampionsLeaguePrize = 10000000L;

        // سجل الجوائز التي حصل عليها النادي
        private readonly List<PrizeRecord> prizeHistory = new List<PrizeRecord>();

        public event Action<PrizeRecord> OnPrizeAwarded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>منح جائزة بعد مباراة</summary>
        public void AwardMatchPrize(bool won, int goalsScored, int goalsConceded)
        {
            long prize = won ? WinPrize : (goalsScored == goalsConceded ? DrawPrize : LossPrize);
            prize += goalsScored * GoalBonus;

            var record = new PrizeRecord
            {
                Type = PrizeType.MatchWin,
                Amount = prize,
                Description = won ? "فوز في المباراة" : "نتيجة المباراة",
                Date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            prizeHistory.Add(record);
            OnPrizeAwarded?.Invoke(record);

            // إضافة إلى ميزانية النادي
            CareerManager.Instance?.Club.budget += prize;
            CareerManager.Instance?.TotalEarnings += prize;

            Debug.Log($"[PrizeManager] جائزة مباراة: {prize} (فوز: {won}, أهداف: {goalsScored})");
        }

        /// <summary>منح جائزة بطولة (دوري/كأس)</summary>
        public void AwardTournamentPrize(string tournamentName, string position, long amount)
        {
            var record = new PrizeRecord
            {
                Type = PrizeType.TournamentWin,
                Amount = amount,
                Description = $"{tournamentName} - المركز {position}",
                Date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            prizeHistory.Add(record);
            OnPrizeAwarded?.Invoke(record);

            CareerManager.Instance?.Club.budget += amount;
            CareerManager.Instance?.TotalEarnings += amount;

            Debug.Log($"[PrizeManager] جائزة بطولة: {amount} ({tournamentName} - المركز {position})");
        }

        /// <summary>منح جائزة فردية (الهداف، أفضل لاعب، إلخ)</summary>
        public void AwardIndividualPrize(string playerName, string awardType, long amount)
        {
            var record = new PrizeRecord
            {
                Type = PrizeType.Individual,
                Amount = amount,
                Description = $"{awardType} - {playerName}",
                Date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            prizeHistory.Add(record);
            OnPrizeAwarded?.Invoke(record);

            Debug.Log($"[PrizeManager] جائزة فردية: {amount} ({awardType} - {playerName})");
        }

        public IReadOnlyList<PrizeRecord> GetPrizeHistory() => prizeHistory;
        public long GetTotalPrizeMoney()
        {
            long total = 0;
            foreach (var p in prizeHistory) total += p.Amount;
            return total;
        }
    }

    /// <summary>أنواع الجوائز</summary>
    public enum PrizeType { MatchWin, TournamentWin, Individual, League, Cup }

    /// <summary>سجل جائزة واحدة</summary>
    [Serializable]
    public class PrizeRecord
    {
        public PrizeType Type;
        public long Amount;
        public string Description;
        public string Date;
    }
}
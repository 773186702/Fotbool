using System;
using Fotbool.Data;
using UnityEngine;

namespace Fotbool.Management
{
    /// <summary>Offline career transactions. All money is integer currency units; wages are weekly.</summary>
    public sealed class ClubManager : MonoBehaviour
    {
        public TeamProfile Club { get; private set; }
        public event Action Changed;
        public int Week { get; private set; }
        public int CoachRating { get; private set; } = 40;
        public long CoachWage { get; private set; }
        private readonly System.Collections.Generic.HashSet<string> trained = new System.Collections.Generic.HashSet<string>();

        public void CreateClub(string name, string stadium, Color primary, Color secondary, int crest)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(stadium))
                throw new ArgumentException("Club and stadium names are required.");
            Club = new TeamProfile { name = name.Trim(), stadiumName = stadium.Trim(),
                primaryColor = primary, secondaryColor = secondary, crestStyleId = Mathf.Max(0, crest) };
            Week = 0; CoachRating = 40; CoachWage = 0; trained.Clear();
            Changed?.Invoke();
        }

        public static long MinimumFee(PlayerProfile player) => player == null ? long.MaxValue : player.marketValue;

        public bool BuyPlayer(TeamProfile seller, string playerId, long fee, long weeklyWage, int years)
        {
            return Transfer(seller, Club, playerId, fee, weeklyWage, years);
        }

        public bool SellPlayer(TeamProfile buyer, string playerId, long fee, long weeklyWage, int years)
        {
            return Transfer(Club, buyer, playerId, fee, weeklyWage, years);
        }

        private bool Transfer(TeamProfile seller, TeamProfile buyer, string id, long fee, long wage, int years)
        {
            if (seller == null || buyer == null || seller == buyer || seller.id == buyer.id ||
                seller.squad == null || buyer.squad == null || string.IsNullOrEmpty(id) ||
                fee < 0 || wage < 0 || years < 1 || years > 5 || buyer.squad.Count >= 30) return false;
            var player = seller.squad.Find(p => p != null && p.id == id);
            if (player == null || buyer.squad.Exists(p => p != null && p.id == id) ||
                fee < MinimumFee(player) || wage < player.wage || buyer.budget < fee ||
                seller.budget < 0 || seller.budget > long.MaxValue - fee) return false;
            // Keep one year of wages in reserve. Checked arithmetic rejects overflow before mutation.
            long wageReserve;
            try
            {
                checked
                {
                    long total = wage + (buyer == Club ? CoachWage : 0);
                    foreach (var p in buyer.squad) total += p.wage;
                    wageReserve = total * 52;
                }
            }
            catch (OverflowException) { return false; }
            if (wageReserve < 0 || buyer.budget - fee < wageReserve) return false;
            seller.squad.Remove(player);
            buyer.squad.Add(player);
            seller.budget += fee;
            buyer.budget -= fee;
            player.wage = wage;
            player.contractYears = years;
            Changed?.Invoke();
            return true;
        }

        public bool HireCoach(int rating, long signingFee, long weeklyWage)
        {
            if (Club == null || rating < 1 || rating > 99 || signingFee < rating * 10000L ||
                weeklyWage < rating * 100L || Club.budget < signingFee) return false;
            Club.budget -= signingFee;
            CoachRating = rating; CoachWage = weeklyWage;
            Changed?.Invoke();
            return true;
        }

        public bool Train(string playerId, AttributeType attribute)
        {
            if (Club == null || (int)attribute < 0 || (int)attribute > 6 || trained.Contains(playerId)) return false;
            var p = Club.squad.Find(x => x.id == playerId);
            if (p == null || p.attributes == null || p.attributes.Length != 7) return false;
            int current = p.GetAttribute(attribute);
            int cap = Mathf.Clamp(p.potential, 1, 99);
            long cost = 5000L + current * 100L;
            if (current >= cap || Club.budget < cost || p.stamina < 20f) return false;
            p.SetAttribute(attribute, Mathf.Min(cap, current + (CoachRating >= 80 ? 2 : 1)));
            p.RecalculateOverall(); p.stamina -= 10f;
            Club.budget -= cost; trained.Add(playerId);
            Changed?.Invoke();
            return true;
        }

        public bool AdvanceWeek()
        {
            if (Club == null) return false;
            long payroll = CoachWage;
            try { checked { foreach (var p in Club.squad) payroll += p.wage; } }
            catch (OverflowException) { return false; }
            if (payroll < 0 || Club.budget < payroll || Week == int.MaxValue) return false;
            Club.budget -= payroll; Week++; trained.Clear();
            foreach (var p in Club.squad)
            {
                p.stamina = Mathf.Min(100f, p.stamina + 20f);
                if (Week % 52 == 0) { p.age++; p.contractYears = Mathf.Max(0, p.contractYears - 1); }
            }
            Changed?.Invoke();
            return true;
        }
    }
}

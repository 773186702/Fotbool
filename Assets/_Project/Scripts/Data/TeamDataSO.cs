using System;
using Fotbool.Core;
using UnityEngine;

namespace Fotbool.Data
{
    [Serializable]
    public class TacticsProfile
    {
        public string formationId = "4-3-3";
        public Mentality mentality = Mentality.Balanced;
        [Range(0, 100)] public int pressingIntensity = 50;   // how aggressively the team hunts the ball
        [Range(0, 100)] public int defensiveLine = 50;       // 0 = deep block, 100 = very high line
        [Range(0, 100)] public int width = 55;               // horizontal spread of the block
        [Range(0, 100)] public int tempo = 50;               // build-up speed
        public bool counterAttack = false;                   // sit deep & spring forward
        public bool offsideTrap = false;
    }

    [Serializable]
    public class TeamProfile
    {
        public string id = Guid.NewGuid().ToString("N");
        public string name = "My Club";
        public string shortName = "CLB";
        public string city = "Madrid";
        public string country = "International";
        public bool isNationalTeam;

        [Header("Identity / Branding")]
        public Color primaryColor = new Color(0.85f, 0.15f, 0.15f);
        public Color secondaryColor = Color.white;
        public int crestStyleId;                 // index into crest generator shapes
        public int crestPatternId;               // stripes / halves / sash...
        public string homeKitId = "kit_home_default";
        public string awayKitId = "kit_away_default";

        [Header("Stadium")]
        public string stadiumName = "Home Arena";
        public int stadiumCapacity = 30_000;

        [Header("Football")]
        public string formationId = "4-3-3";
        public TacticsProfile tactics = new TacticsProfile();
        public System.Collections.Generic.List<PlayerProfile> squad = new System.Collections.Generic.List<PlayerProfile>();

        [Header("Economy")]
        public long budget = 25_000_000;
        [Range(1, 100)] public int reputation = 50;

        /// <summary>Rating of the best available XI (squad is assumed sorted by the caller when needed).</summary>
        public int OverallRating
        {
            get
            {
                if (squad == null || squad.Count == 0) return 50;
                // top-11 average gives a realistic first-team strength
                var sorted = new System.Collections.Generic.List<PlayerProfile>(squad);
                sorted.Sort((a, b) => b.overall.CompareTo(a.overall));
                int n = Mathf.Min(11, sorted.Count);
                int sum = 0;
                for (int i = 0; i < n; i++) sum += sorted[i].overall;
                return n == 0 ? 50 : sum / n;
            }
        }
    }

    [CreateAssetMenu(fileName = "TeamData_", menuName = "Fotbool/Data/Team Data")]
    public class TeamDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string teamName = "My Club";
        public string shortName = "CLB";
        public string city = "Madrid";
        public string country = "Spain";

        [Header("Branding")]
        public Color primaryColor = new Color(0.85f, 0.15f, 0.15f);
        public Color secondaryColor = Color.white;
        public int crestStyleId;
        public int crestPatternId;

        [Header("Stadium")]
        public string stadiumName = "Home Arena";
        public int stadiumCapacity = 30_000;

        [Header("Football")]
        public string formationId = "4-3-3";

        public TeamProfile ToProfile()
        {
            return new TeamProfile
            {
                name = teamName,
                shortName = shortName,
                city = city,
                country = country,
                primaryColor = primaryColor,
                secondaryColor = secondaryColor,
                crestStyleId = crestStyleId,
                crestPatternId = crestPatternId,
                stadiumName = stadiumName,
                stadiumCapacity = stadiumCapacity,
                formationId = formationId,
                tactics = new TacticsProfile { formationId = formationId }
            };
        }
    }
}
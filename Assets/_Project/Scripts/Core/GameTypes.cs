using System;
using UnityEngine;

namespace Fotbool.Core
{
    /// <summary>الجانب الذي يلعب من أجله الفريق في المباراة.</summary>
    public enum TeamSide { Home, Away }

    /// <summary>مراكز اللاعبين في الملعب.</summary>
    public enum PlayerPosition { GK, CB, LB, RB, CDM, CM, CAM, LM, RM, ST, LW, RW }

    /// <summary>عائلة المركز المستخدمة لحساب التقييم العام (Overall).</summary>
    public enum PositionFamily { Goalkeeper, Defender, Midfielder, Attacker }

    /// <summary>سمات اللاعب السبعة (مؤشرها يتطابق مع int[] في PlayerProfile.attributes).</summary>
    public enum AttributeType
    {
        Pace = 0,
        Shooting = 1,
        Passing = 2,
        Dribbling = 3,
        Defending = 4,
        Physicality = 5,
        Goalkeeping = 6
    }

    /// <summary>عقلية الفريق التكتيكية.</summary>
    public enum Mentality { Defensive = 0, Balanced = 1, Attacking = 2 }

    /// <summary>صعوبة الذكاء الاصطناعي (تؤثر على سرعة الرد ودقة التمرير/التسديد).</summary>
    public enum AIDifficulty { VeryEasy = 0, Easy = 1, Medium = 2, Hard = 3, VeryHard = 4 }

    /// <summary>أنواع الأحداث التي تسجل أثناء المباراة.</summary>
    public enum MatchEventType { Goal, YellowCard, RedCard, Foul, Offside, Corner, ThrowIn, GoalKick, Substitution, HalfTime, FullTime }

    /// <summary>نتائج الطعن / التدخل.</summary>
    public enum TackleResult { None, Success, Foul, Missed }

    /// <summary>نوع الركلة الحرة أو ركلة الجزاء.</summary>
    public enum SetPieceType { None, Penalty, FreeKick, Corner, GoalKick, ThrowIn }

    /// <summary>امتدادات مفيدة للعدادات.</summary>
    public static class EnumExtensions
    {
        public static PositionFamily Family(this PlayerPosition position)
        {
            return position switch
            {
                PlayerPosition.GK => PositionFamily.Goalkeeper,
                PlayerPosition.CB or PlayerPosition.LB or PlayerPosition.RB => PositionFamily.Defender,
                PlayerPosition.CDM or PlayerPosition.CM or PlayerPosition.CAM or PlayerPosition.LM or PlayerPosition.RM => PositionFamily.Midfielder,
                _ => PositionFamily.Attacker
            };
        }

        public static string ShortName(this PlayerPosition position)
        {
            return position switch
            {
                PlayerPosition.GK => "GK",
                PlayerPosition.CB => "CB",
                PlayerPosition.LB => "LB",
                PlayerPosition.RB => "RB",
                PlayerPosition.CDM => "CDM",
                PlayerPosition.CM => "CM",
                PlayerPosition.CAM => "CAM",
                PlayerPosition.LM => "LM",
                PlayerPosition.RM => "RM",
                PlayerPosition.ST => "ST",
                PlayerPosition.LW => "LW",
                PlayerPosition.RW => "RW",
                _ => "?"
            };
        }

        public static string ShortName(this AttributeType type)
        {
            return type switch
            {
                AttributeType.Pace => "PAC",
                AttributeType.Shooting => "SHO",
                AttributeType.Passing => "PAS",
                AttributeType.Dribbling => "DRI",
                AttributeType.Defending => "DEF",
                AttributeType.Physicality => "PHY",
                AttributeType.Goalkeeping => "GK",
                _ => "?"
            };
        }
    }

    /// <summary>بيانات حدث واحد في المباراة.</summary>
    [Serializable]
    public struct MatchEvent
    {
        public MatchEventType Type;
        public int Minute;
        public int Second;
        public string PlayerName;
        public string TeamName;
        public TeamSide Side;
        public int MatchMinute => Minute;
    }

    /// <summary>إحصائيات فريق واحدة بعد المباراة.</summary>
    [Serializable]
    public struct TeamMatchStats
    {
        public int Goals;
        public int Shots;
        public int ShotsOnTarget;
        public int PossessionPercent;
        public int Passes;
        public int PassAccuracyPercent;
        public int Tackles;
        public int Fouls;
        public int YellowCards;
        public int RedCards;
        public int Corners;
        public int Offsides;
        public float DistanceCoveredKm;
    }
}
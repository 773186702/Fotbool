using System;
using Fotbool.Core;
using UnityEngine;

namespace Fotbool.Data
{
    /// <summary>
    /// Runtime player model. Uses int[] for attributes so it stays 100% JsonUtility-safe
    /// for the career save system.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string id;
        public string fullName;
        public int age = 24;
        public string nationality = "International";
        public PlayerPosition position = PlayerPosition.CM;
        public int overall = 60;
        public int potential = 75;
        public long marketValue = 1_000_000;
        public long wage = 20_000;
        public int contractYears = 3;
        public float stamina = 100f;   // persisted match fitness
        public float form = 1f;        // 0.85..1.15 multiplies match performance
        public int[] attributes = new int[7];

        public int GetAttribute(AttributeType type) => attributes[(int)type];
        public void SetAttribute(AttributeType type, int value) => attributes[(int)type] = Mathf.Clamp(value, 1, 99);
        public bool IsGoalkeeper => position == PlayerPosition.GK;

        public void RecalculateOverall() => overall = ComputeOverall(position, attributes);

        /// <summary>Position-weighted overall rating (0..99).</summary>
        public static int ComputeOverall(PlayerPosition position, int[] attrs)
        {
            if (attrs == null || attrs.Length != 7)
                throw new ArgumentException("Exactly seven attributes are required.", nameof(attrs));
            float wPace, wShot, wPass, wDrib, wDef, wPhys, wGk;
            switch (position.Family())
            {
                case PositionFamily.Goalkeeper:
                    wPace = 0.02f; wShot = 0.02f; wPass = 0.08f; wDrib = 0.02f; wDef = 0.06f; wPhys = 0.10f; wGk = 0.70f; break;
                case PositionFamily.Defender:
                    wPace = 0.18f; wShot = 0.02f; wPass = 0.14f; wDrib = 0.05f; wDef = 0.36f; wPhys = 0.25f; wGk = 0f; break;
                case PositionFamily.Midfielder:
                    wPace = 0.13f; wShot = 0.14f; wPass = 0.30f; wDrib = 0.20f; wDef = 0.14f; wPhys = 0.09f; wGk = 0f; break;
                default: // Attacker
                    wPace = 0.24f; wShot = 0.30f; wPass = 0.13f; wDrib = 0.20f; wDef = 0.03f; wPhys = 0.10f; wGk = 0f; break;
            }
            float v =
                attrs[(int)AttributeType.Pace] * wPace +
                attrs[(int)AttributeType.Shooting] * wShot +
                attrs[(int)AttributeType.Passing] * wPass +
                attrs[(int)AttributeType.Dribbling] * wDrib +
                attrs[(int)AttributeType.Defending] * wDef +
                attrs[(int)AttributeType.Physicality] * wPhys +
                attrs[(int)AttributeType.Goalkeeping] * wGk;
            return Mathf.Clamp(Mathf.RoundToInt(v), 1, 99);
        }

    }

    [CreateAssetMenu(fileName = "PlayerData_", menuName = "Fotbool/Data/Player Data")]
    public class PlayerDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string playerId;
        public string fullName = "New Player";
        public int age = 24;
        public string nationality = "International";
        public PlayerPosition position = PlayerPosition.CM;

        [Header("Attributes (1-99)")]
        [Range(1, 99)] public int pace = 60;
        [Range(1, 99)] public int shooting = 60;
        [Range(1, 99)] public int passing = 60;
        [Range(1, 99)] public int dribbling = 60;
        [Range(1, 99)] public int defending = 50;
        [Range(1, 99)] public int physicality = 60;
        [Range(1, 99)] public int goalkeeping = 20;

        [Header("Growth & Economy")]
        [Range(40, 99)] public int potential = 78;
        public long marketValue = 1_000_000;
        public long weeklyWage = 20_000;
        public int contractYears = 3;

        public PlayerProfile ToProfile()
        {
            var p = new PlayerProfile
            {
                id = string.IsNullOrEmpty(playerId) ? Guid.NewGuid().ToString("N") : playerId,
                fullName = fullName,
                age = age,
                nationality = nationality,
                position = position,
                potential = potential,
                marketValue = marketValue,
                wage = weeklyWage,
                contractYears = contractYears,
                attributes = new int[7]
            };
            p.SetAttribute(AttributeType.Pace, pace);
            p.SetAttribute(AttributeType.Shooting, shooting);
            p.SetAttribute(AttributeType.Passing, passing);
            p.SetAttribute(AttributeType.Dribbling, dribbling);
            p.SetAttribute(AttributeType.Defending, defending);
            p.SetAttribute(AttributeType.Physicality, physicality);
            p.SetAttribute(AttributeType.Goalkeeping, goalkeeping);
            p.RecalculateOverall();
            return p;
        }
    }
}
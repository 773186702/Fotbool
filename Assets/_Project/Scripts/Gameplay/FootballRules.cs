using System;
using System.Collections.Generic;
using Fotbool.Core;
using Fotbool.Data;
using Fotbool.Gameplay;
using UnityEngine;

namespace Fotbool.Gameplay
{
    public sealed class FootballRules : MonoBehaviour
    {
        public MatchManager Match;
        public TeamProfile HomeTeam;
        public TeamProfile AwayTeam;

        [Header("Offside")]
        public bool EnableOffside = true;

        [Header("Cards")]
        [Range(0f, 1f)] public float FoulToYellowChance = 0.15f;
        [Range(0f, 1f)] public float FoulToRedChance = 0.02f;

        [Header("Set Pieces")]
        public float FreeKickDistanceFromGoal = 17f;
        public float CornerRadius = 1f;

        private readonly Dictionary<PlayerProfile, int> yellowCards = new Dictionary<PlayerProfile, int>();
        private readonly HashSet<PlayerProfile> redCarded = new HashSet<PlayerProfile>();

        public event Action<MatchEvent> OnMatchEvent;

        private void Awake()
        {
            if (Match == null) Match = GetComponent<MatchManager>();
        }

        public SetPieceType HandleBallOutOfBounds(Vector3 ballPosition)
        {
            float absX = Mathf.Abs(ballPosition.x);
            float absZ = Mathf.Abs(ballPosition.z);

            if (absZ > 52.61f)
            {
                bool homeAttacking = Match.HomeAttackSign > 0;
                bool attackingGoalLine = (homeAttacking && ballPosition.z > 0) || (!homeAttacking && ballPosition.z < 0);
                var lastTouch = Match.Ball.LastTouch;
                if (lastTouch != null)
                {
                    if (attackingGoalLine)
                    {
                        RaiseEvent(MatchEventType.Corner, lastTouch);
                        return SetPieceType.Corner;
                    }
                    RaiseEvent(MatchEventType.GoalKick, lastTouch);
                    return SetPieceType.GoalKick;
                }
                RaiseEvent(MatchEventType.GoalKick, null);
                return SetPieceType.GoalKick;
            }

            if (absX > 34.11f)
            {
                var lastTouch = Match.Ball.LastTouch;
                RaiseEvent(MatchEventType.ThrowIn, lastTouch);
                return SetPieceType.ThrowIn;
            }

            return SetPieceType.None;
        }

        public TackleResult EvaluateTackle(PlayerController tackler, PlayerController ballCarrier)
        {
            float defenderDef = tackler.Profile.GetAttribute(AttributeType.Defending) / 99f;
            float attackerDrib = ballCarrier.Profile.GetAttribute(AttributeType.Dribbling) / 99f;
            float successChance = Mathf.Clamp01(0.5f + (defenderDef - attackerDrib) * 0.5f);

            if (UnityEngine.Random.value < successChance * 0.85f)
                return TackleResult.Success;

            if (UnityEngine.Random.value < 0.35f)
            {
                ApplyCard(tackler);
                return TackleResult.Foul;
            }

            return TackleResult.Missed;
        }

        public bool IsOffside(PlayerPosition attacker, Vector3 ballPosition, TeamSide attackingSide)
        {
            if (!EnableOffside) return false;
            if (attacker == PlayerPosition.GK) return false;

            int sign = attackingSide == TeamSide.Home ? 1 : -1;
            if (ballPosition.z * sign < 0f) return false;

            float playerZ = GetPlayerZ(attacker);
            if (playerZ * sign <= ballPosition.z * sign) return false;

            TeamSide defending = attackingSide == TeamSide.Home ? TeamSide.Away : TeamSide.Home;
            float secondLastDefenderZ = float.MaxValue;
            int defendersAhead = 0;
            foreach (var p in Match.Players)
            {
                if (p.Side != defending) continue;
                float pz = p.transform.position.z;
                if (pz * sign < 0f) continue;
                defendersAhead++;
                if (Mathf.Abs(pz) < Mathf.Abs(secondLastDefenderZ)) secondLastDefenderZ = pz;
            }
            if (defendersAhead < 2) return false;
            return playerZ * sign > secondLastDefenderZ * sign;
        }

        public void ApplyCard(PlayerController player)
        {
            if (player == null) return;
            if (redCarded.Contains(player.Profile)) return;

            if (yellowCards.ContainsKey(player.Profile))
            {
                redCarded.Add(player.Profile);
                RaiseEvent(MatchEventType.RedCard, player);
                player.SetMove(Vector3.zero, false);
                return;
            }

            float roll = UnityEngine.Random.value;
            if (roll < FoulToRedChance)
            {
                redCarded.Add(player.Profile);
                RaiseEvent(MatchEventType.RedCard, player);
                player.SetMove(Vector3.zero, false);
            }
            else if (roll < FoulToRedChance + FoulToYellowChance)
            {
                yellowCards[player.Profile] = 1;
                RaiseEvent(MatchEventType.YellowCard, player);
            }
        }

        public bool Substitute(PlayerController outgoing, PlayerProfile incomingProfile)
        {
            if (outgoing == null || incomingProfile == null || Match == null) return false;
            if (!Match.Players.Contains(outgoing)) return false;

            outgoing.Initialize(incomingProfile, outgoing.Side, Match.Ball);
            outgoing.ResetPosition(outgoing.transform.position, outgoing.transform.rotation);
            RaiseEvent(MatchEventType.Substitution, outgoing);
            return true;
        }

        public bool IsSentOff(PlayerProfile profile) => profile != null && redCarded.Contains(profile);
        public int YellowCount(PlayerProfile profile) => yellowCards.TryGetValue(profile, out var v) ? v : 0;

        private void RaiseEvent(MatchEventType type, PlayerController involved)
        {
            var ev = new MatchEvent
            {
                Type = type,
                Minute = Mathf.FloorToInt(Match.Elapsed / 60f),
                Second = Mathf.FloorToInt(Match.Elapsed % 60f),
                PlayerName = involved != null ? involved.Profile.fullName : "System",
                TeamName = involved != null ? (involved.Side == TeamSide.Home ? HomeTeam?.name ?? "Home" : AwayTeam?.name ?? "Away") : "System",
                Side = involved != null ? involved.Side : TeamSide.Home
            };
            OnMatchEvent?.Invoke(ev);
        }
    }
}
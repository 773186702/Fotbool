using System;
using Fotbool.Core;
using Fotbool.Data;
using Fotbool.Gameplay;
using UnityEngine;

namespace Fotbool.AI
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class FootballAI : MonoBehaviour
    {
        public enum State { HoldFormation, Chase, Support, Dribble, Pass, Shoot, Tackle, GuardGoal }
        public AIDifficulty Difficulty = AIDifficulty.Medium;
        public MatchManager Match;
        public TacticalAI Team;
        public int FormationSlot;
        public State CurrentState { get; private set; }
        private PlayerController player;
        private float nextThink;
        private float nextDecision;
        private Node root;

        // Small priority behavior tree selects intent; FSM executes persistent movement states.
        private abstract class Node { public abstract bool Tick(); }
        private sealed class Branch : Node
        {
            private readonly Func<bool> condition;
            private readonly Action action;
            public Branch(Func<bool> condition, Action action) { this.condition = condition; this.action = action; }
            public override bool Tick() { if (!condition()) return false; action(); return true; }
        }
        private sealed class Selector : Node
        {
            private readonly Node[] nodes;
            public Selector(params Node[] nodes) { this.nodes = nodes; }
            public override bool Tick() { foreach (var node in nodes) if (node.Tick()) return true; return false; }
        }
        public static float ReactionSeconds(AIDifficulty level) => Mathf.Lerp(0.8f, 0.12f, (int)level / 4f);
        public static float AimError(AIDifficulty level) => Mathf.Lerp(4.5f, 0.35f, (int)level / 4f);

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            root = new Selector(
                new Branch(() => player.HasBall, DecideOnBall),
                new Branch(() => FormationSlot == 0, () => CurrentState = State.GuardGoal),
                new Branch(IsPresser, () => CurrentState = State.Chase),
                new Branch(() => Match.Ball.LastTouch != null && Match.Ball.LastTouch.Side == player.Side,
                    () => CurrentState = State.Support),
                new Branch(() => true, () => CurrentState = State.HoldFormation));
            nextThink = Time.time + UnityEngine.Random.Range(0f, 0.5f);
        }

        private bool IsPresser()
        {
            return Match.Nearest(player.Side, Match.Ball.transform.position) == player &&
                (Vector3.Distance(player.transform.position, Match.Ball.transform.position) <
                    Mathf.Lerp(14f, 60f, Team.Pressing));
        }

        private void DecideOnBall()
        {
            if (Time.time < nextDecision) { CurrentState = State.Dribble; return; }
            Vector3 goal = new Vector3(0, 0, 52.5f * player.AttackSign);
            float distance = Vector3.Distance(player.transform.position, goal);
            if (FormationSlot == 0) CurrentState = State.Pass;
            else if (distance < Mathf.Lerp(18f, 28f, (int)Difficulty / 4f)) CurrentState = State.Shoot;
            else
            {
                TeamSide other = player.Side == TeamSide.Home ? TeamSide.Away : TeamSide.Home;
                var opponent = Match.Nearest(other, player.transform.position);
                CurrentState = opponent != null && Vector3.Distance(opponent.transform.position, player.transform.position) < 4f
                    ? State.Pass : State.Dribble;
            }
            nextDecision = Time.time + ReactionSeconds(Difficulty);
        }

        private void Update()
        {
            if (player.IsHuman || player.Profile == null || Match == null || Match.Ball == null || Team == null || Match.Phase != MatchPhase.Playing) return;
            if (player.IsSentOff) return;
            if (Time.time >= nextThink)
            {
                root.Tick();
                nextThink = Time.time + ReactionSeconds(Difficulty);
            }
            Vector3 target = Team.Target(FormationSlot, player.AttackSign);
            switch (CurrentState)
            {
                case State.Chase:
                    target = Match.Ball.transform.position + Match.Ball.Body.velocity * Mathf.Lerp(0.05f, 0.45f, (int)Difficulty / 4f);
                    if (player.HasBall && Match.Ball.LastTouch != null && Match.Ball.LastTouch.Side != player.Side)
                    { CurrentState = State.Tackle; player.Tackle(); }
                    break;
                case State.Dribble:
                    target = new Vector3(player.transform.position.x * 0.8f, 1f, 50f * player.AttackSign);
                    break;
                case State.Pass:
                    player.Pass(Match.PassTarget(player), Team.Tactics.counterAttack);
                    CurrentState = State.Support;
                    break;
                case State.Shoot:
                    player.Shoot(new Vector3(UnityEngine.Random.Range(-AimError(Difficulty), AimError(Difficulty)),
                        0, 52.5f * player.AttackSign), 0.75f);
                    CurrentState = State.Support;
                    break;
                case State.GuardGoal:
                    if (Vector3.Distance(player.transform.position, Match.Ball.transform.position) < 7f)
                        target = Match.Ball.transform.position;
                    break;
            }
            Vector3 delta = Vector3.ProjectOnPlane(target - player.transform.position, Vector3.up);
            bool sprint = CurrentState == State.Chase && Difficulty >= AIDifficulty.Medium;
            player.SetMove(delta.magnitude > 0.6f ? delta.normalized : Vector3.zero, sprint);
        }
    }
}

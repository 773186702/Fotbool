using Fotbool.Core;
using Fotbool.Data;
using Fotbool.Gameplay;
using UnityEngine;

namespace Fotbool.AI
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class GoalKeeperAI : MonoBehaviour
    {
        public MatchManager Match;
        public TacticalAI Team;
        public float RushThreshold = 12f;
        public float CatchRadius = 2f;
        public float PunchForce = 12f;
        public float ReturnToLineSpeed = 4f;

        public enum GKState { OnLine, Rushing, Recovering, Distributing }
        public GKState CurrentState { get; private set; } = GKState.OnLine;

        private PlayerController player;
        private BallPhysics ball;
        private Vector3 goalLineCenter;
        private float goalLineZ;
        private float nextThink;
        private bool hasPossession;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            ball = Match?.Ball;
            goalLineZ = 52.5f * player.AttackSign;
            goalLineCenter = new Vector3(0f, 1f, goalLineZ);
        }

        private void Update()
        {
            if (player.IsHuman || player.Profile == null || Match == null || ball == null || Match.Phase != MatchPhase.Playing)
                return;

            if (Time.time >= nextThink)
            {
                Think();
                nextThink = Time.time + 0.25f;
            }

            Execute();
        }

        private void Think()
        {
            Vector3 ballPos = ball.transform.position;
            float distToBall = Vector3.Distance(player.transform.position, ballPos);
            bool ballInPenaltyArea = Mathf.Abs(ballPos.x) < 16.5f &&
                                     ((player.AttackSign > 0 && ballPos.z > 36f) ||
                                      (player.AttackSign < 0 && ballPos.z < -36f));

            if (ballInPenaltyArea && distToBall < RushThreshold && !player.HasBall)
            {
                CurrentState = GKState.Rushing;
            }
            else if (player.HasBall && ball.LastTouch == player)
            {
                CurrentState = GKState.Distributing;
            }
            else if (CurrentState == GKState.Rushing && distToBall > RushThreshold * 1.5f)
            {
                CurrentState = GKState.Recovering;
            }
            else if (CurrentState != GKState.Rushing)
            {
                CurrentState = GKState.OnLine;
            }
        }

        private void Execute()
        {
            Vector3 target = goalLineCenter;

            switch (CurrentState)
            {
                case GKState.OnLine:
                    target = new Vector3(
                        Mathf.Clamp(ball.transform.position.x * 0.5f, -5.5f, 5.5f),
                        1f,
                        goalLineZ
                    );
                    break;

                case GKState.Rushing:
                    target = ball.transform.position;
                    if (Vector3.Distance(player.transform.position, ball.transform.position) < CatchRadius)
                    {
                        CatchBall();
                    }
                    break;

                case GKState.Recovering:
                    target = goalLineCenter;
                    break;

                case GKState.Distributing:
                    Distribute();
                    break;
            }

            Vector3 delta = Vector3.ProjectOnPlane(target - player.transform.position, Vector3.up);
            bool sprint = CurrentState == GKState.Rushing;
            player.SetMove(delta.magnitude > 0.5f ? delta.normalized : Vector3.zero, sprint);
        }

        private void CatchBall()
        {
            if (!ball.CanControl) return;
            ball.Body.velocity = Vector3.zero;
            ball.Body.angularVelocity = Vector3.zero;
            ball.transform.position = player.transform.position + Vector3.up * 1.2f;
            ball.transform.SetParent(player.transform, false);
            hasPossession = true;
            CurrentState = GKState.Distributing;
        }

        private void Distribute()
        {
            if (!hasPossession) return;
            var receiver = Match.Nearest(player.Side, player.transform.position + new Vector3(0, 0, 10f * player.AttackSign), player);
            if (receiver != null)
            {
                ball.transform.SetParent(null);
                ball.Kick(player, (receiver.transform.position - player.transform.position).normalized * 14f, Vector3.zero);
                hasPossession = false;
                CurrentState = GKState.OnLine;
            }
            else
            {
                ball.transform.SetParent(null);
                ball.Kick(player, new Vector3(0, 0, 20f * player.AttackSign), Vector3.zero);
                hasPossession = false;
                CurrentState = GKState.OnLine;
            }
        }

        public void Punch()
        {
            if (!ball.CanControl) return;
            Vector3 away = (ball.transform.position - goalLineCenter).normalized;
            ball.Kick(player, away * PunchForce + Vector3.up * 5f, Vector3.zero);
            hasPossession = false;
            CurrentState = GKState.Recovering;
        }
    }
}
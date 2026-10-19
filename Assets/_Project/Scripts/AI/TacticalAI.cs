using Fotbool.Core;
using Fotbool.Data;
using Fotbool.Gameplay;
using UnityEngine;

namespace Fotbool.AI
{
    /// <summary>Team-level blackboard. Formation coordinates are metres on a 105 x 68m pitch.</summary>
    public sealed class TacticalAI : MonoBehaviour
    {
        public MatchManager Match;
        public TeamSide Side;
        public TacticsProfile Tactics = new TacticsProfile();
        public bool Adaptive = true;
        public float Pressing { get; private set; }
        private float nextThink;
        private float advance;
        private static readonly Vector2[] Formation433 =
        {
            new Vector2(0,-48), new Vector2(-24,-30), new Vector2(-8,-33),
            new Vector2(8,-33), new Vector2(24,-30), new Vector2(0,-20),
            new Vector2(-12,-10), new Vector2(12,-10), new Vector2(-24,2),
            new Vector2(0,8), new Vector2(24,2)
        };
        private static readonly Vector2[] Formation442 =
        {
            new Vector2(0,-48), new Vector2(-24,-30), new Vector2(-8,-33),
            new Vector2(8,-33), new Vector2(24,-30), new Vector2(-24,-10),
            new Vector2(-8,-15), new Vector2(8,-15), new Vector2(24,-10),
            new Vector2(-9,6), new Vector2(9,6)
        };
        public static Vector2 Slot(string formation, int slot)
        {
            return (formation == "4-4-2" ? Formation442 : Formation433)[Mathf.Clamp(slot, 0, 10)];
        }

        private void Update()
        {
            if (Match == null || Match.Phase != MatchPhase.Playing || Time.time < nextThink) return;
            nextThink = Time.time + 1f;
            int difference = (Match.HomeGoals - Match.AwayGoals) * (Side == TeamSide.Home ? 1 : -1);
            advance = ((int)Tactics.mentality - 2) * 4f;
            Pressing = Tactics.pressingIntensity / 100f;
            if (Adaptive && Match.Progress > 0.65f)
            {
                advance += difference < 0 ? 9f : difference > 0 ? -6f : 0f;
                Pressing = Mathf.Clamp01(Pressing + (difference < 0 ? 0.25f : -0.1f));
            }
        }

        public Vector3 Target(int slot, int attackSign)
        {
            Vector2 local = Slot(Tactics.formationId, slot);
            Vector3 ball = Match.Ball.transform.position;
            if (slot == 0)
                return new Vector3(Mathf.Clamp(ball.x * 0.16f, -3f, 3f), 1f, -48f * attackSign);
            float width = Mathf.Lerp(0.65f, 1.15f, Tactics.width / 100f);
            bool ownPossession = Match.Ball.LastTouch != null && Match.Ball.LastTouch.Side == Side;
            float counter = Tactics.counterAttack ? (ownPossession ? 7f : -6f) : 0f;
            float line = (Tactics.defensiveLine - 50) * 0.1f;
            float z = local.y + advance + counter + line + Mathf.Clamp(ball.z * attackSign * 0.35f, -12f, 20f);
            return new Vector3(Mathf.Clamp(local.x * width + ball.x * 0.12f, -31f, 31f), 1f,
                Mathf.Clamp(z, -45f, 43f) * attackSign);
        }
    }
}

using Fotbool.Data;
using UnityEngine;

namespace Fotbool.Gameplay
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class PlayerController : MonoBehaviour
    {
        public PlayerProfile Profile { get; private set; }
        public TeamSide Side { get; private set; }
        public int AttackSign { get; set; } = 1;
        public bool IsHuman { get; set; }
        public BallPhysics Ball { get; private set; }
        public Rigidbody Body { get; private set; }
        public float Fitness { get; private set; } = 100f;
        public bool HasBall => Ball != null && Ball.IsReachable(this);
        public Vector3 MoveIntent { get; private set; }
        public bool Sprinting { get; private set; }
        public bool IsSentOff { get; private set; }
        private float nextAction;
        private Animator animator;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int KickHash = Animator.StringToHash("Kick");

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.mass = 75f;
            Body.constraints = RigidbodyConstraints.FreezeRotation;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            animator = GetComponentInChildren<Animator>();
        }

        public void Initialize(PlayerProfile profile, TeamSide side, BallPhysics ball)
        {
            if (profile == null) throw new System.ArgumentNullException(nameof(profile));
            if (ball == null) throw new System.ArgumentNullException(nameof(ball));
            if (profile.attributes == null || profile.attributes.Length != 7)
                throw new System.ArgumentException("Player requires seven attributes.", nameof(profile));
            Profile = profile;
            Side = side;
            Ball = ball;
            AttackSign = side == TeamSide.Home ? 1 : -1;
            Fitness = Mathf.Clamp(profile.stamina, 0f, 100f);
        }

        public void SetMove(Vector3 worldDirection, bool sprint)
        {
            worldDirection.y = 0;
            MoveIntent = Vector3.ClampMagnitude(worldDirection, 1f);
            Sprinting = sprint;
        }

        private void FixedUpdate()
        {
            if (Profile == null || Ball == null || IsSentOff) return;
            bool sprint = Sprinting && Fitness > 5f && MoveIntent.sqrMagnitude > 0.1f;
            Fitness = Mathf.Clamp(Fitness + (sprint ? -8f : 4f) * Time.fixedDeltaTime, 0f, 100f);
            float speed = Mathf.Lerp(4.1f, 7.3f, Profile.GetAttribute(AttributeType.Pace) / 99f);
            speed *= sprint ? 1.3f : 1f;
            Vector3 desired = MoveIntent * speed;
            Vector3 planar = Vector3.ProjectOnPlane(Body.velocity, Vector3.up);
            Vector3 next = Vector3.MoveTowards(planar, desired, 24f * Time.fixedDeltaTime);
            Body.velocity = new Vector3(next.x, Body.velocity.y, next.z);
            if (MoveIntent.sqrMagnitude > 0.02f)
                Body.MoveRotation(Quaternion.RotateTowards(Body.rotation,
                    Quaternion.LookRotation(MoveIntent), 540f * Time.fixedDeltaTime));
            Ball.Dribble(this, desired);
            if (animator != null) animator.SetFloat(SpeedHash, next.magnitude);
        }

        private bool Strike(Vector3 velocity, Vector3 spin)
        {
            if (Time.time < nextAction || Ball == null || !Ball.Kick(this, velocity, spin)) return false;
            nextAction = Time.time + 0.35f;
            if (animator != null) animator.SetTrigger(KickHash);
            return true;
        }

        public bool Shoot(Vector3 target, float power = 0.8f, float curve = 0f)
        {
            if (Profile == null || Ball == null) return false;
            Vector3 direction = Vector3.ProjectOnPlane(target - transform.position, Vector3.up).normalized;
            if (direction == Vector3.zero) direction = transform.forward;
            float speed = Mathf.Lerp(15f, 33f, Mathf.Clamp01(power)) *
                Mathf.Lerp(0.78f, 1f, Profile.GetAttribute(AttributeType.Shooting) / 99f);
            return Strike(direction * speed + Vector3.up * Mathf.Lerp(2f, 6f, Mathf.Clamp01(power)),
                Vector3.up * Mathf.Clamp(curve, -1f, 1f) * 45f);
        }

        public bool Pass(PlayerController receiver, bool throughBall = false)
        {
            if (Profile == null || receiver == null || receiver.Body == null || receiver == this || receiver.Side != Side) return false;
            Vector3 target = receiver.transform.position + receiver.Body.velocity * (throughBall ? 0.8f : 0.2f);
            if (throughBall) target += Vector3.forward * AttackSign * 4f;
            Vector3 delta = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
            float speed = Mathf.Clamp(delta.magnitude * 0.9f, 8f, throughBall ? 25f : 20f);
            speed *= Mathf.Lerp(0.85f, 1f, Profile.GetAttribute(AttributeType.Passing) / 99f);
            return Strike(delta.normalized * speed + Vector3.up * 0.5f, Vector3.zero);
        }

        public bool Tackle()
        {
            if (Time.time < nextAction || !HasBall) return false;
            return Strike(transform.forward * (5f + Profile.GetAttribute(AttributeType.Defending) * 0.05f), Vector3.zero);
        }

        public bool SkillMove(float direction = 1f)
        {
            if (Profile == null) return false;
            float strength = Mathf.Lerp(2f, 5f, Profile.GetAttribute(AttributeType.Dribbling) / 99f);
            return Strike(transform.forward * 3f + transform.right * Mathf.Sign(direction) * strength, Vector3.up * 12f);
        }

        public void ResetPosition(Vector3 position, Quaternion rotation)
        {
            SetMove(Vector3.zero, false);
            Body.position = position;
            Body.rotation = rotation;
            Body.velocity = Body.angularVelocity = Vector3.zero;
            nextAction = Time.time + 0.5f;
        }

        public void SetSentOff(bool sentOff)
        {
            IsSentOff = sentOff;
            if (sentOff)
            {
                SetMove(Vector3.zero, false);
                Body.velocity = Vector3.zero;
            }
        }
    }
}

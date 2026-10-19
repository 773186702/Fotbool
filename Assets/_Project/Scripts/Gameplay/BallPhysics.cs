using UnityEngine;

namespace Fotbool.Gameplay
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class BallPhysics : MonoBehaviour
    {
        [SerializeField] private float magnusCoefficient = 0.0015f;
        [SerializeField] private float airDrag = 0.002f;
        [SerializeField] private float rollingDeceleration = 0.65f;
        [SerializeField, Range(0f, 1f)] private float restitution = 0.55f;
        [SerializeField] private float maxSpeed = 38f;
        public Rigidbody Body { get; private set; }
        public PlayerController LastTouch { get; private set; }
        public bool CanControl => Time.time >= controlAllowedAt;
        private float controlAllowedAt;
        private float radius;
        private PhysicMaterial material;
        private bool grounded;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.mass = 0.43f;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.maxAngularVelocity = 100f;
            var sphere = GetComponent<SphereCollider>();
            radius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            material = new PhysicMaterial("Football")
            {
                dynamicFriction = 0.15f, staticFriction = 0.2f, bounciness = restitution,
                bounceCombine = PhysicMaterialCombine.Maximum,
                frictionCombine = PhysicMaterialCombine.Minimum
            };
            sphere.sharedMaterial = material;
        }

        private void FixedUpdate()
        {
            Vector3 velocity = Body.velocity;
            Body.AddForce(-airDrag * velocity.magnitude * velocity, ForceMode.Force);
            Body.AddForce(Vector3.ClampMagnitude(magnusCoefficient *
                Vector3.Cross(Body.angularVelocity, velocity), 3f), ForceMode.Force);
            if (grounded)
            {
                Vector3 planar = Vector3.ProjectOnPlane(velocity, Vector3.up);
                float deceleration = Mathf.Min(rollingDeceleration, planar.magnitude / Time.fixedDeltaTime);
                Body.AddForce(-planar.normalized * deceleration, ForceMode.Acceleration);
            }
            Body.velocity = Vector3.ClampMagnitude(Body.velocity, maxSpeed);
            grounded = false; // refreshed only by upward-facing contacts
        }

        private void OnCollisionStay(Collision collision) => UpdateGround(collision);
        private void OnCollisionEnter(Collision collision)
        {
            UpdateGround(collision);
            // Bounded surface-speed variation; no random per-frame energy injection.
            if (grounded) material.bounciness = restitution * Mathf.Lerp(0.92f, 1.04f,
                Mathf.Clamp01(collision.relativeVelocity.magnitude / 25f));
        }

        private void UpdateGround(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
                if (collision.GetContact(i).normal.y > 0.7f) grounded = true;
        }

        public bool IsReachable(PlayerController player, float distance = 1.65f)
        {
            return player != null && CanControl && transform.position.y < player.transform.position.y + 1f &&
                (player.transform.position - transform.position).sqrMagnitude <= distance * distance;
        }

        public bool Kick(PlayerController player, Vector3 velocity, Vector3 spin)
        {
            if (!IsReachable(player) || velocity.sqrMagnitude < 0.01f) return false;
            LastTouch = player;
            Body.velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
            Body.angularVelocity = Vector3.ClampMagnitude(spin, 100f);
            controlAllowedAt = Time.time + 0.22f;
            return true;
        }

        public void Dribble(PlayerController player, Vector3 desiredVelocity)
        {
            if (!IsReachable(player) || desiredVelocity.sqrMagnitude < 0.1f) return;
            LastTouch = player;
            Vector3 target = player.transform.position + desiredVelocity.normalized * 1.05f;
            target.y = radius;
            Vector3 correction = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
            Vector3 acceleration = (desiredVelocity + correction * 6f - Body.velocity) * 8f;
            acceleration.y = 0;
            Body.AddForce(Vector3.ClampMagnitude(acceleration, 35f), ForceMode.Acceleration);
        }

        public void ResetBall(Vector3 position)
        {
            Body.position = position;
            Body.rotation = Quaternion.identity;
            Body.velocity = Body.angularVelocity = Vector3.zero;
            LastTouch = null;
            controlAllowedAt = Time.time + 0.5f;
            grounded = false;
        }

        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}

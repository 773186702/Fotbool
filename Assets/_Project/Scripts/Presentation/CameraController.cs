using Fotbool.Core;
using UnityEngine;

namespace Fotbool.Presentation
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        public enum View { Broadcast, Action, DynamicFollow }
        public View Mode = View.Broadcast;
        public Transform Ball;
        public Transform ControlledPlayer;
        public MatchManager Match;
        public ReplayRecorder Replay;
        [SerializeField] private float responsiveness = 5f;
        private Camera lens;
        private Vector3 replayFocus;
        public bool IsReplaying => Replay != null && Replay.IsPlaying;
        private MatchPhase returnPhase;

        private void Awake() { lens = GetComponent<Camera>(); }
        public void NextView() { Mode = (View)(((int)Mode + 1) % 3); }

        public bool StartReplay()
        {
            if (Replay == null || Match == null || IsReplaying || Match.Phase == MatchPhase.Replay) return false;
            if (!Replay.Begin()) return false;
            returnPhase = Match.Phase;
            Match.SetPhase(MatchPhase.Replay);
            return true;
        }

        public void StopReplay()
        {
            if (!IsReplaying) return;
            Replay.End();
            if (Match != null) Match.SetPhase(returnPhase);
        }

        private void LateUpdate()
        {
            if (Ball == null) return;
            Vector3 focus = Ball.position;
            Vector3 offset;
            float fov;
            if (IsReplaying)
            {
                bool active = Replay.Step(Time.unscaledDeltaTime * 0.6f, out replayFocus);
                focus = replayFocus;
                float angle = Replay.PlaybackTime * 0.25f;
                offset = new Vector3(Mathf.Cos(angle) * 13f, 7f, Mathf.Sin(angle) * 13f);
                fov = 48f;
                if (!active) StopReplay();
            }
            else
            {
                switch (Mode)
                {
                    case View.Action:
                        if (ControlledPlayer != null) focus = Vector3.Lerp(ControlledPlayer.position, focus, 0.65f);
                        offset = new Vector3(0, 6f, -11f); fov = 65f; break;
                    case View.DynamicFollow:
                        Vector3 direction = ControlledPlayer != null ? ControlledPlayer.forward : Vector3.forward;
                        offset = -direction * 15f + Vector3.up * 10f; fov = 60f; break;
                    default: offset = new Vector3(38f, 30f, -7f); fov = 52f; break;
                }
            }
            float blend = 1f - Mathf.Exp(-responsiveness * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, focus + offset, blend);
            Vector3 aim = focus - transform.position;
            if (aim.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aim), blend);
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView, fov, blend);
        }

        private void OnDisable() { StopReplay(); }
    }
}

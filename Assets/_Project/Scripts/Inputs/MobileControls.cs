using Fotbool.Core;
using Fotbool.Data;
using Fotbool.Gameplay;
using Fotbool.Presentation;
using UnityEngine;

namespace Fotbool.Inputs
{
    /// <summary>Dependency-free demo controls. Production UI should bind the same PlayerController API.</summary>
    public sealed class MobileControls : MonoBehaviour
    {
        public MatchManager Match;
        public CameraController View;
        public PlayerController Selected { get; private set; }
        private static readonly string[] Labels = { "Pass J", "Through K", "Shoot L", "Tackle Space", "Skill U", "Switch Tab" };
        private int joystickFinger = -1;
        private Vector2 origin, stick;
        private bool sprint;
        private Rect Button(int index) => new Rect(Screen.width - 330 + index % 3 * 108,
            Screen.height - 180 + index / 3 * 80, 100, 70);

        private void Start() { SwitchPlayer(); }
        public void SwitchPlayer()
        {
            if (Match == null || Match.Ball == null) return;
            if (Selected != null) { Selected.IsHuman = false; Selected.SetMove(Vector3.zero, false); }
            Selected = Match.Nearest(TeamSide.Home, Match.Ball.transform.position);
            //skip sent-off players
            if (Selected != null && Selected.IsSentOff)
            {
                Selected.IsHuman = false;
                Selected = null;
                // try next nearest
                float bestDist = float.PositiveInfinity;
                foreach (var p in Match.Players)
                {
                    if (p.Side != TeamSide.Home || p.IsSentOff) continue;
                    float d = Vector3.Distance(p.transform.position, Match.Ball.transform.position);
                    if (d < bestDist) { bestDist = d; Selected = p; }
                }
            }
            if (Selected != null) { Selected.IsHuman = true; View.ControlledPlayer = Selected.transform; }
        }
        private void Action(int index)
        {
            if (Selected == null || Match.Phase != MatchPhase.Playing) return;
            switch (index)
            {
                case 0: Selected.Pass(Match.PassTarget(Selected)); break;
                case 1: Selected.Pass(Match.PassTarget(Selected), true); break;
                case 2: Selected.Shoot(new Vector3(0, 0, 52.5f * Selected.AttackSign)); break;
                case 3: Selected.Tackle(); break;
                case 4: Selected.SkillMove(); break;
                case 5: SwitchPlayer(); break;
            }
        }
        private void Update()
        {
            if (Match == null || View == null || Selected == null || Match.Phase != MatchPhase.Playing) { Clear(); return; }
            stick = Vector2.zero; sprint = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 guiPoint = new Vector2(touch.position.x, Screen.height - touch.position.y);
                if (touch.phase == TouchPhase.Began)
                {
                    if (touch.position.x < Screen.width * 0.45f && joystickFinger < 0)
                    { joystickFinger = touch.fingerId; origin = touch.position; }
                    else for (int b = 0; b < 6; b++) if (Button(b).Contains(guiPoint)) Action(b);
                }
                if (touch.fingerId == joystickFinger)
                {
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) joystickFinger = -1;
                    else { Vector2 delta = (touch.position - origin) / 90f;
                        stick = Vector2.ClampMagnitude(delta, 1); sprint = delta.magnitude > 1.2f; }
                }
            }
            if (Input.touchCount == 0)
            {
                stick = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
                sprint = Input.GetKey(KeyCode.LeftShift);
                if (Input.GetKeyDown(KeyCode.J)) Action(0);
                if (Input.GetKeyDown(KeyCode.K)) Action(1);
                if (Input.GetKeyDown(KeyCode.L)) Action(2);
                if (Input.GetKeyDown(KeyCode.Space)) Action(3);
                if (Input.GetKeyDown(KeyCode.U)) Action(4);
                if (Input.GetKeyDown(KeyCode.Tab)) Action(5);
                if (Input.GetKeyDown(KeyCode.C)) View.NextView();
            }
            Vector3 forward = Vector3.ProjectOnPlane(View.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Selected.SetMove(forward * stick.y + right * stick.x, sprint);
        }
        private void OnGUI()
        {
            if (Match == null || View == null) return;
            GUI.Label(new Rect(20, 20, 450, 30), $"HOME {Match.HomeGoals} - {Match.AwayGoals} AWAY   {Match.Elapsed:0}s  {Match.Phase}");
            if (GUI.Button(new Rect(20, 60, 100, 45), "Camera")) View.NextView();
            if (GUI.Button(new Rect(130, 60, 100, 45), View.IsReplaying ? "End replay" : "Replay"))
            { if (View.IsReplaying) View.StopReplay(); else View.StartReplay(); }
            for (int i = 0; i < 6; i++)
            {
                bool clicked = GUI.Button(Button(i), Labels[i]);
                if (clicked && !Application.isMobilePlatform) Action(i);
            }
            GUI.Label(new Rect(20, Screen.height - 90, 400, 60), "Drag left side to move; drag farther to sprint.\nDesktop: WASD + Shift.");
        }
        private void Clear() { joystickFinger = -1; stick = Vector2.zero; if (Selected != null) Selected.SetMove(Vector3.zero, false); }
        private void OnApplicationFocus(bool focus) { if (!focus) Clear(); }
        private void OnDisable() { Clear(); }
    }
}

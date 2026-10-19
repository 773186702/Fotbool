using System.Collections.Generic;
using Fotbool.AI;
using Fotbool.Audio;
using Fotbool.Core;
using Fotbool.Data;
using Fotbool.Gameplay;
using Fotbool.Inputs;
using Fotbool.Management;
using Fotbool.Presentation;
using Fotbool.UI;
using UnityEngine;

namespace Fotbool.Core
{
    /// <summary>Add to an empty scene and press Play. Generated geometry is a mechanics demo, not final art.</summary>
    public sealed class MatchBootstrapper : MonoBehaviour
    {
        public AIDifficulty Difficulty = AIDifficulty.Medium;
        public bool InitializeCareer = true;
        public bool InitializeUI = true;

        private readonly List<Material> materials = new List<Material>();
        private GameObject root;

        private Material Material(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null) shader = Shader.Find("Standard");
            if (shader == null) throw new System.InvalidOperationException("Install URP or enable the built-in render pipeline.");
            var material = new Material(shader); material.color = color; materials.Add(material); return material;
        }
        private GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name;
            obj.transform.SetParent(root.transform, false);
            obj.transform.position = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }
        private void Start()
        {
            root = new GameObject("Generated Match");
            var green = Material(new Color(0.08f, 0.35f, 0.12f));
            var white = Material(Color.white);
            var home = Material(new Color(0.8f, 0.1f, 0.12f));
            var away = Material(new Color(0.1f, 0.2f, 0.85f));
            Shape("Pitch", PrimitiveType.Cube, new Vector3(0, -0.25f, 0), new Vector3(72, 0.5f, 110), green);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape("Touchline", PrimitiveType.Cube, new Vector3(side * 34, 0.005f, 0), new Vector3(0.08f, 0.01f, 105), white);
                Shape("Goal line", PrimitiveType.Cube, new Vector3(0, 0.005f, side * 52.5f), new Vector3(68, 0.01f, 0.08f), white);
                foreach (float x in new[] {-3.66f, 3.66f})
                    Shape("Post", PrimitiveType.Cube, new Vector3(x, 1.22f, side * 52.5f), new Vector3(0.12f, 2.44f, 0.12f), white);
                Shape("Crossbar", PrimitiveType.Cube, new Vector3(0, 2.44f, side * 52.5f), new Vector3(7.44f, 0.12f, 0.12f), white);
            }
            var ball = Shape("Ball", PrimitiveType.Sphere, new Vector3(0, 0.2f, 0), Vector3.one * 0.22f, white).AddComponent<BallPhysics>();
            var match = root.AddComponent<MatchManager>();
            var actors = new Transform[23];
            for (int side = 0; side < 2; side++)
            {
                var teamObject = new GameObject("Tactics " + side); teamObject.transform.SetParent(root.transform);
                var tactics = teamObject.AddComponent<TacticalAI>(); tactics.Match = match; tactics.Side = (TeamSide)side;
                int sign = side == 0 ? 1 : -1;
                for (int slot = 0; slot < 11; slot++)
                {
                    Vector2 coordinate = TacticalAI.Slot("4-3-3", slot);
                    // All players start in their own half; AI adopts the attacking shape after kickoff.
                    Vector3 position = new Vector3(coordinate.x, 1f, (coordinate.y > -1 ? -3f : coordinate.y) * sign);
                    var obj = Shape("Player " + side + "-" + slot, PrimitiveType.Capsule, position, Vector3.one, side == 0 ? home : away);
                    obj.transform.rotation = Quaternion.Euler(0, side * 180, 0);
                    var profile = new PlayerProfile { id = System.Guid.NewGuid().ToString("N"), fullName = obj.name,
                        position = slot == 0 ? PlayerPosition.GK : slot < 5 ? PlayerPosition.CB : slot < 8 ? PlayerPosition.CM : PlayerPosition.ST,
                        attributes = new[] {70,70,70,70,70,70,70} };
                    profile.RecalculateOverall();
                    var player = obj.AddComponent<PlayerController>(); player.Initialize(profile, (TeamSide)side, ball);
                    var ai = obj.AddComponent<FootballAI>(); ai.Match = match; ai.Team = tactics; ai.FormationSlot = slot; ai.Difficulty = Difficulty;

                    // slot 0 = حارس المرمى لكل فريق
                    if (slot == 0)
                    {
                        var gk = obj.AddComponent<GoalKeeperAI>();
                        gk.Match = match;
                        gk.Team = tactics;
                    }

                    match.Players.Add(player); actors[side * 11 + slot] = obj.transform;
                }
            }
            actors[22] = ball.transform; match.Initialize(ball);

            // Football rules system
            var rules = root.AddComponent<FootballRules>();
            rules.Match = match;
            rules.HomeTeam = new TeamProfile { name = "Home FC", shortName = "HOM" };
            rules.AwayTeam = new TeamProfile { name = "Away FC", shortName = "AWY" };

            // Sound system
            var sound = root.AddComponent<SoundManager>();

            // Penalty Kick system ( أثناء المباراة )
            var penaltyKick = root.AddComponent<PenaltyKickManager>();
            penaltyKick.Match = match;

            // Penalty Shootout system (ركلات الترجيح)
            var penaltyShootout = root.AddComponent<PenaltyShootoutManager>();
            penaltyShootout.Match = match;

            // Cup Tournament system (بطولات الكأس)
            var cupTournament = root.AddComponent<CupTournamentManager>();
            cupTournament.TournamentName = match.TournamentName;

            // Prize Manager (نظام الجوائز)
            var prizeManager = root.AddComponent<PrizeManager>();

            // Career system (persistent)
            if (InitializeCareer)
            {
                var careerGO = new GameObject("CareerManager");
                var career = careerGO.AddComponent<CareerManager>();
                if (FindObjectOfType<CareerManager>() == null)
                    career.NewCareer("My Club", "Home Arena", Color.white, Color.red, 0);
            }

            // UI system
            if (InitializeUI)
            {
                var uiGO = new GameObject("UIManager");
                var ui = uiGO.AddComponent<UIManager>();
                ui.Initialize(match);
            }

            var camera = Camera.main;
            if (camera == null) { var obj = new GameObject("Main Camera"); obj.tag = "MainCamera"; obj.transform.SetParent(root.transform); camera = obj.AddComponent<Camera>(); obj.AddComponent<AudioListener>(); }
            camera.transform.position = new Vector3(38, 30, -7);
            var view = camera.gameObject.AddComponent<CameraController>(); view.Ball = ball.transform; view.Match = match;
            var replay = root.AddComponent<ReplayRecorder>(); replay.Match = match; replay.Initialize(actors); view.Replay = replay;
            var input = root.AddComponent<MobileControls>(); input.Match = match; input.View = view;
            var lightObject = new GameObject("Sun"); lightObject.transform.SetParent(root.transform);
            var sun = lightObject.AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.1f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            Application.targetFrameRate = 60;
        }
        private void OnDestroy() { if (root != null) Destroy(root); foreach (var material in materials) Destroy(material); }

        private void EnsureGlobalSystems()
        {
            if (FindObjectOfType<SoundManager>() == null)
            {
                var go = new GameObject("SoundManager");
                go.AddComponent<SoundManager>();
            }
        }
    }
}

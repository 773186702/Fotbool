# Architecture and production roadmap

## 1. Implemented source tree

All entries below belong to the absolute root `D:\Fotbool` (indented names are tree entries, not standalone paths).

```text
D:\Fotbool
├── .gitignore
├── .vscode
│   ├── extensions.json
│   └── settings.json
├── Packages
│   └── manifest.json
├── ProjectSettings
│   └── ProjectVersion.txt
├── Assets\_Project
│   ├── Scripts
│   │   ├── Fotbool.Runtime.asmdef
│   │   ├── Data
│   │   │   ├── GameTypes.cs
│   │   │   ├── PlayerDataSO.cs
│   │   │   └── TeamDataSO.cs
│   │   ├── Core
│   │   │   ├── MatchManager.cs
│   │   │   └── MatchBootstrapper.cs
│   │   ├── Gameplay
│   │   │   ├── BallPhysics.cs
│   │   │   └── PlayerController.cs
│   │   ├── AI
│   │   │   ├── FootballAI.cs
│   │   │   └── TacticalAI.cs
│   │   ├── Inputs\MobileControls.cs
│   │   ├── Presentation
│   │   │   ├── CameraController.cs
│   │   │   └── ReplayRecorder.cs
│   │   ├── Management\ClubManager.cs
│   │   └── Performance
│   │       ├── ObjectPool.cs
│   │       └── AdaptiveGraphics.cs
│   └── Tests\EditMode
│       ├── CoreTests.cs
│       └── Fotbool.Tests.EditMode.asmdef
├── Docs\Architecture.md
└── README.md
```

Unity will generate missing project settings, `.meta` GUIDs, package lock, and IDE solution files on import. Commit `.meta`, authored settings, scenes, assets, and `packages-lock.json` after successful import. This skeleton does not yet contain those authored assets.

## 2. Runtime ownership and data flow

- **Composition root:** MatchBootstrapper creates dependencies once; no per-frame scene searches. It is a demonstration factory, not the production scene loader.
- **Data:** PlayerDataSO / TeamDataSO hold authoring data, `ToProfile()` creates mutable runtime records. Do not mutate shared ScriptableObjects during career simulation. Persist stable IDs; generated GUID fallbacks are unsuitable for repeatable content imports.
- **Match:** MatchManager owns clock, score, phase and player registry. Public collections simplify the starter; production should freeze registrations after kickoff and expose read-only views. State transitions currently set global `Time.timeScale`; use a scoped simulation clock before integrating other scenes or network systems.
- **Commands:** MobileControls and FootballAI both call the same PlayerController API. `SetMove` records intent; physics movement and ball forces run in FixedUpdate. Camera rendering runs in LateUpdate. Input UI is replaceable without changing locomotion.
- **AI:** a priority selector picks on-ball, goalkeeper, presser, support, or shape behavior; an FSM executes movement/actions. Staggered decision intervals avoid all agents thinking every frame. TacticalAI computes shape and pressing once a second, adjusting late-game aggression from score. It supports 4-3-3 and 4-4-2, not arbitrary formations yet.
- **Presentation:** CameraController owns view blending; ReplayRecorder owns a 300-sample transform ring. Playback hides live mesh renderers and uses collider-free proxies while the match is paused. It is visual playback, not deterministic physics resimulation.
- **Career:** ClubManager validates a transaction before transferring the same profile between two squads. Fee floor = market value; wage floor = current wage; contracts = 1–5 years; buyer reserves one year of payroll. This is a deterministic acceptance policy, not a dialogue/agent negotiation system. Club customization stores colours/IDs/names, not rendered crests or kit assets.
- **Performance:** pool capacity is bounded (Rent returns null when exhausted); leased objects must not be destroyed externally. Reset particles, subscriptions and UI bindings before Return. AdaptiveGraphics clones a URP asset and adjusts render scale over five-second windows. LOD requires authored LODGroup assets; the script only changes global bias.

## 3. AI tiers and physics model

| Tier | Decision interval (s) | Shot lateral aim error (m) |
|---|---:|---:|
| Beginner | 0.80 | 4.50 |
| Easy | 0.63 | 3.46 |
| Medium | 0.46 | 2.43 |
| Hard | 0.29 | 1.39 |
| Legendary (Professional) | 0.12 | 0.35 |

Difficulty also changes prediction lead, shooting range and sprint permission; it does not silently multiply player stats. These are initial tuning values, not calibrated competitive difficulty.

Ball: gravity/contacts from PhysX; aerodynamic drag `-k * |v| * v`; Magnus force `c * (angularVelocity × velocity)` with bounded force; ground rolling deceleration limited to prevent reverse velocity. Restitution changes are bounded but contact callbacks occur after a solve, so the variation may affect the next contact rather than the current impact. This is a game approximation, not a validated aerodynamic model. Kick velocity assignment is intentional authored control; max speed is a safety bound, not energy conservation.

Player root is a 2m capsule centred 1m above ground. Render meshes may be children; disable animation root motion and supply optional Animator `Speed` float / `Kick` trigger. Foot planting, dribble-touch timing, goalkeeper IK and motion matching are future animation work. High-fidelity players cannot be obtained from code alone.

## 4. Production folder additions (planned, not supplied)

Create under `D:\Fotbool\Assets\_Project`: Scenes (Boot, Frontend, Match), Settings (URP tiers, build profiles), Prefabs (Player, Ball, Stadium, UI), Art (Characters, Pitch, Stadium, Crowd, Kits), Animation (controllers, clips, masks), Audio, Data (clubs, squads, formations, competitions), UI, and Tests/PlayMode.

Later split assemblies into Data/Domain, Simulation, Career, Presentation, Infrastructure, Editor and Tests. Keep dependency direction toward Domain; this starter has a single runtime assembly to avoid circular references while its API evolves. Do not add ECS/Jobs/Burst complexity without a profiler-identified bottleneck.

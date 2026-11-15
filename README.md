# Jemparingan

A Unity archery game inspired by Javanese jemparingan. Two playable modes share the bow, arrow physics, and scoring pipeline: **Traditional** (moving bandulan, molo/jangga/awak scoring) and **Modern** (ring targets). The outdoor arena includes a Joglo and rice-field landscape.

**Editor:** Unity **6000.3.27f1** with URP. See `ProjectSettings/ProjectVersion.txt` for the exact version.

## Play on desktop

1. Open `Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity`.
2. Press **Play**; choose **1** (Traditional) or **2** (Modern).
3. Hold **left mouse** to draw; release to shoot. Use **right mouse + move** to aim, **R** to restart, and **M** to return to mode selection where supported.

Do not regenerate the authored scene to change gameplay code.

## XR development

The XR implementation is **experimental**, not a validated Quest release.

- `Assets/Jemparingan/Scenes/JemparinganQuest.unity` uses the existing Meta `OVRHand` adapter and OpenXR configuration.
- **XR Hands Editor playtest:** use **Tools > Jemparingan > Simulator > Create/Open Jemparingan XRI Playtest** to create a *separate local copy* from the Quest scene, then press Play. Follow [XR Hands playtest](docs/QA_XRI_HAND_SIMULATOR_SMOKE.md).
- `DesktopBowInput`, `XRHandsBowInput`, and `MetaQuestBowInput` implement the same `IJemparinganInput` contract; the game keeps a single shot/scoring path.
- The XR simulator is **not** evidence of a working Android APK or headset tracking. Physical Quest validation is still required.

## Verification

- **Edit Mode:** run **Tools > Jemparingan > Run Gameplay QA (Traditional + Modern)**.
- **Play Mode:** test a full 12-arrow round in *both* desktop modes, including restart, misses, scoring, moving targets, and one shot per release.
- **XR:** check left pinch holds the bow, right pinch draws/releases, and tracking loss cancels without firing. Keep the desktop and Quest source scenes unchanged.

Current QA evidence and limitations are indexed in [docs/README.md](docs/README.md). Do not equate code review or source checks with Unity compilation, device testing, or FPS measurements.

## Key files

- `Assets/Scripts/JemparinganGame.cs` — game loop and runtime presentation.
- `Assets/Scripts/IJemparinganInput.cs` — common input pose.
- `Assets/Scripts/DesktopBowInput.cs`, `XRHandsBowInput.cs`, `MetaQuestBowInput.cs` — platform input adapters.
- `Assets/Scripts/JemparinganArrow.cs`, `JemparinganTarget.cs` — projectile and hit detection.
- `Assets/Scripts/Editor/` — Unity scene setup and validation tools.

**Workflow:** keep scene changes and generated `.meta` files explicit; do not commit `Library/`, `Temp/`, `Logs/` or build outputs. Cleanups are reviewed on feature branches before merging.

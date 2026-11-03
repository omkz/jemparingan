# Jemparingan — RASA: Lighting v3 → Landscape v4 → Playable Environment v5

This is a **separate Unity Editor preview workflow** on branch `feature/safe-environment-preview-v1`. The existing `JemparinganOptimized_v02.unity` scene is not regenerated or modified by these menus. The original game defaults are unchanged unless the new outdoor scene explicitly enables the optional external-target configuration.

## Get the updates

```bash
git status --short
git switch feature/safe-environment-preview-v1
git pull origin feature/safe-environment-preview-v1
```

Commit or stash unrelated local work before switching branches. Open the project in the recorded Unity Editor version **6000.2.14f1**.

## Lighting v3

- A procedural golden-hour skybox material under `Assets/Jemparingan/EnvironmentPreview/`, with camera clear-color fallback if the shader is unavailable.
- One warm directional sunlight source and balanced sky/equator/ground ambient lighting.
- Linear distance fog softens distant scenery.
- **No realtime shadow maps or post-processing dependencies** during this preview. Actual Quest performance is unmeasured.

## Landscape v4

- Paddies in slightly staggered groups rather than a rigid checkerboard.
- Bamboo lane boundaries, side access paths, simple tropical trees and banana-leaf silhouettes.
- Three lightweight authored-by-code ridge meshes replace overscaled spherical mountains.
- Assets generated under `Assets/Jemparingan/EnvironmentPreview/` can later be replaced with dedicated Blender models.

## Scenic preview (no shooting)

Select **Tools > Jemparingan > Environment > Create Joglo Preview v2**. Despite the historical menu label, regeneration includes **Lighting v3 and Landscape v4** along with the Joglo v2 structure.

This only overwrites `Assets/Jemparingan/Scenes/JemparinganEnvironmentPreview_v01.unity` after confirmation. Back up that local file first if you have manually edited it. Inspect the **Game** view from the seated camera.

## Playable Environment v5 (shooting enabled)

1. Select **Tools > Jemparingan > Environment > Create Playable Environment v5**.
2. Unity creates a *different* scene: `Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity`.
3. In Edit Mode, run **Tools > Jemparingan > Environment > Validate Playable v5 Setup**. It verifies the serialized outdoor target anchor, single camera, gameplay controller, desktop input, and no duplicated static bandulan.
4. Press **Play**. The original 12-arrow, three-stage game loop runs inside the large outdoor pendopo environment.
5. Hold **left mouse** to draw, release to fire; hold **right mouse + move mouse** to aim; press **R** to replay.
6. Shoot all 12 arrows and verify hit/miss feedback, score, stage transitions at shots 4 and 8, results, and restart. Inspect Console errors and warnings.
7. Return to **Edit Mode**. Use **Tools > Jemparingan > Environment > Set Target 20m** or **30m**, save the scene and enter Play Mode again. The target stand and its runtime bandulan move together.

**The default Playable v5 target is 10 m.** Speed (20–27 m/s), gravity (4.5 m/s²), aim assist (0.65), and flight lifetime (3 s) are preview tuning values serialized **only in this new scene**. A longer predicted-path marker is used for these shots. They are not physically calibrated simulations of traditional arrows.

## Outdoor readability pass v5.1 — bow and HUD

This optional update addresses the bow and text being hard to see against the bright landscape.

- The **desktop bow** is placed around 0.92 m ahead and 0.33 m to the left of the camera instead of being almost centered at 0.55 m. The existing desktop input logic and release mechanics remain unchanged.
- The outdoor bow mesh is slightly thicker with contrast accents; bowstrings are more visible.
- Outdoor score/title/status text is larger and shorter; small teak-colored contrast panels separate it from rice fields and sky.
- The world-space HUD is parented to the preview camera at approximately 1.85 m so it remains in view while aiming with the mouse. The lower instruction panel is at 1.65 m. This is for **Editor preview readability**; headset ergonomics and comfortable UI placement require on-device validation.
- The old v0.9 scene still uses the original bow presentation and HUD.

**Regenerate the playable scene to enable this.** The code is not automatically applied to an already-saved `.unity` scene:

1. Exit Play Mode and back up any manual changes to `JemparinganPlayableEnvironment_v05.unity`.
2. After `git pull`, select **Tools > Jemparingan > Environment > Create Playable Environment v5**, then confirm overwrite.
3. In Edit Mode, run **Validate Playable v5 Setup**. Its report now checks that `outdoorBowPresentation` is enabled.
4. Press Play and inspect the full bow silhouette, legible score/title, and readable instruction panel.
5. If you changed target distance earlier, set it again after regeneration.

This is a focused legibility improvement, **not** final busur 3D art and **not** a TextMeshPro migration. TextMesh remains the existing lightweight text system; a future VR HUD pass can use SDF text once its dependency is deliberately installed.

## HUD placement v5.2 — clear bow and aim lane

The outdoor HUD, including its opening and end-of-round panels, now sits on the **upper-right side** of the camera view, while the bow remains on the left. The instruction panel is below it on the right. The status/feedback backing is centered within that panel, not placed over the middle of the shooting lane.

**No scene regeneration required** for this fix: these objects are created at runtime by `JemparinganGame.BuildScene()`. After pulling the code, reopen the existing Playable v5 scene and press Play. The existing target distance and any hand-edited layout are preserved.

The placement is tuned for desktop Game view; verify headset field of view and comfort later on physical Quest hardware.

## Safety notes and limitations

- The existing v0.9 gameplay scene remains on its old 2.65 m near-target setup by default.
- The play scene uses the existing `JemparinganGame`, `JemparinganArrow`, `JemparinganTarget`, and `DesktopBowInput`; no second gameplay implementation.
- A **new opt-in flag** in `JemparinganGame` suppresses the tiny procedural pendopo/backdrop and uses the outdoor band's serialized anchor. The flag is false in older scenes.
- `JemparinganArrow.Launch` accepts an optional maximum-flight lifetime, defaulting to the previous **1.6 s**. The outdoor scene uses **3 s**.
- The decorative static bandulan appears only in the scenic preview. In the playable scene, the old game instantiates its single live bandulan beneath the new frame.
- This is still programmer-art blockout. It is not final stylized realism, historically exact Joglo reconstruction, or an optimized Quest build.
- **Unity Editor compilation, actual Play Mode hits, 12-shot rounds, headset hand tracking, and 72 FPS on Quest have not been verified here.** Validate them locally; send the Game-view screenshot and Console output if there is a problem.
- Unity-generated `.unity`, `.mat`, `.asset`, and matching `.meta` files are local until you explicitly commit them.
- To go back to the unmodified main branch, save/stash any local changes, then `git switch main`.

## Suggested acceptance checklist

- [ ] Scene generator completes without compiler/Console errors
- [ ] Sky is visible (no solid black void)
- [ ] Roof and saka guru frame the player's view without blocking the lane
- [ ] Distant mountains read as layered ridges, not giant oval primitives
- [ ] Setup validation reports PASS
- [ ] 12 shots complete and results/replay work in the playable scene
- [ ] Target at 10/20/30m can be repositioned in Edit Mode
- [ ] The original `JemparinganOptimized_v02.unity` scene still plays unchanged

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

## Bow Polish v1 — optional curved bow (runtime)

The **outdoor playable scene** now uses a 1.30m curved, tapered wooden bow generated by `JemparinganBowVisual.cs`, with a wrapped grip, subtle brass collars and a natural-colored string. Both string endpoints are derived from the actual limb mesh curve. The original v0.9 bow remains unchanged when outdoor mode is disabled.

**No scene regeneration required**. Pull the branch, open the existing `JemparinganPlayableEnvironment_v05.unity` scene and press **Play**. Check the bow at rest, at full draw and after release; ensure it remains clear of the upper-right HUD. Verify a full 12-arrow round and the original v0.9 scene.

This is a procedural prototype, not the final Blender art. **Unity compilation, first-person appearance and real Quest performance are not verified here.** See [Bow Polish v1 design and QA spec](BOW_POLISH_V1_SPEC.md).

## Desktop training aim v5.3 — easier aiming without changing Quest

The outdoor playable scene now has an **Editor-only training aid**. The bandulan is genuinely small, and the former aim-assist only worked if the player had already accounted for arrow drop. Training mode is enabled by default in **Unity Editor only**.

- **Right mouse + move:** aim the camera, at a lower outdoor-only sensitivity than v0.9.
- **Hold Shift while right-mouse aiming:** even finer precision for the narrow bandulan.
- **Small reticle at the center:** normally cream; **green when the bandulan is within the training cone (2.8°)**. For green shots, the system corrects gravitational drop and anticipates the existing target sway at the current draw power.
- **Left mouse hold/release:** draw and shoot exactly as before. The reticle is a visual guide, not an extra arrow.
- **T:** toggle training assist/reticle **ON/OFF** during Play Mode. With training OFF, the original 8° falloff aim-assist continues to apply.
- **R:** reset the round.

The initial target distance is 10m. Try the complete 12-shot scoring cycle before increasing to 20m and 30m.

**Implementation:** `DesktopBowInput.cs` modifies sensitivity only when `outdoorBowPresentation` is enabled. `JemparinganGame.cs` adds an Editor-only training sight and solves a low-angle ballistic trajectory with predicted target sway; it does **not** change `JemparinganArrow`, `JemparinganTarget`, scoring, or the Quest input handler.

**No scene regeneration required**: these behavior and UI objects are configured at runtime. Pull changes, open the existing `JemparinganPlayableEnvironment_v05.unity`, and press Play. The implementation and default training behavior require Unity Editor verification; this is not a Quest-validated aiming technique.

## Outdoor HUD polish v5.4 — concise and less distracting

The outdoor gameplay HUD now keeps the **upper-right layout** while reducing repeated text:

- Score line: **SKOR / KENA / PANAH** rather than a long score+combo label. The session results still report combo and bonus RASA.
- Stage changes no longer show a duplicate floating stage banner in the outdoor scene; the phase/status remain visible.
- The dark **feedback strip appears only while a message is active** and hides after ~1.5 seconds. No permanently empty banner.
- Instructions in idle state are shortened to **KIRI TARIK | KANAN BIDIK | SHIFT HALUS**. During draw, the help line still shows **TARIK % | RASA FOKUS/TENANG**. Training mode **T** and restart **R** are documented in the aim section above.
- The training reticle remains visible, but its center text is **empty by default**. Only a green, aligned shot while drawing shows the short instruction **LEPAS**.
- Reticle color/material and text are updated **only when alignment or draw state changes**, rather than rewriting the same content every frame.
- The outdoor end-of-round panel uses shorter two-to-three-line results; the original v0.9 scene's labels and results are unchanged.

**Test:** Pull `feature/safe-environment-preview-v1`, open `JemparinganPlayableEnvironment_v05.unity`, press **Play**. Confirm the bow (left) and bandulan (center) stay unobstructed. Check feedback appears then disappears after a shot, the reticle changes color at the target and shows **LEPAS** only while drawing, and the results panel still includes score/accuracy/center hits/combo/bonus after 12 shots. Test **T**, **R**, and one legacy round if possible.

**No scene regeneration required:** all changes are in runtime UI logic. This is a low-dependency TextMesh cleanup, **not a migration to TextMeshPro**, and Unity/Quest visual validation remains outstanding.

## English-only player-facing UI (internationalization pass)

All in-game Indonesian labels are now in **English**, in both the outdoor playable scene and the original v0.9 gameplay UI. The historical/cultural identity **JEMPARINGAN**, the mechanic name **RASA**, and the target name **bandulan** remain unchanged where used as names.

| Context | English player-visible copy |
|---|---|
| Round headers | `ROUND I • CALM`, `ROUND II • WIND`, `ROUND III • RASA CHALLENGE` |
| Core status | `SCORE`, `HITS`, `ARROWS`, `DRAW`, `RASA: STEADY/FOCUS` |
| Shot events | `ARROW RELEASED`, `HIT`, `BULLSEYE`, `MISS`, `DRAW FURTHER` |
| Practice | `AIM TRAINING ON/OFF`, `RELEASE`, `LMB DRAW | RMB AIM | SHIFT FINE` |
| End of session | `ROUND COMPLETE`, `ACCURACY`, `BEST COMBO`, `RASA BONUS`, `NEW RECORD`, `RESTART` |
| Results | `RASA IN BALANCE`, `TRUE AIM`, `FINDING CALM`, `FIRST STEPS` |

The titles come from `JemparinganScoreRules.cs`, and the rest from `JemparinganGame.cs`. `JemparinganEditorMenu.cs` gameplay QA strings were updated to assert the English names, avoiding failures caused only by translation.

**How to test:** pull the branch, open the existing `JemparinganPlayableEnvironment_v05.unity`, press **Play** and check the intro, aim training (`T`), idle/drawing HUD, stage transitions, hits/misses, final results and restart (`R`). Also inspect the original `JemparinganOptimized_v02.unity` gameplay UI.

**No scene regeneration needed** because the text is generated at runtime. This is an English copy pass rather than a fully switchable localization system. Unity rendering and Play Mode QA have not been run here.

## Web browser demo (Unity 6, Web/WebGL)

A desktop-browser version complements the competition's separate native Meta Quest experience. It is **not WebXR**: the web demo is controlled by mouse/keyboard; the Quest build requires its own XR integration and hardware QA.

### Source compatibility

`JemparinganGame.UsesDesktopInput()` now selects `DesktopBowInput` on `UNITY_WEBGL && !UNITY_EDITOR` and preserves the previous Editor-vs-Quest behavior on other builds. The practice reticle and training toggle, `R` restart, `T` training, left mouse draw/release, and right mouse aim are available in browser builds. Physics, RASA scoring, and `JemparinganTarget` are not changed for Web.

### Build on Linux Mint using Unity 6000.2.x

1. Unity Hub → **Installs** → find the same editor version as this project → **Add Modules** → **Web Build Support**. Restart the Editor if needed.
2. Switch to `feature/safe-environment-preview-v1` and pull the latest source.
3. Unity → **File → Build Profiles → Add Build Profile → Web** (if needed), then **Switch Profile**.
4. In Scene List, include **`Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity`** as **scene 0**. Remove or disable old prototype scenes for this demo.
5. Select **Build and Run** first, targeting e.g. `Builds/Jemparingan-Web`. Unity serves it locally and opens your default browser. Do not assume a build works when opening `index.html` using the `file://` protocol.
6. Play and verify: crosshair appears and changes green at the bandulan; left mouse draws/releases; right mouse aims; Shift precision; `T` toggles training; `R` restarts; score and stages still work.
7. With a successful local run, use **Publish to Play** in the Web Build Profile for a Unity Play project. Confirm visibility (private/unlisted vs public) **before sharing**. Alternatively, zip the Web build output **contents**, keeping `index.html` at the root, and upload it as an HTML game on itch.io.

**Limitations:** Actual Web compilation, browser graphics, input focus, caching, download size and live play require local building and testing. This document does **not** claim deployment is complete. Browser WebGL is separate from Quest VR. Keep Web output folders ignored by Git (the project already ignores `Builds/`). Beware of HTTPS/compression server configuration on custom hosts.

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

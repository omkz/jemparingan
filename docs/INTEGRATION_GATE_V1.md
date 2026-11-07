# Jemparingan — Desktop Polish Integration Gate v1

**Target:** Unity 6000.2.14f1, playable outdoor scene `Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity`.

**Status:** Proposed integration from the stacked development branches; **not a passed release test**. Desktop Traditional and Modern are the current game modes. Meta Quest is a separate future device-validation gate.

## Branch and change provenance

This integration branch starts at `polish/first-person-left-hand-v2` (PR #12). The full ancestry includes:

1. PR #6 — remove Classic/Challenge, keep Traditional and Modern.
2. PR #7 — three progressive Traditional stages with shared desktop bow.
3. PR #8 — shared bow and left-hand polish, arrow visual polish included in branch history.
4. PR #10 — joglo and golden-hour lighting, **generated mesh/materials committed**.
5. PR #11 — runtime-once rice/foliage/mountain overlays.
6. PR #12 — first-person left-hand wrist/cuff polish.

The integration PR targets `main` directly and is kept as a **draft** until the checks below pass. Do not merge the six stacked PRs *and* this integration PR independently: choose one integration strategy to avoid redundant merges. No existing PR is automatically merged or closed.

## Blocking issue: Joglo visual state is not serialized to the playable scene

The committed `JemparinganPlayableEnvironment_v05.unity` file was byte-identical on `main` and `polish/first-person-left-hand-v2` when this gate was created. PR #10 adds all required `JogloPolish_v01` materials, skybox and joinery mesh but **not** the scene changes that reference them.

**How to fix in Unity (one-time; subsequent checkouts need no Apply):**

1. Check out `integrate/desktop-polish-v1` and open the v5 playable scene in **Edit Mode** (not Play).
2. Select **Tools > Jemparingan > Environment > Apply Joglo + Lighting Polish v1**. Do **not** choose `Create Playable Environment v5`: that generator rebuilds the scene.
3. Look at the roof, wood materials, sky and bandulan; confirm the aiming lane is unobstructed. Optionally run **Validate Joglo Polish v1**.
4. **Ctrl+S** in Unity. Ensure `Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity` appears as modified in `git status --short`.
5. Stage and commit the saved scene (plus any additional generated `.mat`, `.asset` or `.meta` files actually created):
   ```bash
   git add Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity
   git add Assets/Jemparingan/EnvironmentPreview/
   git diff --cached --stat
   git commit -m "Bake joglo polish scene references into playable v5"
   git push origin integrate/desktop-polish-v1
   ```
6. Reopen Unity from a **fresh checkout or after reimport** and confirm the polished joglo is present in **Edit Mode**, without any Apply action. If the scene file still shows no Git diff after Apply and Ctrl+S, do not claim persistence; inspect the Console/scene save path.

The landscape PR #11 remains a separate automatic Play Mode overlay; its mesh objects are not persisted to scene. That is intentional for prototyping and is assessed in profiling.

## Build Settings

Prior to integration, `ProjectSettings/EditorBuildSettings.asset` had `m_Scenes: []`. This branch registers the playable v5 scene as the sole enabled build entry (scene index 0) using its committed `.unity.meta` GUID. Check Unity **File > Build Profiles / Build Settings > Scenes** before making a player build; no platform build is produced by this change alone.

## Go/no-go: editor + playtest

- [ ] No Unity Console compile errors and no missing material/mesh/script references after import.
- [ ] Run **Tools > Jemparingan > Run Gameplay QA (Traditional + Modern)** in Edit Mode. Follow any failures back to code rather than bypassing them.
- [ ] Open v5, start Traditional, finish **12 shots** through stages 1–3 (4 shots each), inspect red/yellow/white hit scoring, moving target/cord, hit and miss, stage transition and results.
- [ ] Restart Traditional, including **R while string is held**: release must not fire a phantom shot.
- [ ] Start Modern, complete its progression; check physical sight calibration, target-ring scoring, moving targets, arrow cam return, impact feedback and restart.
- [ ] At idle / partial / full draw, visually inspect shared left hand, rolled wrist cuff, bow grip, string and nocked arrow (no gaps/clipping/occluded target).
- [ ] Check landscape overlays appear once per scene load; optional Play Mode menu **Validate Landscape Polish v1** passes; no duplicate groups on scene reload or new game.
- [ ] Verify new joglo material/sky/geometry is preserved in **Edit Mode on another checkout**, not only Play Mode on the author's workstation.
- [ ] Profile representative gameplay in **Window > Analysis > Profiler** (CPU, GC allocations, rendering, number of draw calls and startup hitches). Record hardware / settings and compare with overlay disabled if it becomes a suspected bottleneck.
- [ ] Build a **desktop development player** with v5 scene as the entry point; open, complete a round, replay and confirm no crash or missing scene. Unity Play in Editor does not replace player validation.

**No-go:** missing saved joglo scene update, missing materials, red compiler errors, incorrect scoring, a phantom arrow after restart, unfinishable round, duplicate runtime overlays, or serious startup/frame-time regression. Do not describe Quest performance as validated without actual headset measurements.

## Post-gate

After the integration is approved and merged once, close redundant stacked PRs with an explanation, keep `main` as the sole baseline, and start the subsequent Shooting Feel polish from updated `main`. Don't begin large visual/feature rewrites while this gate remains blocked.

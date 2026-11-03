# JEMPARINGAN — RASA | Bow Polish v1 — Design & Implementation Specification

**Status:** Specification only. **No C# or Unity scene modifications in this step.**
**Target:** Unity 6000.2.14f1 / URP, desktop-playable outdoor environment; later Meta Quest hand-tracking experience.
**Art direction:** A **Javanese-inspired traditional wooden bow**, tastefully stylized for modern VR. This is an artistic interpretation, not a claim of a precise museum-grade historic bow.

## 1. Goal

Replace the present bow's visual "four short straight rods and a thin string" feel with a graceful, recognizable wooden archery bow that remains readable in first person against green foliage and a warm sky.

**Visual keywords:** elegant, restrained, dark teak, warm grain-like variation, wrapped grip, fine brass accents, a continuous curved silhouette.

**Not in scope:** altering arrow ballistics, input gestures, shooting direction, RASA focus, score logic, stage progression, target distances, environment geometry, Quest SDK installation, final textured Blender models.

## 2. Shape / dimensions (Unity meters)

The bow's grip-center is local `(0,0,0)`, oriented along its local Y axis, with the arrow firing along local **+Z**. The model stays positioned by the existing `_bow.SetPositionAndRotation(pose.BowPosition, Quaternion.LookRotation(aim, Vector3.up))`.

| Element | Design target | Notes |
|---|---|---|
| Overall bow tip-to-tip | **1.30 m** (local Y: -0.65 to +0.65) | Visual target for the *next* implementation; preserve game aim and projectile spawn |
| Upper/lower limb | Two gently curved tapered meshes | Symmetric prototype is fine; avoid a literal Japanese asymmetrical yumi silhouette |
| Limb width at grip | ~0.035–0.045 m | Wider, readable near hand |
| Limb width near tip | ~0.012–0.018 m | Smooth taper, no abrupt cylinder joints |
| Limb depth | ~0.020–0.030 m | Enough volume for soft highlights, not oversized |
| Maximum forward/rear sweep | ~0.08–0.12 m | Curved along bow's local X/Z silhouette; tune by screenshot |
| Grip length | ~0.15–0.18 m | Center at Y=0 |
| Grip thickness | ~0.04–0.05 m | Brown wrapped sleeve over dark wood |
| String | Light beige, anchored to both tips | Clear against foliage; remain visually continuous while drawing |
| Accents | 2 narrow muted-brass bands near grip | Avoid excessive glowing/fantasy ornaments |

The existing prototype has tip points near `y=±0.53`. A future v1 implementation must **update tip geometry and the two `UpdateBowStrings` endpoints together** if changing them to `±0.65`, so the string is never detached from the bow.

## 3. Curved mesh specification

- Build one continuous-looking tapered limb per side. Suggested first implementation: a custom swept rectangular/rounded-section mesh with **10–14 segments per limb** and shared vertices/normals for smooth silhouette.
- Small sculpted sweep across X/Z; no sharp kinks between shaft sections.
- Preserve local **Y-up, +Z shoot-forward** conventions and keep the grip origin at hand position.
- No colliders or Rigidbodies on bow cosmetics; real hit logic remains on the existing arrow/target classes.
- Generate/reuse meshes and materials without allocating a fresh mesh every frame. Build once per session, or use serialized assets/prefab in a later pass.
- Aim for simple geometry and very few material changes; do not claim Quest performance without device profiling.

## 4. Material / color palette

| Region | Color reference (approx.) | Surface |
|---|---|---|
| Main limbs | Dark warm teak **#543529** | Satin / subtle highlight, not black |
| Limb edge/highlight | Warm teak **#A46E42** | Restrained, helps silhouette |
| Grip wrap | Aged leather brown **#493127** | Matte, five to seven visible bands |
| Tip accents | Light wood **#C8A079** | Distinguishes tip from foliage |
| Fine inlay | Muted brass **#B99559** | No emissive glow |
| Bowstring | Natural cord **#E8D8B5** | Consistent width; avoid pure white |

For Bow Polish v1 these can be **URP-lit solid colors**; real wood grain, normal maps and physically modeled carving belong to the later Blender pass.

## 5. First-person presentation

- Keep the existing **outdoor-only desktop bow offset** in `DesktopBowInput`, approximately 0.92 m forward, 0.33 m left, 0.18 m below eye level as the baseline.
- The actual bow must be visible when **not drawing**, when **fully drawn**, and **on release**.
- The right-side HUD must not cover the bow or the center shooting lane; do not relocate HUD back over the weapon.
- Never solve clipping by moving the gameplay target or distorting projectile origin. Tune cosmetics/offset only.
- Keep view comfortable for seated play; real Quest hand-pose placement remains unvalidated until physical testing.

## 6. String / arrow visual behavior

- Two dynamic bowstring sections connect top and bottom tips to the existing pull marker.
- String grows taut at rest and visibly bends when `StringPinched` is held; restore at release.
- Bowstring thickness is modest and consistent (preview target radius ~3–4 mm, adjust visually).
- Optional *static preview nock bead* near pull marker is acceptable; do not add visual clutter or extra gameplay collision.
- A visually aligned arrow-on-string preview is desirable **only if** it follows the existing pose and cannot be confused with an extra fired projectile; defer if risky.

## 7. Controlled implementation plan (future option 2)

1. **Curved silhouette:** replace straight limb rods with reusable tapered curved meshes; keep `_bow` transform and shoot logic intact.
2. **Grip + wrap:** add one continuous grip shape and 5–7 slim wrap bands.
3. **Tips and string:** align both endpoints with actual mesh tips; test draw/restore.
4. **Materials:** apply the palette with 2 restrained brass trims and stronger contrast.
5. **Visual QA:** screenshots from the current outdoor Playable v5 view, draw/release frames, both left/center target visibility, and an old v0.9 scene regression check.
6. **Only after desktop acceptance:** make Blender asset replacements and Quest-specific hand positioning.

Implement the change behind the existing `useOutdoorEnvironment` branch (or an opt-in serialized presentation flag). Do **not** unintentionally change the original v0.9 standalone bow appearance on `main`.

## 8. Acceptance criteria

- [ ] Looks like a single curved wooden bow, not rods joined at angles.
- [ ] No visible gaps between limb, grip, tips and string at rest or full draw.
- [ ] The grip wrap and subtle brass details are visible in the Editor Game view.
- [ ] Bow stays legible against both bright sky and dense greenery.
- [ ] Right-hand HUD does not overlap the bow; target remains unobstructed.
- [ ] Existing desktop controls, shot-count, scoring, RASA, 10–30 m outdoor target and replay behavior remain unchanged.
- [ ] Visual QA includes a **Play Mode** screenshot, not only a Scene-view screenshot.
- [ ] Compiles under Unity 6000.2.14f1. Physical Quest hand tracking and performance are separately validated later.
- [ ] Changes stay in `feature/safe-environment-preview-v1` until user approval.

## Source context

Current implementations at specification time:
- `Assets/Scripts/JemparinganGame.cs`: `BuildBow()`, `UpdateBowStrings()`, `PositionString()`, projectile spawn.
- `Assets/Scripts/DesktopBowInput.cs`: the **outdoor-only** first-person bow pose.
- `Assets/Scripts/JemparinganArt.cs`: procedural primitives and shared materials.
- `docs/ENVIRONMENT_V3_V5.md`: environment/gameplay and readability QA.

**Decision for now:** approve design intent; defer code implementation to option 2.

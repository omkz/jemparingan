# Modern Archery v1 — Playtest notes

Modern Archery is a fourth, experimental desktop mode in the existing
`JemparinganPlayableEnvironment_v05` scene. It reuses the current bow,
first-person camera/input, procedural environment, arrow pool and
gravity-based projectile. No external assets, scene regeneration, or
separate physics system are required.

## What's playable

- Main menu: **4 / Modern Archery** (or click the fourth mode card)
- Three circular boards with slightly different placements and distances
- Five visible rings, scored by where the existing projectile tip crosses
  each board's front plane: **10 gold / 8 red / 6 blue / 4 dark / 2 white**
- **12 arrows** per attempt (Inspector: `modernArrowCount`)
- Live score, number of hits, remaining arrows, bullseyes, a results
  overlay, and persistent best score (`JemparinganModernBestScore`)
- **R** retry, **M** return to modes (including mid-round)
- Existing LMB draw/release, RMB aim and Shift fine aiming
- Each Modern shot uses Arrow Cam by default (press **Esc** to skip; disable via Inspector for normal first-person shooting)
- Arrow Cam runs on every Modern shot by default (Inspector toggle: `modernEveryShotArrowCam`), independently from the existing Challenge toggle. `Esc` skips an active follow; hit briefly holds the embedded projectile, miss returns to player view.

The three boards are spawned procedurally near the existing outdoor
target anchor, while the original bandulan and hanging cord are hidden.
Classic/Traditional/Challenge restore their own target presentation,
scoring and state when selected. The modern first-person grip/forearm
is reused, but the seated character geometry is hidden.

## Implementation

- `JemparinganModernTarget.cs`: three-dimensional ring visuals,
  swept-tip circular collision, exact ring boundaries and impact marker
- `JemparinganArrow.cs`: optional Modern target array/callback added
  to the existing Launch lifecycle, preserving all existing call sites
- `JemparinganGame.cs`: Modern mode selection, procedural board layout,
  score and high score, mode transitions and UI
- `JemparinganEditorMenu.cs`: additional deterministic QA checks for
  ring boundaries, swept hit, miss and near-parallel segment

## Modern visuals: gawang removal and hit polish

- The authored v05 gawang lives under `Preview Bandulan | movable target`,
  alongside the `Gameplay bandulan anchor | 1.79m above ground` object.
  Merely hiding the generated bandulan leaves the authored two uprights and
  crossbar visible. Modern now temporarily deactivates this **entire preview
  frame root**; Classic, Traditional, Challenge and the mode menu restore its
  original active state. The generated gameplay bandulan is a separate root.
- Only Modern flight trails are slightly wider/longer. Original trail
  settings are cached and restored on subsequent non-Modern launches.
- Modern boards now use a small, reusable impact-spark burst, ring-points
  popup and damped face recoil. The stand, projectile trajectory, scoring
  boundaries, collision test and original gameplay modes are unchanged.
- Arrow Cam is now shared between **Challenge and Modern**, with independent Inspector toggles. Classic and Traditional remain unaffected.

### Specific regression playtest

1. Start Modern and inspect all three targets. No authored bandulan crossbar,
   uprights, hanging cord or target should be in the sightline.
2. Hit rings on each board. Confirm +2/4/6/8/10 score popups, subtle wobble,
   small sparks, and the unchanged accumulated score.
3. Miss deliberately: no score popup or target recoil.
4. Fire multiple shots: each Modern shot should trigger the existing Arrow Cam, follow its real projectile, hold briefly on hit and return on miss. Press Esc to skip. No stuck camera or mouse-look suppression.
5. Press M and enter Classic, Traditional, and Challenge in turn: the
   authored frame is restored exactly, including when Modern is exited mid-round.
6. Return to Modern, retry, then switch back: no pooled arrow trail leak.
7. Run Unity Play Mode and gameplay QA; verify Console. These checks
   have **not** been executed by this GitHub-only code change.

## First-person hand and forearm cleanup

The same procedural left grip hand appears in Traditional, Challenge and Modern.
Earlier the forearm mesh started within the camera frustum, exposing a flat
sleeve end and making the arm look cut off. The mesh now begins below the
visible frame (computed from the camera FOV), uses more interpolation rings,
and includes a small palm volume behind the procedural fingers.

Play Mode visual checks:

1. Modern: keep the bow idle and rotate view left/right/up/down. The left
   sleeve should enter smoothly from the lower screen edge, without a visible
   severed flat cap.
2. Draw and release several shots. The hand stays attached to the actual
   leather grip without obscuring the sightline or affecting arrow origin.
3. Check Traditional and Challenge: same first-person hand improvement,
   unchanged scoring, posture visibility and Arrow Cam.
4. Check Modern Arrow Cam and Esc skip: mouse look resumes normally when
   the follow ends and the hand returns to the first-person bow view.
5. If a half-hand is still visible, capture a Game View screenshot and
   camera FOV; it may be a different clipping/framing issue.

This is a code-only change; Unity compile and rendering tests are pending.

## Modern right-hand draw visual (experimental)

Modern desktop now shows **only a small right-hand pinch and narrow sleeve edge**
while LMB is held. It follows the existing cosmetic string/nock position in
screen space, with conservative lower-right framing and near-clip protection.
On release the right-hand visual hides immediately. It has no collider and does
not change string pull, the left grip hand, shot direction, projectile, scoring,
or Arrow Cam. Traditional and Challenge remain left-hand-only.

Play Mode visual checks:

1. Enter Modern, keep LMB released: no floating right hand.
2. Hold LMB: a little right palm/index/thumb appears to the lower right of
   the sightline and moves with draw progress. Target center stays visible.
3. Release: right hand disappears and the existing Arrow Cam follows the
   projectile; on return the left grip is still correctly positioned.
4. Aim up/down/right/left at all three modern boards and hold a full draw:
   no giant near-camera hand, floating fingers, cut-off sleeve cap or collision.
5. Switch to Traditional and Challenge and draw: the right hand is not
   rendered, and previous gameplay and arrow camera behavior remain intact.
6. Check game Console for errors and inspect framing at the chosen Game View
   resolution. GitHub code inspection is not Unity runtime validation.

### Right-hand visual refinement

The first right-hand prototype used a sphere palm, cylinder fingers and
long cylinder sleeve; it looked disconnected at first-person scale and
could float beside the string because the pose clamped to fixed viewport
coordinates. The refinement uses a small tapered mesh for a pinching
thumb/index/middle-finger silhouette and a short cuff. Its contact point
follows the visual nock in world space rather than moving away from it.

Verify in **Modern** only: hold LMB with the string near and far from full
draw, check that the fingertips touch the string, the small hand is not
obtrusive, and it hides on release or near-camera clipping. Check the
original left grip hand and aim remain unchanged. This is still a low-poly
procedural placeholder, not a rigged 3D hand asset.

### Draw-hand proportion tuning

The bow remains at its established first-person position; moving it would
also shift the existing projectile launch origin. Instead, the cosmetic
Modern right hand has been scaled down to **64%** at its reference camera
distance (0.65m). Its visual-only local scale decreases as the nock gets
closer to the camera, so drawing the string does not make the hand appear
enormous in perspective. The fingertips remain at the true nock position.

In Play Mode, compare Modern before drawing, mid-draw and full-draw at the
same camera angle. The right hand should have a consistent apparent size,
remain subordinate to the bow, stay attached to the string, and not block
the target. Check Arrow Cam, Classic, Traditional and Challenge for any
regressions. This adjustment has not been rendered or tested in Unity yet.

### Right hand alignment to the existing bow

The small Modern draw hand now uses the **current bow's rotation** and the
actual visual nock's world position, rather than orienting to the camera.
This lets it follow the bow's 8-degree cant and stay attached while aiming
up/down/sideways. The grip, arrow origin, physics and the bow's existing
0.70m camera offset are not changed. Its hand-only perspective scaling
remains in place.

Play Mode validation: compare aiming left/right, up/down, half draw and
full draw; fingertips should stay at the string while the hand remains
small and does not obscure the ring centers. Verify the hand disappears
on release, the Arrow Cam still works, and Traditional/Challenge hand
presentation is unchanged. This is not a claim of Unity runtime validation.

## Modern first-person viewmodel rig v2 (desktop A/B experiment)

The new **Modern first-person rig v2** separates the original gameplay
launch anchor from the cosmetic bow. Physics, target scoring, ring hit tests,
arrow speed, draw power, aiming and Arrow Cam still use the old gameplay
trajectory: camera eye + aim × 0.70m + right × 0.15m - up × 0.16m,
then launch from that point + aim × 0.25m. Toggling visual rig settings
must not alter this origin.

In the **JemparinganGame Inspector**, under
**Modern first-person rig v2 (desktop visual only)**:

| Control | Default | Purpose |
| --- | ---: | --- |
| modernViewmodelRigV2 | on | Opt-in per play session; V toggles v1/v2 in Modern |
| modernBowForwardDistance | 0.78 m | Eye-to-bow distance for visual geometry only |
| modernBowRightOffset | 0.20 m | Shift visual bow to the screen right |
| modernBowDownOffset | 0.18 m | Lower visual bow to keep the target visible |
| modernBowVisualScale | 0.94 | Relative size of cosmetic bow/string/preview arrow |
| modernViewmodelFov | 65° | Simulated viewmodel FOV through model scale only |
| modernRightHandScale | 0.92 | Right-hand size relative to the cosmetic bow |

**Important:** this is a lightweight *virtual viewmodel FOV*, achieved by
rescaling the bow with the tangent-of-half-FOV ratio. There is **no second
camera** and no new overlay rendering layer. The real scene camera FOV,
player eye transform and target/world projection remain unchanged. The
visual model still shares the main camera's near clip plane, so this is
not equivalent to a full dual-camera viewmodel setup.

### Required Play Mode checks

1. Enter **Modern**. Compare **V** (v2/v1) while looking at each target.
   Only the cosmetic bow and hands should move/resize; environment and
   target projections must not jump.
2. Draw at half and full power. Right-hand pinch stays near the actual
   visual nock and is smaller relative to the bow. The left grip stays
   attached to the leather wrap and the center of the target stays clear.
3. Fire from unchanged aim/power in v1 and v2. The preview dots,
   flight path, scores and impact locations should be unchanged (allow
   normal input variability); check shot origin on an instrumented test
   if numerical equality is required.
4. Test **V** mid-draw and after Arrow Cam returns. No stuck hands,
   camera lock, double projectile or visual carry-over.
5. Enter **Classic**, **Traditional** and **Challenge** after v2 Modern;
   the bow's transform scale must restore to 1.0, and their original
   positioning and Arrow Cam behavior must be unchanged.
6. Press **M**, **R**, **Esc**, and perform a Modern miss as additional
   camera and draw-state regression checks.
7. Check Game View at different resolutions/aspect ratios and test
   Inspector tuning. Unity compilation and Play Mode are **not yet run**
   for this GitHub-only code change.

## FPS bow pose pass — idle → drawing → full draw

Modern now uses a **three-state first-person presentation** when the
existing v2 viewmodel toggle is enabled. This is informed by readable FPS
archery framing (such as Far Cry 4) without copying art or animation from a
commercial game. It uses the existing Jemparingan procedural bow and hands.

- **Idle / lowered ready:** without LMB or RMB, the visual bow is an extra
  0.22m right, 0.22m down, and 0.08m forward from its v2 aiming offset.
  It is held at 90% of the tuned visual scale and canted slightly more.
- **Drawing / raising:** on LMB draw or RMB aim, the cosmetic bow eases
  upward and inward toward the aiming lane. The transition is driven by
  `modernPoseTransitionSpeed` (default 4.8 blend units per second).
  The existing bowstring and right-hand mesh continue to follow actual
  input draw progress.
- **Full draw / settle:** above roughly mid-draw, the bow moves another
  0.035m inward and 0.025m up and slightly straightens. The transition
  peaks near full draw; this only affects visual composition.
- **Release / return:** with LMB and RMB released, the visual bow eases
  back to the lower-ready pose. Arrow Cam retains its original behavior.

**Controls:** V still compares the original static v1 framing with the
new animated v2 framing. Inspector values
`modernIdleRightShift`, `modernIdleLowering`,
`modernIdleForwardShift`, `modernIdleBowScale`, and
`modernPoseTransitionSpeed` tune the idle-to-aim transition.

Crucially, the player's Main Camera/FOV, original gameplay bow anchor,
aim trajectory, shot timing, draw strength, projectile spawn/velocity,
scoring, targets and other game modes are unchanged. This is an
**animated viewmodel**, not a skeletal anatomical rig or dual-camera
render layer.

### Visual QA (Unity Play Mode required)

1. Enter Modern with v2 on, do not press LMB/RMB. Confirm bow is lowered
   and partially to the right, leaving all three target centers clear.
2. Hold RMB without LMB: bow should raise smoothly to the sightline and
   return to low-ready when released; **no shot** should fire.
3. Hold LMB: bow raises, right-hand fingers follow the string. At half and
   full draw, the bow settles slightly into the final aiming position.
4. Release LMB: one projectile per release, Arrow Cam still starts, and
   camera/look returns normally; bow lowers afterward.
5. While idle and at full draw, toggle V: old static view vs new animated
   view should be noticeably different without target/world camera movement.
6. Aim left/right/up/down, resize the Game View, and fire at each target:
   no obstruction, floating wrist, near-clip blowup, or altered hit results.
7. Retry (R), return to modes (M), switch Traditional/Challenge/Classic,
   and switch back. Their existing bows and hands must be unchanged.
8. Run Tools > Jemparingan > Run Gameplay QA v0.9 and inspect Console.

This code was checked statically through GitHub, **not** compiled or
rendered by the Unity Editor in this change.

## Validate in Unity

This document records a **test plan**, not a claim that Unity Play Mode
has already been run for this change.

1. Open `JemparinganPlayableEnvironment_v05`; enter Play Mode.
2. Press **4** or click **MODERN ARCHERY**.
3. Confirm three round targets render and the old hanging bandulan is hidden.
4. Aim at each target, hold LMB to draw and release. Look for a short
   impact marker, scoring feedback and an arrow embedded in the board.
5. Verify gold/red/blue/dark/white scores 10/8/6/4/2; off-board shots miss.
6. Fire all 12 shots. Confirm results, accuracy and best score persistence.
7. Use **R** to retry and **M** to change modes, even mid-round.
8. Switch to Classic, Traditional and both Challenge missions. Confirm
   original bandulan, scoring, arrow counts and Challenge Arrow Cam behavior.
9. Run **Tools > Jemparingan > Run Gameplay QA v0.9**.
10. Inspect Console for errors and warnings. Compare target framing and
    scoring feel before adding extra visual polish.

Known scope boundaries: no new dedicated stadium scene, leaderboard,
multiplayer or wave progression. Modern uses the existing desktop Arrow Cam,
without slow motion. Unity runtime,
physical input, graphics and balance **still require Play Mode validation**.

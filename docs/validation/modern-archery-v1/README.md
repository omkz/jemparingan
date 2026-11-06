# Modern Archery v1 — Playtest notes

Modern Archery is a fourth, experimental desktop mode in the existing
`JemparinganPlayableEnvironment_v05` scene. It reuses the current bow,
first-person camera/input, procedural environment, arrow pool and
gravity-based projectile. No external assets, scene regeneration, or
separate physics system are required.

## CURRENT: Modern left-hand-only viewmodel

Supersedes the previous two-hand implementation documented below. The
offscreen right hand, finger meshes, right sleeve, release animation and
per-frame updates are now removed from `JemparinganModernHands.cs`,
rather than only being hidden. The remaining viewmodel includes the
Modern left palm, four curled fingers, thumb, cuff, and bottom-left
deforming forearm, parented to the cosmetic bow grip.

The unused historic Modern right-hand prototype is also no longer
instantiated in `JemparinganTraditionalArcher`. Traditional/Challenge
retain the existing left grip hand and its presentation path. No code
for bowstring animation, nock, arrow trajectory, optical sight, LMB
input, scoring or Arrow Cam was changed. Fewer objects and less per-frame
work may help performance a little; measurable FPS claims require
profiling in Unity.

Unity QA: verify left hand stays on grip during idle/draw/full aim,
right hand never appears, optical plus remains clear, shots still hit
their intended boards, and V/R/M/Arrow Cam plus older modes still work.

**Status:** static source checks, not Unity compilation or Play Mode.

## CURRENT: completely new Modern FPS hands & release animation

This is **not** the old `JemparinganTraditionalArcher` reused in Modern.
A new `JemparinganModernHands.cs` is built separately under the cosmetic
bow hierarchy for **Modern desktop V2 only**:

- **Left gripping hand:** new tapered palm/wrist, four curled fingers
  around the leather grip, thumb, and compact cuff. It follows the grip
  directly as the -2-degree bow raises, zooms and aims.
- **Left forearm:** new smoothly interpolated 3D sleeve tube from the
  lower-left camera frame to the wrist; recomputes vertices rather
  than creating new GameObjects per frame.
- **Right drawing hand:** new tapered palm, separate pinching
  index/middle/thumb meshes. It tracks the *actual visual nock* every
  frame during LMB draw; no unrelated screen-space guess.
- **Right forearm:** separate tube enters from the lower-right frame
  and follows its wrist. Right hand/forearm are hidden while idle.
- **Release follow-through:** on LMB release, the fingers unfold and
  the right wrist moves away for a short ~0.18-second gesture. With
  the default Modern Arrow Cam active, both hands hide immediately
  when follow begins, preventing floating body parts in the camera.
- **Near-camera protection:** the right hand shifts minimally toward
  the bow if the nock sits too close to the near clip plane. This
  is visual-only; the string, release frame, and ballistic zeroing
  are never moved by it.
- **Old modes unchanged:** Classic/Traditional/Challenge keep the
  pre-existing Traditional first-person left hand and original
  gameplay. No existing hand meshes have been deleted.
- **Inspector:** `modernUseNewHands = true` controls ONLY the new
  Modern rig; `modernShowHands` is a retained legacy serialized
  setting and is not reused for Modern rendering.

### Required Unity Editor Play Mode tests (not yet performed)

1. Enter Modern. Left fingers must wrap the visible leather grip and
   sleeve enter from screen bottom-left without a visible cut end.
2. Hold LMB to full draw, aim to each board: right fingers should stay
   close to the nock and its sleeve come from bottom-right, without
   growing huge or obscuring the optical +.
3. Release LMB with `modernEveryShotArrowCam` disabled for one test:
   look for the short finger opening/wrist release gesture. Then
   re-enable Arrow Cam and confirm arms hide during follow.
4. Repeat with G calibration and hit center and noncenter rings:
   the successful ballistic impact must not change.
5. Press V (v1 view), R (retry), M (menu), Esc (Arrow Cam skip),
   and change modes. The new Modern mesh must not leak into older
   modes; all original input schemes must still work.
6. Check high/low camera pitch and different Game View ratios.
   If fingertips appear offset when the nock nears the clip plane,
   adjust only the **visual** near-plane allowance; do not move
   the gameplay launch origin or change camera aim.
7. No console errors, mesh leaks or recurring GameObject creation.
   Confirm frame rate is reasonable in WebGL.

**Status:** Source integration is complete. The Unity editor has not
compiled or visually playtested this new hand rig in this session.

## CURRENT: Archery Master-inspired optical bow sight, 2-degree cant

This revision **supersedes** the previous 6-degree Modern cant, and the
older tiny single-pin sight. The user's screenshot shows a sight attached
to the right-side bow, with a visible transparent lens and a centered **+**
reticle. It is **not** a large, floating screen-space HUD widget.

- **Modern bow:** `modernBowCantDegrees = -2` by default at full draw
  (almost vertical). Idle can lean a few extra degrees as the bow lowers.
  The camera-relative upper-tip height and grip landmark remain fitted.
  Traditional/Classic/Challenge rotations are unchanged.
- **Slightly larger optical sight:** aperture radius `0.0156m`,
  approximately **30%** larger than the prior `0.012m` rim. The
  smooth 24-segment ring remains attached to the cosmetic bow riser
  through one slim support rod. It is *not* a full-screen scope.
- **Lens:** a circular transparent mesh under the reticle, tinted
  very lightly blue (alpha `0.12`); it uses the scene pipeline's
  transparent blend setup, with a Standard/Sprites shader fallback.
  This is a cosmetic glass impression, **not** optical magnification.
- **Reticle:** a fine dark **plus (+)** visible through the ring, with
  a tiny contrasting center bead. No large HUD crosshair by default.
- **One-time calibration:** the bow-local reticle center is solved by
  intersecting the real camera sight ray with the fixed bow-local
  sight plane, after the draw and zoom finish settling. The reticle
  does **not** chase the camera afterward. Use **G** to compare the
  physical plus with the temporary HUD debug marker.
- **Unchanged:** LMB hold→draw→mouse aim→release, arrow-tip ballistic
  zeroing, target hit tests, scoring, Modern Arrow Cam and the other
  modes. Modern hands remain hidden until sight framing is approved.

### Local Unity Play Mode validation (not yet run)

1. Enter Modern. At full draw, the riser should look nearly upright,
   around -2°. Compare to the reference screenshot's vertical bow.
2. The ring must be only moderately bigger, with translucent glass,
   a clearly visible **+**, and no oversized or opaque blue disc.
3. Hold LMB to steady full draw and tap G: the physical plus should
   align with the small debug HUD crosshair. G only toggles debug.
4. Fire at all three boards, center and off-center. The visually
   calibrated reticle should continue to match actual arrow impact.
5. Change aim while holding LMB: optical sight remains rigidly
   attached to the bow rather than tracking camera pixels separately.
6. Confirm the lens renders properly in the project's URP/WebGL
   target; if the blue disc is invisible or opaque, inspect the
   URP material/render queue before changing gameplay physics.
7. Check V comparison, M mode menu, R retry, Esc Arrow Cam skip and
   Classic/Traditional/Challenge for leaked sights/zoom or changed
   controls. No hands are enabled in Modern during this pass.

**Verification scope:** only source/static checks; Unity Editor build,
Play Mode rendering, and user screenshot comparison remain pending.

## CURRENT: modern upright bow and simpler fixed sight

This **supersedes** the earlier large/dynamic sight implementation and
17° bow cant in the sections below.

- **Modern V2 bow:** the full-draw riser has a small **-6° camera-relative
  cant** (`modernBowCantDegrees`, Inspector range -12..+12). The idle
  pose relaxes a little further, but full draw is close to vertical.
  Traditional, Challenge and Classic rotations stay unchanged.
- **Same reliable reference framing:** the grip remains at the existing
  screenshot viewport landmark (`modernReferenceGripViewportX/Y`).
  The new projected size keeps the *vertical* upper-tip landmark while
  intentionally allowing upper-tip X to shift left as the bow straightens.
  This fixes the previous conflict of forcing the bow to reproduce an
  excessively slanted screenshot at the same time as using a modern sight.
- **Simpler physical sight:** one small metal bracket, **one thin support
  rod**, **one smaller ring**, **one little pin**. Removed the redundant
  jointed extension and frame-by-frame camera-facing ring rotation.
- **Fixed bow-local pin:** the sight calibrates its local position **once**
  after the first nearly settled full draw and zoom. It keeps the
  resulting bow-local transform during mouse movement and subsequent
  shots; it does **not** repeatedly chase the center of the camera.
  Calibration resets only on Modern round reset/entry and on V view
  comparison. Out-of-range calibration is rejected in favor of the
  small physically plausible resting placement.
- The **G debug HUD** remains off by default on entering Modern;
  enable it temporarily to compare the physical pin to screen center.
  **LMB hold/mouse aim/release**, ballistic zeroing, arrow speed/drop,
  scoring, and Arrow Cam have **not** been changed.
- Modern hands remain hidden during the visual calibration pass.
  The sight never becomes visible in other game modes.

### Unity Play Mode checks still needed

1. **Idle → full draw:** modern bow appears closer to upright; full
   draw should settle around -6° rather than the former prominent lean.
2. **Sight geometry:** single slender support touches the riser;
   small ring and fiber pin are visible, without large multi-rod hardware.
3. **Press G at stable full draw:** the pin and HUD crosshair should
   overlap after zoom is fully settled. Move mouse while holding LMB:
   pin stays in its fixed place on the bow, not sliding to chase the HUD.
4. **Fire at center and outer rings of all three targets:** the
   previously verified impact/crosshair relationship must remain intact.
5. **V old/new view**, M menu, R reset, Esc Arrow Cam skip, and
   Classic/Traditional/Challenge: no sight leak, no extra shots, no
   persistent FOV zoom.
6. Repeat at different aspect ratios. The pin is sighted in again on
   entering Modern; do not claim stable alignment across mid-round
   resolution/FOV changes without further testing.

**Verification:** GitHub code and static math checks, not Unity runtime
compilation or visual inspection.

## CURRENT: bow-mounted physical sight (no default HUD crosshair)

The successful one-button LMB aiming and ballistic zeroing are kept
**unchanged**. The next step is visual: a 3D recurve-style **aperture ring
with a small fiber-style pin**, mounted on the Modern cosmetic bow.

- **Physical model:** new `JemparinganModernBowSight.cs`, attached to the
  bow's own transform. A brass mounting plate near the upper grip connects
  via an adjustable metal arm to a small circular aperture and its pin.
  It is genuine Unity 3D geometry with no `OnGUI` reticle, no collider and
  no new imported assets.
- **Calibrated at stable full draw:** only after the one-button
  precision pose is almost settled, place the pin at the world point
  where the real camera forward axis crosses the bow-depth plane.
  Convert that point into **bow-local coordinates**; the bracket remains
  visibly connected to the bow, and adapts to the camera FOV/viewmodel
  scaling rather than drifting when zooming. The physical sight is
  visible in lowered/drawing states too and follows the bow geometry.
- **HUD crosshair off by default** on entering Modern. Press **G**
  during full draw to turn on the original camera-axis debug mark to
  compare the physical pin with the screen center. G does not alter aim
  or shot physics. Turn G off to return to sight-only gameplay.
- **Modern V2 only:** the sight is hidden if **V** selects the old static
  view, or `modernUseReferenceFraming` is unchecked. Other modes do
  not render a Modern sight. Inspector toggle `modernUsePhysicalSight`
  allows quick bow-only comparison.
- **Hands stay hidden** via `modernShowHands = false`. Rebuild them
  separately once the sight and bow composition are visually approved.
- Ballistics, target hit tests, scoring, Arrow Cam, input and zoom are
  unchanged by this visual feature.

### Physical-sight Play Mode checklist

1. Start Modern, hold LMB until full draw. Check the **3D ring/pin is
   attached to the riser** and moves with the bow through the draw.
2. At full draw, press **G**. The center of the **physical pin** should
   overlap the small debug HUD crosshair after the aim pose settles.
   Toggle G off and shoot the middle board: the arrow should still hit
   the marked ring as in the previous accurate version.
3. Aim left/right/up/down and shoot all three targets. Check physical
   sight stays visible and attached, does not noticeably drift, and
   its ring does not block the entire target.
4. Change Game View aspect ratio and test again; the pin anchor uses
   the actual camera FOV and perspective, not a fixed pixel offset.
5. While holding LMB, press **V** to switch old/new view models. New
   sight should disappear with V1, and return with V2. Release LMB
   exactly one shot should fire.
6. Trigger Arrow Cam, Esc skip, R retry, M modes, and switch to Classic,
   Traditional, Challenge. No extra sight or HUD marker should leak
   into those modes; FOV and input restoration remain intact.
7. Leave **hands disabled** for this round. The old procedural hands
   are retained for Traditional/Challenge and are **not** the new
   Modern hand rig.

**Verification state:** source/static inspection only. This is not a
claim of successful Unity compilation or Play Mode screenshots.

## CURRENT FIX: sight reticle vs real arrow impact

The full-draw one-button LMB control is unchanged. The earlier HUD
debug crosshair marked the camera axis, but the actual launch started
from a different position and fell under gravity; misses below the
crosshair were therefore expected. This fix **zeros** the crosshair to
the actual target face instead of pretending a camera-axis marker is
already an accurate bow sight.

### Modern desktop only

1. Cast a ray along the center of the actual player camera.
2. Intersect it with the real front plane of each of the three Modern
   boards (`JemparinganModernTarget.FaceDepth`). Choose the nearest
   board directly under the reticle, or use a nearby plane for range
   estimation if the player deliberately aims off the board. Never
   snap toward a bullseye or an automatically chosen score ring.
3. Compute the **fixed-speed low-arc ballistic initial velocity** to
   the exact ray/plane intersection point, including launch-origin
   parallax, `simulatedGravity`, and the **0.36m arrow-tip offset**
   that the current swept-tip collision algorithm actually tests.
4. Use the same calculation for held-draw trajectory preview and for
   **release-frame** launch. The latter must use `_lastDraw` (stored
   draw power), not `distance` (which immediately resets to zero
   when LMB is released). The existing shot speed and gravity still
   determine reachability and the arc.
5. The debug crosshair says **ZEROED TO TARGET FACE** when the
   reticle is on a target and the ballistic solution is reachable.
   Otherwise it says **AXIS ONLY - NO TARGET LOCK**. Toggling it with
   **G** never changes the shot. This is range zeroing, **not homing**.
6. If a target is physically unreachable at the current draw speed,
   keep the legacy unassisted launch and do not show a misleading
   'zeroed' status. All non-Modern modes keep their existing
   trajectory/assistance and control scheme.

The ballistic method solves for a flight time `t` at fixed speed
`s` by using `u=t²`:

`(g²/4)u² + (g*deltaY - s²)u + dot(delta,delta) = 0`

The shorter positive root yields the lower trajectory. Iteratively
subtract the **actual velocity-direction arrow-tip offset** at impact
to align the collision point, not merely the arrow root.

### Test results and remaining QA

A stand-alone numeric parity test of this equation against the
discrete 60 FPS swept-tip update covered **18 reachable combinations**
of 8m/12m/16m target ranges, 7.5/11m/s speeds, and center/off-center
aim points. All landed within approximately **0.01cm** in the
idealized no-camera-rotation test. This is a math test, **not proof of
Unity-game alignment**, especially with authored scene transforms,
real camera position, different display frame rates or an occluding
foreground object.

**Local Unity Play Mode verification required:**

- Full draw on target 1/2/3 center and fire: ring impact should
  coincide with the HUD crosshair's intended plane point.
- Aim deliberately at the red/blue/outer ring: impact must remain
  near that off-center point, **not snap to center**.
- Release early at reduced power, and draw fully: solver must
  use the release-frame stored power and preserve shot count.
- Swing mouse quickly while holding LMB: trajectory preview and
  release frame should use current aim without stale aim values.
- Test with the camera off any board: release should miss naturally.
- Verify Arrow Cam, Esc skip, G toggle, V comparison, R retry,
  M modes, Classic/Traditional/Challenge, and no new console errors.
- If target is unreachable, the reticle must not falsely claim
  ballistic accuracy.

**Unity compilation and Play Mode testing have not been executed.**

## CURRENT: Archery Master-style one-button controls (Modern desktop)

This update **supersedes the earlier RMB full-draw aim experiment below**.
Modern now uses a single held **LMB** for quick drawing, mouse look
and sight refinement. **RMB is not required for Modern.**

- **Hold LMB:** the existing nock/string pulls smoothly to full power
  over **0.28 seconds** by default (Inspector:
  `DesktopBowInput.modernFullDrawSeconds`). Mouse movement during the
  hold turns the first-person camera. This supports the familiar
  click → reach full draw → hold/move mouse → release flow.
- **After near-full draw (96% power by default):** the cosmetic bow
  transitions automatically into the screenshot-calibrated precision
  pose and the camera smoothly zooms toward **60° FOV**. No RMB press
  is required. Before that, the bow is raised but not yet in the
  final calibrated sight pose.
- **Release LMB:** fire using the original shot origin, power-to-speed
  curve, gravity, collisions, scoring and optional Arrow Cam. Releasing
  early can still yield a weaker shot under the existing draw rules.
- **Shift + LMB mouse movement:** finer aiming at the existing outdoor
  precision multiplier (the Modern mode uses this regardless of the
  old `outdoorBowPresentation` Inspector flag).
- **G:** toggle the camera-axis **debug crosshair** (visible only
  while full-draw aiming, when V2 screenshot framing is enabled).
  It does **not** predict the ballistically affected impact.
- **V:** compare V1 static viewmodel framing against the V2
  screenshot-calibrated pose. **One-button mouse controls remain the
  same in both views**.
- **Hands remain hidden:** `modernShowHands = false` by default.
  Both hands are not deleted; Traditional/Challenge are untouched.

**Other modes:** Classic, Traditional and Challenge keep their original
**RMB camera look**, **LMB one-second draw**, and LMB release to shoot.
Quest input, target/scoring rules, and the real projectile have not
been modified by this update.

### One-button Unity Play Mode QA (not yet executed)

1. Enter Modern, hold **RMB alone**: there must be no aim zoom or
   Modern mouse-look activation. Move mouse while **LMB is held**:
   the camera should turn immediately.
2. Hold **LMB** through the ~0.28s draw. The bow raises, then settles
   to the screenshot pose when its draw reaches 96%; zoom begins.
   Mouse movement remains responsive while held.
3. Still holding LMB, move mouse over each target; add **Shift**
   for fine aim. Release LMB: exactly **one** projectile fires and
   Arrow Cam follows it. Ensure there is no second shot.
4. Toggle **G** mid-hold and check camera-axis marker visibility.
   Turn V1 on with **V**, verify one-button aiming still works.
5. Release before full draw: only pre-existing minimum draw/hold
   rules apply; a quick tap must not accidentally become a full shot.
6. Press **R** while holding LMB, exit with **M**, enter Classic,
   Traditional and Challenge: no extra shots, no stuck camera,
   no residual zoom, and RMB-only camera look is restored.
7. On startup with initialOutdoorMode=Modern, the mode selection
   menu must not activate the Modern mouse controls until selection.
8. Repeat after the Arrow Cam exit/skip with Esc; look must resume
   only when another LMB draw is started.

**Validation boundary:** source-level checks only. Unity compilation,
camera motion and actual screenshot matching require editor Play Mode.

## Current experiment — Modern full draw → RMB precision aim

**This is the current behavior.** Sections below describe the history of
earlier visual iterations and may contain obsolete instructions. Unity
Play Mode has **not yet been run** for the changes in this section.

**Controls (Modern desktop, screenshot-framing v2 enabled):**

1. **Hold LMB:** the bow raises and its string/arrow draw progresses.
   This does **not** zoom the camera.
2. **Draw at least 96%** of the playable power span, then **hold RMB**:
   smoothly transition from a pre-aim bow pose to the calibrated sight
   pose, and gradually zoom the Main Camera (typically 65° → 60° FOV).
   Holding RMB earlier is allowed for look input, but zoom/final pose
   activate only after full draw is reached.
3. **Move mouse while RMB is held:** refine the existing camera aim
   (Shift+RMB remains fine aiming). **Release LMB** to shoot exactly
   once with existing draw power/shot math, then Arrow Cam follows as
   before. LMB may still fire a lower-power shot before full draw; it
   just does not unlock precision aiming.
4. **G:** toggle a small temporary **camera-axis debug crosshair**,
   visible **only** during full-draw RMB precision aiming. It does
   **not** compensate arrow drop, and is **not** a physical bow sight.
5. **V:** toggle old static view (v1) and the calibrated animated v2.

**Hands remain off** by default in Modern via
`modernShowHands = false`. The procedural hands are retained for
Traditional and Challenge. No hand meshes/assets were deleted.

**Why no physical sight pin yet?** We first need to verify the
camera-axis marker against the actual falling projectile and measure
the sight offset at target distance. Placing a sight pin now could make
the visual aim appear correct while actual ring hits remain offset.

Inspector settings added in `JemparinganGame`:
`modernPrecisionDrawThreshold` (0.96),
`modernPreAimRightShift` (0.09m),
`modernPreAimLowering` (0.055m),
`modernPrecisionPoseSpeed` (7 blend units/s),
`modernDebugCameraCrosshair` (on),
`modernReferenceAimFov` (60°).

### Play Mode acceptance checklist

1. Select Modern. Hold **RMB only**: the camera may turn, but there
   must be **no zoom**, final aim pose or debug crosshair.
2. Hold **LMB only**, draw up to full power: bow raises and string
   moves; **no zoom** or debug crosshair.
3. While holding full LMB draw, hold **RMB**: the bow should settle
   inward/up into the target-calibrated pose, and FOV should smoothly
   change from 65° (or scene baseline) toward 60°. The debug center
   crosshair becomes visible when enabled.
4. Release **RMB** but keep full LMB draw: zoom and debug crosshair
   disappear; the bow returns smoothly to its pre-aim pose. Re-hold
   RMB and press **G** twice to test the marker toggle.
5. Release LMB: exactly **one** projectile launches and Arrow Cam
   follows it, without leaving the FOV zoom applied. **Esc** skips.
6. **R** mid-draw and **M** while aiming: FOV and input restore,
   and no release-after-reset spurious shot.
7. Compare **V** old/new views. Enter Classic, Traditional and Challenge
   and verify no camera zoom, no new HUD marker and original hands.
8. At different target distances, confirm whether the camera-axis
   marker differs from real impact due to gravity. That offset is
   intentional pending a separately calibrated physical sight pin.
9. Test with a narrow custom scene FOV: aim must not zoom out.

**Verification boundary:** GitHub source checks only; Unity compilation,
runtime controls and visual matching require the user's local editor.

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

## Screenshot calibration v3 — **current Modern desktop experiment**

This supersedes the older fixed-offset / 1.16x screenshot framing
documented below. The screenshot supplied by the player is a **full-draw
gameplay view**, 1493 × 893 pixels, not a marketing-only camera angle.
The estimated **bow grip center** is around (827,652) measured from
top-left; its Unity viewport target is (0.554,0.270). The visible upper
limb continues above the frame, so we use an **extrapolated upper-tip
landmark** of (1082,-67) or viewport (0.725,1.075). These are
calibration estimates, not certified pixel-perfect measurements.

### Projection-derived rig

For a perspective camera with vertical FOV `f`, aspect ratio `a` and
chosen grip depth `d`:

- `kY = 2*tan(f/2)`, `kX = a*kY`
- Model upper-tip local coordinate is `t=(tx,ty,tz)`.
- For each axis `i` (X/Y), set
  `Ai = (tipViewport_i - gripViewport_i)*ki*d`, and
  `Bi = (tipViewport_i - 0.5)*ki*tz`.
- With `q=1/visualScale`, solve the positive root:
  `(Ax²+Ay²)q² + 2(AxBx+AyBy)q + (Bx²+By² - tx²-ty²) = 0`.
- Recover `scale = 1/q`, then
  `roll = atan2(Ay/scale+By, Ax/scale+Bx) - atan2(ty,tx)`.
- Place grip with `Camera.ViewportToWorldPoint((gripX,gripY,d))`
  and rotate the bow relative to the camera by the solved roll.

This **simultaneously fits the projected grip AND upper limb**, unlike
the previous v2 which only fit the grip while leaving cant/size fixed.
It is re-solved every frame if FOV or aspect ratio changes. The
computed scale and roll, not a guessed scale multiplier, are applied
to the **cosmetic** bow. Default FOV during LMB or RMB aim is **60°**
(from the scene's original FOV, typically 65°), a modest real camera
zoom: approximate size magnification at 65→60 is ~1.10x.

For the provided screenshot aspect ratio, the projection calculation
at 60° yields ~1.19 visual scale and ~-17° roll, with near-zero
reprojection error for the two chosen reference landmarks.

### Isolating bow-only framing

- **`modernShowHands = false` by default:** all first-person arm,
  sleeve, grip hand, and right draw-hand meshes are hidden in
  **Modern only**. Keep this off while calibrating; other game modes'
  hand presentation is untouched. The physical bow, string and
  cosmetic nocked arrow remain visible.
- `modernReferenceGripViewportX` / `Y` (0.554 / 0.270)
  control the bow grip's fixed screen anchor; `Depth` (0.70m)
  controls how perspective changes along the shaft.
- `modernReferenceTipViewportX` / `Y` (0.725 / 1.075)
  control cant and apparent bow length at full draw.
- `modernReferenceAimFov` (60°) controls subtle whole-scene
  zoom during RMB aim or LMB draw. Camera FOV returns to its
  original value when leaving Modern or releasing aim.
- V keeps the old v1 framing for A/B comparison, **but** hands are
  hidden in both versions while `modernShowHands` is false.

### Unity acceptance tests (not yet executed)

1. Select Modern, hold LMB to full draw, compare the **bow only** with
   the supplied screenshot. The grip should be around 55% width,
   27% height from bottom, and the upper limb should leave the top
   of frame near 72–73% width.
2. Release LMB, hold RMB to test slight zoom (e.g. 65°→60°) and the
   idle-to-aim pose transition. Release RMB and verify zoom resets.
3. Switch V on/off while aiming. V1 must preserve gameplay behavior;
   V2 should have visibly different size, tilt, and screen composition.
4. Exit to mode select (M) and enter Classic, Traditional, Challenge.
   Check the scene camera FOV is restored, and arms still appear there.
5. Test at 16:9 and 4:3 Game View aspect ratios: two projected screen
   landmark positions should remain stable; inspect for near clipping.
6. Fire, miss, retry, and wait for/skip Arrow Cam. Confirm real arrow
   physics, score, launch origin, input and camera recovery are intact.
7. Enable `modernShowHands` **only later** once the bow-only pose is
   accepted. It uses the older procedural arms and may need a separate
   animation pass.

**Validation:** static code inspection and a separate numeric reprojection
test only. No Unity Editor build, no Unity Play Mode, no screenshot
pixel comparison of the **new rendered game** was performed.

## Archery Master gameplay-screenshot composition (Modern desktop v2)

The Modern v2 full-draw frame now uses the user's **actual Archery Master
gameplay screenshot** as its composition reference, not a speculative
marketing pose. It deliberately differs from the earlier small +5cm horizontal
offset:

- **Full-draw bow grip** targets normalized camera-viewport position
  **(0.56, 0.29)** at **0.70m depth** (bottom-left origin) to keep the
  target visible while the bow occupies the right foreground.
- **Bow size** has a **1.16x screenshot boost** on top of the previous v2
  visual scale/FOV setting; the upper limb can be cropped by the top edge.
- **Left forearm** enters diagonally from the bottom-left, becomes fuller
  near the screen edge, and tapers into the actual leather-grip hand.
- **Idle / RMB aim / LMB full draw** animations still operate. At full draw,
  the viewport grip anchor is honored without the extra offset applied by
  the older v2 full-draw settle.
- **Modern only:** Traditional/Challenge keep the existing small,
  screen-edge forearm, and Classic remains untouched.
- **V** compares the screenshot-framed, animated v2 against the old static
  v1. Use Inspector toggle **modernUseReferenceFraming** to return to the
  previous v2 preset independently.

Inspector tuning:
`modernReferenceGripViewportX` (0.56),
`modernReferenceGripViewportY` (0.29),
`modernReferenceGripDepth` (0.70m),
`modernReferenceBowScaleBoost` (1.16).
The ViewportPoint method uses the real Main Camera's vertical FOV/aspect
ratio. It does **not** instantiate another camera.

### Screenshot-specific QA before accepting the change

1. In Modern, hold LMB until draw is at 100%. Compare screenshot-to-game:
   large bow on the right, upper limb near/crossing top, open target area,
   visible diagonal left sleeve connected to grip.
2. Release, hold RMB and compare aiming. The busur should rise smoothly
   from low-ready; release RMB and confirm it lowers.
3. Toggle V while idle and while LMB is held. V1 and reference V2 should
   have a clearly different silhouette, not just a slight right shift.
4. Check right-hand visibility/proportion at half and full draw, clipping
   at all screen edges, and whether the sightline feels natural.
5. Repeat at 16:9 and another Game View aspect ratio. Screenshot reference
   uses normalized viewport positions, but arm and bow clipping can differ.
6. Verify one shot per LMB release, original launch origin and velocity,
   unchanged target hits, score, physics, Arrow Cam and reset behavior.
7. Switch to Classic, Traditional and Challenge. Their forearm silhouette,
   projectile behavior, camera and bow scale must be unchanged.
8. If this framing is uncomfortable, toggle **V** (old static baseline) or
   uncheck **modernUseReferenceFraming** (previous v2). Tune the viewport
   anchor in Inspector without modifying physics.

**Limitation:** the visible bow and shaft are cosmetic and already differ
from the original gameplay launch origin. The screenshot pass intentionally
leaves projectile mechanics intact; inspect release-frame alignment in
Unity before further tightening the cosmetic-to-physical transition.

**Validation state:** GitHub source inspection and static checks only.
Unity compilation, Play Mode screenshots and gameplay tests must be
performed in the editor.

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

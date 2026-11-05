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
- Challenge Arrow Cam remains Challenge-only

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
- Arrow Cam remains **Challenge-only**.

### Specific regression playtest

1. Start Modern and inspect all three targets. No authored bandulan crossbar,
   uprights, hanging cord or target should be in the sightline.
2. Hit rings on each board. Confirm +2/4/6/8/10 score popups, subtle wobble,
   small sparks, and the unchanged accumulated score.
3. Miss deliberately: no score popup or target recoil.
4. Fire multiple times rapidly: no duplicate lingering sparks, camera jump,
   or unintended Arrow Cam.
5. Press M and enter Classic, Traditional, and Challenge in turn: the
   authored frame is restored exactly, including when Modern is exited mid-round.
6. Return to Modern, retry, then switch back: no pooled arrow trail leak.
7. Run Unity Play Mode and gameplay QA; verify Console. These checks
   have **not** been executed by this GitHub-only code change.

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
multiplayer, wave progression or modern Arrow Cam in v1. Unity runtime,
physical input, graphics and balance **still require Play Mode validation**.

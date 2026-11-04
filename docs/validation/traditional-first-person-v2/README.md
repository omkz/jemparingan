# Traditional first-person aiming v2 validation

Validated in the existing `JemparinganPlayableEnvironment_v05` scene with
Unity 6000.2.14f1 through the connected Unity Pipeline MCP.

## Root causes

- The old presentation solved two full procedural arms every frame even though
  the right arm occupied the aiming lane and was not required for comfortable
  desktop play.
- The right-hand target added a camera-space face correction after calculating
  a bow-local nock. The string bead, arrow and hand therefore did not share one
  authoritative attachment point.
- Long first-person sleeves attempted to connect world-space hands to a
  seat-local torso. Camera rotation made that hidden full-body IK visually
  unstable and produced large, disconnected-looking limbs at the screen edge.
- The 20-degree bow cant and previous horizontal placement made the bow/hand
  silhouette broader than necessary around the bandulan.

## Result

- Traditional Mode now renders only the useful left grip hand and a short,
  continuous partial forearm. The hand is attached to the real leather grip.
- The string bead, both string halves and cosmetic nocked arrow use the same
  bow-local nock coordinate throughout rest, draw and release.
- The right-hand/arm presentation and its per-frame two-bone IK are removed.
- The gandewa is slightly farther left and canted 15 degrees. Cosmetic framing
  remains independent of launch direction and projectile physics.
- Classic Mode does not execute or display the Traditional presentation.

## Game View evidence

| State | Before | After |
| --- | --- | --- |
| Idle | [before-idle.png](before-idle.png) | [after-idle.png](after-idle.png) |
| Half draw | [before-half-draw.png](before-half-draw.png) | [after-half-draw.png](after-half-draw.png) |
| Full draw | [before-full-draw.png](before-full-draw.png) | [after-full-draw.png](after-full-draw.png) |

Additional final captures: [release](after-release.png),
[look left](after-look-left.png), [look right](after-look-right.png), and
[Classic regression](after-classic.png).

## Unity MCP checks

- Idle, half draw, full draw, release, yaw left and yaw right were captured
  from the actual Game View.
- Runtime attachment checks at every pose: left grip error `0.0 m`, arrow/nock
  error `0.0 m`, visible forearm length about `0.318 m`, right-side
  presentation renderer count `0`.
- A production pooled projectile miss completed normally. A production pooled
  projectile aimed at molo produced `shots=1`, `hits=1`, `score=3`, `molo=1`.
- Traditional snapshot: 12 arrows, stationary target, Traditional body visible.
- Classic snapshot: 16 arrows, Traditional body hidden, Classic target visible,
  target sway `0.06` at speed `0.7`.
- `Tools/Jemparingan/Run Gameplay QA v0.9`: passed all 39 deterministic
  rule/hitbox checks.
- Final Unity Console: 0 errors, 0 warnings; compilation not failed.

## Remaining limitations

- The visible forearm is a deliberately compact low-poly first-person mesh,
  not a full rigged character arm. This avoids hidden IK work and obstruction,
  but wrist deformation remains stylized at extreme camera angles.
- The validation draw poses use an in-memory test input adapter because Linux
  XTest mouse holds are not reported by Unity's legacy Input API. The adapter
  feeds the normal `IJemparinganInput` update path; production bow, string,
  nocked arrow and presentation code are the same code shown in the captures.
- No Quest headset performance or hand-tracking claim is made by this desktop
  Editor validation.

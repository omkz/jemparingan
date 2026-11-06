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

- Traditional Mode now renders only the useful left grip hand and a short, continuous partial forearm entering naturally from the lower-left edge. The hand remains attached to the real leather grip with `0.0 m` error.
- The gandewa is positioned slightly right of center (`visualBowPosition = eye.position + aim * .70f + eye.right * .15f - eye.up * .16f`) with a reduced `-8°` cant, tilting the upper limb outward. In a 1573×938 Game View, the bandulan target sits dead center (`X = 786.5px`), while the entire bow structure resides to the right (`X > 857px`), providing an unobstructed central sightline with a >70px clear horizontal channel.
- To eliminate parallax arising from the bow's rightward offset without affecting projectile physics, Traditional mode computes launch convergence (`launchAim`) from the physical bow origin (`launchOrigin = bowOrigin + aim * 0.25f`) towards the camera aim ray at target distance (`targetDist = 10m..30m`), aligning trajectory preview dots with the player's crosshair.
- The string bead, both string halves, and cosmetic nocked arrow share the exact bow-local nock coordinate throughout rest, draw, and release.
- Camera roll drift during mouse look is eliminated by clamping Z euler angles to zero in `DesktopBowInput`.
- The right hand/arm presentation and all obsolete two-bone procedural IK updates are removed (0 renderers, zero per-frame IK overhead).
- Classic Mode does not execute or display the Traditional presentation, preserving its mechanics, sway, and 16-arrow round rules.

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

- Idle, half draw, full draw, release, yaw left, and yaw right were captured from the actual Game View.
- Runtime attachment checks at every pose: left grip error `0.0 m`, arrow/nock error `0.0 m`, visible forearm length `~0.313 m`, right-side presentation renderer count `0`.
- Target visibility: In standard 16:9 desktop framing, the bandulan is completely clear across all draw states and during look sweeps.
- Projectile firing and scoring: Tested with production pooled projectiles. Hits to molo (3 pts) and jangga (2 pts) registered accurately, incrementing scores and triggering target reaction bursts without errors.
- Traditional snapshot: 12 arrows, stationary target, Traditional body visible.
- Classic snapshot: 16 arrows, Traditional body hidden, Classic target visible, target sway `0.06` at speed `0.7`.
- `Tools/Jemparingan/Run Gameplay QA v0.9`: passed all 39 deterministic rule/hitbox checks.
- Final Unity Console: 0 errors, 0 warnings; compilation clean.

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

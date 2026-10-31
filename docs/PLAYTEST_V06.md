# JEMPARINGAN — RASA / v0.6 Complete Game Loop — Playtest

**Status:** code pushed, **Unity compilation and Play Mode not yet verified**. Quest hand input remains a future integration and has not been tested.

## Open
1. `git pull origin main`; open with Unity version pinned in `ProjectSettings/ProjectVersion.txt`.
2. Open the **existing** `Assets/Jemparingan/Scenes/JemparinganOptimized_v02.unity`. Gameplay UI is built at runtime; do **not** regenerate the scene.
3. Clear Unity Console before Play. Run `Tools > Jemparingan > Run Target Hitbox Checks` in Edit mode too.

## Test cases

| Case | Steps | Expected |
| --- | --- | --- |
| Intro | Press Play | Small in-world introduction shows 12 shots / 3 stages / RASA bonus. HUD and target still visible. |
| Auto-start | Hold left mouse for a proper draw, then release | Intro hides as drawing begins. One arrow is fired; no separate Start button or menu required. |
| Too-short draw | Tap left mouse very briefly | Does not fire or consume arrow; feedback is shown. |
| Stage 1 | Fire shots 1–4 | Phase 1 visible; four progress marks advance. |
| Stage 2 | Fire shot 4, then 5 | Brief BABAK II banner appears after shot 4; moving target difficulty changes; play continues. |
| Stage 3 | Fire shot 8, then 9 | Brief BABAK III banner appears after shot 8; no blocking modal dialog. |
| Score | Get a hit, a bullseye, a stable hit, combo and miss | HUD points, accuracy basis and combo match visible outcomes. |
| End screen | Complete 12 shots and allow final arrow to resolve | Result card shows score, hit accuracy, number of central hits, best combo, successful RASA bonus points. |
| Record | Beat an existing saved best score, then complete a lower-scoring round | First result says REKOR BARU; second does not. R resets should not save incomplete rounds as records. |
| Replay via keyboard | At results, press R | New 12-shot round, cleared arrows/effects, intro visible, stats reset. |
| Replay via gameplay input | At results, release LMB, then hold LMB for at least 0.7 seconds | New round; while still holding, should not accidentally fire. Release before drawing again. |
| Mid-round restart | Press R after at least one shot | Round resets cleanly; no ghost arrows, progress or results panel. |
| Repeated rounds | Complete 3 rounds in a single Play Mode session | No duplicate UI panels, score overflow, broken stage transitions, or stuck input. |

## Things to pay attention to
- Result text must not overlap the stage/HUD badly when seen from the default camera. If it does, tune `BuildSessionPanels` positions and `TextMesh` character sizes.
- The **gesture replay** uses the existing `IJemparinganInput` interface and is designed for future hands-only use, but still needs on-device Quest validation.
- Result stats are collected on successful hits; the RASA bonus counts *only successful stable hits* (3 points each).
- Observe whether fire/release is blocked appropriately during final-shot resolution and whether hit feedback gets overwritten.
- Target hit radius and difficulty are unchanged from v0.5.

## Exit gate

Check every case in Unity Editor. **Do not label v0.6 runtime-tested, Quest-ready, or competition-ready based only on repository updates.** If errors occur, capture the **first red Console error and stack trace**; fix blockers before adding new assets.

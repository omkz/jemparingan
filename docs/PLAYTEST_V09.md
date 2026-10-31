# JEMPARINGAN — RASA / v0.9 Balancing & Regression Gate

**Implementation:** in `main`. **Verification:** not yet compiled or run in Unity Editor or Quest hardware by ChatGPT. This is a test plan, not evidence the game passed.

## Why this milestone matters

v0.9 is a **stabilization pass**, not a new-feature release. It hardens existing v0.5–v0.8 gameplay:

- Pool capacity is at least the number of arrows per round. Previously the Inspector permitted up to 30 arrows but the pool only held 14.
- Pressing `R` while holding the string now requires releasing the string again before a new draw can shoot.
- Arrow movement and the preview dots now use the **same constant-gravity position equation**.
- Pure rules for scoring, accuracy, stage boundaries, result titles, pool capacity, and trajectory are shared by the running game and Unity Editor checks.

## Step 1 — Quick Editor QA (21 deterministic checks)

1. `git pull origin main`.
2. Open the repository project in **Unity 6000.2.14f1**. Clear Unity Console.
3. Without entering Play Mode, run **Tools > Jemparingan > Run Gameplay QA v0.9**.
4. Expect **`Jemparingan gameplay QA v0.9 PASSED (21 deterministic rule/hitbox checks)`** with no red Console errors.

These checks cover:
- Scoring: standard hit, bullseye, stable bonus, three-hit combo, and all bonuses combined.
- Accuracy: empty round and 9/12 hits.
- Stage thresholds: before 4, at 4, at 8, and stage labels.
- Result-title selection based on accuracy and composure.
- Pool size for 12-shot and 30-shot configurations.
- Projectile/preview trajectory for no gravity, 1-second gravity fall, and t=0.
- Target collision: central hit, ordinary hit, and miss.

**Limit:** Pure-rule checks do **not** simulate input, audio, tracking, moving objects, rendering or actual rounds. All require Play Mode verification.

## Step 2 — Play Mode regression (3 full rounds)

Open the existing `Assets/Jemparingan/Scenes/JemparinganOptimized_v02.unity` and press Play (do not regenerate the scene).

| Round | Test | Pass criteria |
| --- | --- | --- |
| **A: Normal** | Shoot exactly 12 arrows, including misses and hits. | Score, 3 stages (shots 1–4, 5–8, 9–12), feedback, end results and restart function. No 13th shot. |
| **B: RASA** | Test quick shot without bonus, then hold steady to full focus before hit. | Fast shot is allowed with no +3; successful RASA hit awards +3; miss never awards it. |
| **C: Stress** | Press R mid-draw (keep LMB held), fire repeated shots, finish and replay by gesture. | No arrow fires when releasing after R until a new draw begins; no ghost trails, errors, stuck UI or double releases. |

For every round, verify:
- Intro dismisses on first draw; result panel appears only after final arrow resolves.
- Shot progress and result stats are correct, including record persistence.
- Audio ambience is subtle, hit/bullseye/miss distinct, arrow trails disappear on reuse.
- Stage II/III moving bandulan and optional lead hint remain readable.
- No red errors/exceptions in Unity Console.

## Step 3 — Configuration edge cases

With the scene **not playing**, select `Jemparingan Game` and temporarily set **Arrows Per Round** to 20 (or 30), then test a new round. The pool must hold at least that number of unique active projectile instances and the game must not recycle a flying arrow.

**Scope limitation:** Stages currently change only at shots 4 and 8; a 20–30 shot round is a stress case, *not* the intended game design. Restore the default **12** for the actual game. Test 30 only as a memory/capacity regression check; it is not required for the three 12-shot rounds.

Try **Ambient Wind Volume** at 0: only background sound disappears, not normal hit/release audio.

## Step 4 — Measure, don't guess

Capture from the running Unity Editor:
- First red Console error + stack trace (if any).
- Average frame time and any visible hitch while playing 12 shots.
- Short screen recording of one complete round and the final result screen.
- Count of accurate hits by a first-time tester and whether RASA feedback is understandable.

**Editor FPS is not Quest FPS.** We cannot claim 60 FPS on Quest 3/3S without a measured device run.

## GO / NO-GO

**GO to Quest integration:** all 21 checks pass, three full 12-shot rounds pass in Editor, no crashes/compile errors, replay and tracking-loss-safe input boundaries hold in desktop simulation.

**NO-GO:** any red compilation error, incorrect scoring, unintended shot on restart, unfinishable round, stuck result screen, or reused flying arrows. Fix those before any further visual/audio features.

Remaining hardware gate: **real Quest hands-only aiming/pinch/release, no controller dependence, seated comfort, passthrough, Android APK, device frame timing**.

Related regression plans: [v0.5](PLAYTEST_V05.md) · [v0.6](PLAYTEST_V06.md) · [v0.7](PLAYTEST_V07.md) · [v0.8](PLAYTEST_V08.md).

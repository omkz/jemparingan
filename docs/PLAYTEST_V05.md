# JEMPARINGAN — RASA / v0.5 Gameplay Playtest

**Status:** implementation pushed to GitHub. **Not yet compiled or tested in Unity Editor or Quest hardware by the assistant.** This checklist is the release gate; do not mark v0.5 complete until it passes.

## Setup
- Pull the latest `main`. Use Unity **6000.2.14f1** (repo `ProjectVersion.txt`).
- Open existing `Assets/Jemparingan/Scenes/JemparinganOptimized_v02.unity`. No scene regeneration is needed.
- Open the Console and clear existing logs. Press Play to test mouse controls.

## Automated-in-Editor smoke test
Choose **Tools > Jemparingan > Run Target Hitbox Checks** (Edit mode). Expect a single success log:
`Jemparingan v0.5 hitbox checks passed (bullseye, outer hit, miss).`
A failure or compilation error means **stop and fix it before polishing art**.

## Manual acceptance cases
| # | Steps | Expected |
| --- | --- | --- |
| 1 | Tap LMB extremely briefly (under 0.14 sec); release. | No arrow, shot counter unchanged, short-draw feedback. |
| 2 | Hold LMB ~0.8 sec; release. | Exactly **one** arrow, counter advances by one. |
| 3 | Hold LMB at constant power and steady aim for >0.45 sec. | Hint changes from **RASA: FOKUS** to **RASA: TENANG** without changing draw power. |
| 4 | Aim well away from bandulan, shoot and wait ~1.6 sec. | **MELESET** appears in warm red, soft audio cue, combo resets. |
| 5 | Aim near visible target center and hit. | **KENA!** (jade) or **TITIK TENGAH** (gold) matches hit location and score. |
| 6 | Hit 3 times consecutively. | Combo updates and subsequent hits get the +2 bonus. |
| 7 | Fire all 12 arrows. | Transition after shots 4 and 8; final results after the last arrow resolves; no phantom 13th shot. |
| 8 | Press R between shots and once after round end. | Counters/bonus reset, arrows/effects cleared, round starts at BABAK I. |
| 9 | Repeat at least 3 full rounds. | No crashes, duplicate shots, stuck UI, or lingering arrows. |

**Important:** The v0.5 hit radius is now 0.07 m from the bandulan axis (previously 0.12 m), matching its visible ~0.05 m shaft radius with small forgiveness. A central bonus requires contact near the **middle 0.15 m section**, not merely near the long axis. Actual difficulty needs novice playtesting; if too hard, tune assistance rather than silently expanding invisible hitboxes.

## What is explicitly *not* covered by these checks
- Quest hands / pinch gestures and tracking-loss recovery on a physical headset.
- Passthrough, Android APK, headset performance or accessibility.
- Unity Play Mode validation by the assistant (Unity Editor unavailable to the assistant).

## Evidence to collect
- Screenshot of Console after the hitbox smoke check.
- Video clip of one complete 12-shot round, including a miss, a hit, and R reset.
- Any Unity error with the **first red Console message and stack trace**.

**Exit condition:** All relevant checks pass in the editor. Then move to v0.6 (tutorial, game start/results, replay); do not add features before blockers are fixed.

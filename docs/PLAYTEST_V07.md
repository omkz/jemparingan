# JEMPARINGAN — RASA / v0.7 RASA Focus Playtest

**Status: code implemented in GitHub, but not compiled or run in the Unity Editor or Quest hardware by ChatGPT.**
Do not mark v0.7 complete until manual Play Mode tests pass.

## Quick setup
1. `git pull origin main`.
2. Open the existing `Assets/Jemparingan/Scenes/JemparinganOptimized_v02.unity`; there is **no need to regenerate the scene**.
3. Check the Console for C# errors, then press Play. Mouse controls: hold LMB to draw, release to fire, hold RMB and move to aim, R to reset.
4. Also run `Tools > Jemparingan > Run Target Hitbox Checks` from Edit mode to validate previous collision rules.

## RASA rules (v0.7 implementation)
- Once the draw exceeds **35% power**, six small focus marks appear **beside the bow**, not on the target.
- Aim direction and bow-hand position are compared against **slowly adapting anchors**. A player can follow gradual movement; brief tiny jitter is allowed.
- Base tuning: **3.5° angular tolerance**, **8 cm bow-position tolerance**, and about **0.58 seconds of calm** to fill the meter.
- Focus fills during calm and fades during wobble instead of demanding a perfectly motionless hand. Large re-aims clear the meter.
- Once ready, the six marks turn **gold**, one quiet ready tone plays, and the message changes to **RASA: TENANG**.
- A **0.22-second release grace** prevents ordinary release movement from unfairly cancelling an earned bonus. A large movement or loss of valid input cancels it.
- Releasing early still shoots if the normal **minimum draw length and hold duration** are satisfied, but does **not** award RASA.
- RASA scoring is still **+3 points only on a successful hit**; missing never yields the bonus.
- In Babak II and III, a **small green lead cue** shows where the deterministic left/right-moving bandulan is expected around arrow arrival time. It is optional guidance, not an automatic correction, and does not compensate for every ballistic effect.

## Play Mode checks

| # | Action | Expected |
| --- | --- | --- |
| 1 | Start from idle, do not draw | No focus marks or lead marker visible. |
| 2 | Draw too little (<35% power) | No focus meter. A short or weak release still cannot spend a shot. |
| 3 | Draw above 35%, remain steady about 0.6 sec | Focus marks fill progressively; all become gold; RASA: TENANG appears; subtle ready sound once. |
| 4 | While drawing, move RMB+mouse rapidly | Meter drains/resets and ready state is lost after obvious movement. |
| 5 | Settle after the movement | Meter can refill; it does not require a fresh session. |
| 6 | Release before RASA is ready and hit | Hit points still count, but **no +3 RASA**. |
| 7 | Release right after the meter turns gold and hit | Hit earns **+3 RASA**. Inspect final results count. |
| 8 | Earn RASA but intentionally miss the target | No RASA points, combo resets normally. |
| 9 | Fire shots 1–4 | No extra green lead cue in Babak I. |
| 10 | Start drawing in Babak II or III | Small green future-position marker near the moving bandulan; disappears when string is released. |
| 11 | Press R during a draw and again after finishing | Meter, lead cue, pending arrows and all round stats reset; no ghost RASA bonus. |
| 12 | Complete 12 shots, review results | RASA hit count and bonus tally match +3 for each stable successful hit. |

## Quest integration note

`MetaQuestBowInput` now *provisionally* derives the arrow aim from the line between left (bow) hand and right (string) hand **when both pinches are held and geometry is valid**, caching that direction through string release. It falls back to headset forward otherwise. Hand anchors are currently `OVRHand.transform.position` and **need physical Quest tests** for comfort, orientation, and accuracy before the hands-only gameplay can be considered functional.

## Must-pass gate before more polish

- Unity C# compiles (zero red Console errors).
- A complete 12-shot round, restart, stage banners and result panel still work.
- At least 3 novices can understand why RASA was awarded **without verbal coaching** (future playtest).
- Only then tune thresholds, model art and additional audio. Do not claim Quest support or frame-rate without hardware testing.

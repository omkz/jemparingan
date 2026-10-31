# JEMPARINGAN — RASA / v0.8 Audio & Visual Polish Playtest

**Code is pushed, but has not been compiled or run in Unity Editor or on Quest hardware by ChatGPT.** These are acceptance criteria, not claims that the game passed them.

## Setup

1. `git pull origin main`. Open Unity with the editor version recorded in `ProjectSettings/ProjectVersion.txt`.
2. Open the existing `Assets/Jemparingan/Scenes/JemparinganOptimized_v02.unity`; **do not regenerate** it.
3. Clear Console. In Edit mode run `Tools > Jemparingan > Run Target Hitbox Checks`, then enter Play mode.
4. Use headphones or speakers at a moderate volume. Start with the Inspector's `Ambient Wind Volume` around **0.045** (range 0–0.15).

## Manual test cases

| Case | What to do | Expected |
| --- | --- | --- |
| Ambient | Press Play and remain idle | Very quiet continuous original wind texture; not louder than shot or hit SFX, no harsh audible loop. |
| String | Pull and release | A brief plucked-string tone plays once per valid shot. |
| Miss | Fire well away from bandulan | Quiet, non-punitive miss tone and red MISS text; no score popup. |
| Regular hit | Strike the body of the bandulan | Distinct wooden tap, jade +points popup near target, previous v0.7 hit flash and spark burst. |
| Bullseye | Strike near center of target | Different, slightly higher impact tone and gold +points popup near target. |
| Floating text | Observe a hit or bullseye for ~0.85s | Popup rises a few centimeters, disappears automatically; does not obscure aiming. |
| Arrow trail | Fire arrows rapidly | Very thin short trail behind arrow; no long streak, ghost trail or trail teleport from earlier pooled position. |
| Stage | Finish shots 4 and 8 | Gentle stage cue sounds once and normal stage banner remains functional. |
| RASA | Fill the six-step focus indicator, hit while ready | Ready cue remains distinct; +3 RASA score only on successful hit. |
| Results | Finish all 12 shots | Original end title based on accuracy/composure: e.g. PANAH JITU, RASA TERJAGA or LANGKAH PERTAMA. New records still noted in replay hint. |
| Restart | Press R during gameplay and after results | Old arrow trails, score popups and stage effects disappear. Next round starts normally. |
| Volume slider | Set Ambient Wind Volume to 0 in Inspector and restart Play | No generated wind playback; core shot/hit sounds still work. |
| Regression | Complete 3 full rounds | No Console errors, no extra arrows, scoreboard and gesture replay still work. |

## Implementation notes

- Sound is **original procedurally synthesized audio**: brief pluck, hit/bullseye taps, stage tone and a subtle looping wind texture. It is **not** a recording of traditional instruments, and no external audio assets or licenses are added.
- The ambient clip is generated **once** per game instance (4 seconds mono at 22,050 Hz), and the hit samples are prepared once, not synthesized on each hit.
- Every arrow owns one pre-created `TrailRenderer`; trail emission starts on launch and clears on cancellation/reuse.
- A small points label is part of the procedurally built stage, reused for every hit.
- End-of-round titles are **game feedback labels**, not historical cultural grading/ranks.
- No new external SDK, asset store packages, realtime shadow-casting lights, or network services were added.

## Exit gate

- Zero C# errors and zero exceptions during 3 completed rounds in Unity Editor.
- Sounds are pleasant at normal listening volume; target, arrows and RASA remain readable.
- v0.5 (shot handling), v0.6 (result/replay), and v0.7 (focus/lead cue) checks still pass.
- Physical Quest FPS, hands-only input, passthrough and APK release remain **unverified**. Prioritize real device testing before further scope expansion.

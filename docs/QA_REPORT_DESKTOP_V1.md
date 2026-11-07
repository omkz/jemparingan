# Desktop integration QA v1

**Recommendation: HOLD integration into main.** One confirmed desktop release blocker was fixed and rebuilt. The project still fails its stated URP requirement, and the manual checks below remain open. No PR was merged or closed.

## Baseline and method

- Working branch: `polish/sight-impact-fx-v1` (PR #15), starting commit `1c3c6baae481f2f0167c64ce72cdc5f35bad3580`. Clean working tree at entry; remote branch matched that SHA. No checkout from main or the old integration branch, stash, reset, or scene regeneration.
- Editor actually connected through Unity MCP: **6000.2.14f1**, this project. Opened `Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity` from disk in Edit Mode. Never ran Apply, Save Joglo Polish, or a scene generator.
- Scene SHA-256 before/after: `6fdc575db81184ccca4370d14233e36fee7c6b58d904a87dc405d896b4a36b0e`; identical to the starting Git blob. Scene remained clean, with zero missing scripts.
- Unity MCP handled scene opening, validation menus, Play Mode, runtime probes, captures, compilation, profiling, and builds. X11 input sent actual mouse/keyboard events for input checks and the standalone smoke test. Camera direction was injected for repeatable Editor aiming; this does not certify mouse aiming feel.
- Runtime evidence is in [validation/desktop-qa-v1](validation/desktop-qa-v1/). `RuntimeProbe.cs` is outside Assets and runs in memory with MCP `script_run`; it is not shipped in the player. Round probes invoke existing `Shoot` with a near-target origin, then wait for actual projectile Update, swept collisions, callbacks, progression, and results. They do **not** simulate a full human aiming session.
- Editor gameplay/visual/performance probes were collected on the starting source; the fix changes only the native-player input selection plus a startup diagnostic. Both deterministic menus and native startup/scoring/restart were verified again after the fix; standalone full-round completion remains unverified.
- Evidence timestamps are retained as returned by Unity. This host's Editor/probe and BuildReport wall clocks disagree; use build IDs, frame counts and measured elapsed times to correlate, not their absolute timestamps.

Workflow references read: local Pipeline `commands/scripts.md`, `commands/capture.md`, `commands/profiling-and-memory.md`, `commands/build-and-compilation.md`, and `commands/editor-lifecycle-and-observability.md`; Unity ProfilerRecorder documentation fetched through Context7. For reproduction, run MCP `script_run` with `file=docs/validation/desktop-qa-v1/RuntimeProbe.cs`, entry `DesktopQAProbe.Round`, args `[1]` / `[3]`, timeout 55 s. Other entry points are `Physical(mode)`, `HalfDraw(mode)`, `MotionAndFx()`, and `Profile(mode)`. Input helpers require a foreground X11 Game view and can move the pointer; run them serially, once per check. HalfDraw is a capture aid and temporarily changes only the live draw-duration field.

## Results

PASS applies only to the tested scope described in the evidence column. NOT TESTED never means a static review passed a runtime test.

| Check | Result | Actual evidence / limits |
|---|---|---|
| Correct branch, local changes preserved | PASS | Clean starting tree on PR #15 branch; only the scoped fix, report and QA evidence added. |
| Saved Joglo joinery, materials, golden-hour sky without Apply | PASS | `Validate Joglo Polish v1` passed on disk reopen and again after testing: 144 triangles, 1 joinery renderer, 0 joinery colliders, 141 polished material references, 0 old wood/roof finishes, exact saved sky asset, shadow-free sun. `editor-checks.json`; rendered captures. |
| Target Hitbox Checks | PASS | Actual menu log: Molo/Jangga/Awak/miss passed. Repeated after the source fix. |
| Gameplay QA (Traditional + Modern) | PASS | Actual menu log: **38 deterministic rule/hitbox checks passed**. Repeated after the fix. This is an Edit Mode rule check, separate from runtime results. |
| Traditional 12 arrows / 3 stages / results | PASS | `round-1.json`: all 12 expected score deltas matched; stage changes after shots 4/8; final shots 12, hits 9, score 18, finished true. Injected launches. |
| Traditional Molo/Jangga/Awak 3/2/1 | PASS | Each zone scored three times in the runtime round. Physical LMB release at the injected camera aim also scored Molo +3 (`physical-1.json`). |
| Traditional miss recovery and moving-target hits | PASS | Three runtime misses scored zero and did not stall progression; all three zones hit in stages 2 and 3 with sway active. Short flight distances, not a full-range accuracy/balance certification. |
| Traditional pendulum and cord | PASS | `motion-fx.json`: stage 1 travel 0; stage 2 observed travel 0.08008 m; stage 3 observed travel 0.27335 m across the sampling interval (displacement from interval start, not amplitude). Authored sway values 0 / 0.08 / 0.20 m. Maximum sampled cord endpoint error 0.000294 m. |
| Stage transitions, visual presentation | NOT TESTED | State progression and finish passed; no dedicated visual acceptance of each 1.25-second transition overlay. Manual step A below. |
| Traditional restart while drawing | PASS | `physical-1.json`: before restart drawing/held true; R gives shots/score 0 and await-release true; release leaves shots 0; next draw/release gives exactly one shot and +3. |
| Modern ring scoring and misses | PASS | `round-3.json`: 10/8/6/4/2/miss each exercised twice through real projectile collisions; final 12 shots, 10 hits, score 60, finished true. First probe used an invalid origin beyond the tip's crossing plane; correcting the probe from -0.30 m to -0.80 m resolved it without a game-code change. |
| Modern 3-stage progression | PASS | Stages advanced after shots 4/8 and results appeared after shot 12 in the runtime probe. |
| Modern moving center target at stage 3 | NOT TESTED | Scoring probe used target 1. Manual step B: inspect motion and fire at target 2 in stage 3. |
| Idle and full-draw physical sight / lens / center reticle | PASS | Inspected `input-1-idle/full.png`, `input-3-idle/full.png`. Lens leaves target visible; fine reticle/rim render. Full draw: Traditional reticle viewport (0.50038, 0.50011), Modern (0.49935, 0.50079), FOV 60; scored +3/+10 with real release. Idle reticle is offset with the lowered bow, intentionally not centered. |
| Half-draw sight and shared bow/hand/cuff/nocked arrow | PASS | Inspected `verified-half-1/3.png` and full-draw captures: grip/hand/cuff connect and nocked shaft/string render at sampled poses, with no blocking gap or clipping seen. `verified-half-1/3.json` records power 0.458/0.453. Draw duration temporarily slowed to 2 s for these captures only, then restored. Camera moved during synthetic pointer placement, so these images are pose checks, not fixed-camera optical accuracy measurements. |
| Alignment at extreme aim angles and other aspect ratios | NOT TESTED | Sampled Editor views were 759×602, with earlier captures at 1380×844. No systematic angle/aspect sweep. Manual step C. |
| Arrow Cam activates, returns, permits next shot | PASS | Both `physical-*.json`: release creates exactly one flying arrow and active Arrow Cam; resolved sample has no flying arrows and inactive Arrow Cam. Restart/next-shot samples verify input recovery. `input-*-arrowcam.png`. |
| Modern restart while drawing | PASS | `physical-3.json`: held/drawing true; R resets shots/score and arms await-release; release fires nothing; next draw/release scores one +10 hit. |
| Impact streak lifecycle / pooling | PASS | `motion-fx.json`: eight sparks per target, same instance IDs before/after; active early/late with shrinking width/length and outward motion; inactive after expiry; +3/+10 callbacks. |
| Impact streak visual readability at gameplay distance | NOT TESTED | Runtime transform/lifecycle check is not a visual readability test. Manual step D. |
| Console / missing scripts | PASS | No new errors during QA; sampled Unity ground truth: compilationFailed false, consoleErrors 0, consoleWarnings 0. Retained Pipeline buffer has historical errors from before this session; those counters are not current Console state. Zero missing scene scripts. |
| Bounded CPU / GC / rendering profile | PASS | Actual Editor measurements below. GPU timing and render-thread timing unavailable, not zero. |
| Linux development build | PASS | Corrected build `build_e1a0422e4cdf`: Succeeded, **0 errors / 0 warnings**, 225,463,582 bytes, 11.189 s incremental rebuild. `build-result.json`. |
| Standalone desktop startup / input selection | PASS | Before fix: wrong Quest route, HAND TRACKING LOST, no playable desktop input (`player-blocked.png`). After fix: development log says `Jemparingan input route: desktop mouse/keyboard`; desktop HUD, ring targets, bow, mouse release and +2 hit observed in the player. |
| Standalone complete playable round | NOT TESTED | Initial focused pass registered 11/12 arrows, reached stage 3 and continued after misses; input focus loss prevented certification of completion from that pass. Later synthetic-input attempts did not yield a verifiable results capture (the final screen remained stage 1 / 11 arrows). Completion and replay from results remain NOT TESTED; no additional game defect is asserted from this unreliable input run. Manual step A must be repeated in the standalone player. |
| Standalone restart | PASS | Real R reset stage 3 / score 8 / 1 arrow remaining to stage 1 / score 0 / 12 arrows (`player-fixed-replay.png`). |
| URP project requirement | FAIL | Runtime `GraphicsSettings.currentRenderPipeline` is null; serialized `m_CustomRenderPipeline` is zero; manifest has no URP package. Built-in is also explicit in Arrow Cam implementation. Pre-existing configuration mismatch, not introduced by this branch's QA fix. No pipeline migration attempted. |
| Windows/macOS player, WebGL rebuilt player, Quest hands/APK/performance | NOT TESTED | Linux desktop build only. No Quest claims. |

## Confirmed blocker fixed

`JemparinganGame.UsesDesktopInput()` previously returned true only for WebGL or the Editor. A native Linux development player therefore took the Quest-hands route and could not play either desktop mode. The condition now includes `UNITY_STANDALONE` for native desktop players, leaving the Editor preference and Android/Quest route intact. A development/Editor startup diagnostic records the selected route. No scoring, physics, scene, materials, timing, features or visual design changed.

The original build succeeded but was unplayable: build success alone did not establish runtime success. The corrected build's actual startup/input test is the verification for this fix. Windows/macOS share the standalone compilation symbol but were not built or executed here.

## Bounded performance measurements

Intel Core i5-8350U @ 1.70 GHz; Mesa Intel UHD Graphics 620; Linux/OpenGL; Unity 6000.2.14f1 Editor Play Mode; **Built-in**, Ultra quality, VSync 1, no explicit frame cap, 759×602 Game view. Warmed 0.6 s, recorded about 6 s per mode. Workload: idle, one injected hit, one miss, Arrow Cam/impact recovery. No Deep Profile, no screenshots inside the measurement interval. Unity Editor, MCP and probe overhead remain included; allocations/counters are not attributed solely to game scripts.

| Metric (mean / p95 / maximum) | Traditional, 343 samples / 6.071 s | Modern, 332 samples / 6.056 s |
|---|---:|---:|
| Main Thread (ms, includes waits) | 17.60 / 23.43 / 68.10 | 18.13 / 28.81 / 44.92 |
| GC allocated per frame (bytes) | 35,372 / 19,232 / 3,095,139 | 36,001 / 27,183 / 2,312,839 |
| Draw calls / batches | 120.74 / 161 / 182 | 126.43 / 174 / 205 |
| SetPass calls | 103.78 / 137 / 143 | 111.33 / 155 / 166 |
| Triangles | 59,863 / 87,988 / 100,182 | 62,039 / 90,486 / 105,308 |

Raw summaries: `profile-1.json`, `profile-3.json`. GC mean exceeds p95 because of large outliers. The invalid Render Thread recorder is **NOT TESTED**, regardless of its zero samples. GPU time is **NOT TESTED**. Separate Unity MCP Profiler recording `daf211b5` saved 180/180 frames, no Deep Profile, 5.794 s, 31,804,372 bytes at `/tmp/jemparingan-desktop-qa-v1.data`; it is a separate earlier capture, not the interval summarized above.

An earlier 1380×844 Traditional capture showed mean main-thread 29.08 ms, p95 99.49 ms, max 133.65 ms; it included profiling/setup noise and is not directly comparable. No pre-polish baseline, player performance capture, startup-hitch attribution or controlled regression comparison was performed. **No FPS improvement or sustained 60 FPS claim.** Manual step E is needed to attribute spikes on the intended release hardware.

## Remaining gate and manual checks

1. **Project requirement blocker:** resolve the pre-existing Built-in versus URP mismatch with an explicitly scoped pipeline task before claiming the project meets its hard constraints. This report does not authorize a visual migration during integration QA.
2. **Open acceptance evidence:** A–E below remain unverified. They do not identify proven game defects, but prevent a fully green integration gate.

A. Open v5, choose each mode, fire 12 arrows (4 per stage), watch both transition overlays and results, press R to replay. In Traditional include a miss and each colored zone.

B. In Modern stage 3, watch target 2 oscillate; hit its gold/red/outer rings while moving and miss off-board. Verify score feedback and Arrow Cam return.

C. At idle, half and full draw, aim left/right/up/down and repeat at 16:9 and 4:3. Check cuff/grip continuity, shaft/string contact, transparent lens and readable center reticle without target occlusion.

D. Hit Traditional Molo/Jangga/Awak and Modern gold/outer rings at normal gameplay distance. Visually confirm short tapered radial streaks, their fade and target flash/embedded arrow, with no persistent burst after restart.

E. Profile the standalone player on intended release hardware with CPU Timeline, GC and rendering counters; capture startup and a full round. Obtain valid GPU timing where supported, investigate large allocation/frame-time outliers, and compare the same workload/settings against a defined baseline before labelling a performance regression or improvement.

## Handoff

Keep this branch as the single integration source; this report supersedes the obsolete scene-persistence blocker in `INTEGRATION_GATE_V1.md`. The saved Joglo scene now passes without Apply. Do not merge the stacked PRs individually and also merge the consolidated branch. No merge/close actions were taken.

Build artifact: `/tmp/jemparingan-desktop-qa-v1/Jemparingan.x86_64` plus its sibling data/libraries (copy the whole directory). It is a local development artifact, not an uploaded release. Editor is left out of Play Mode with the original WebGL target restored and auto-tick enabled. Normal QA play updates the game's best-score PlayerPrefs. The temporary standalone player is closed at handoff. No tracked project/scene settings changed.

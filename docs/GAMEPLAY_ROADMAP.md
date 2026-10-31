# JEMPARINGAN — RASA / Gameplay Roadmap

**Baseline:** v0.4 (31 October 2026), source checked in `omkz/jemparingan`. Desktop Editor prototype; working Quest hand tracking has **not** been verified.

**Design goal:** Make a polished, instantly understandable, 2–4 minute seated Mixed Reality archery experience inspired by *jemparingan*. The distinctive mechanic is **RASA**: drawing, steadying, choosing when to release, and hearing/seeing meaningful feedback. Accuracy matters more than collecting power-ups or fighting enemies.

## 1. The core gameplay loop

1. **Ready**: Arena and bandulan appear. Player sees the next shot and a bow ready to hold.
2. **Hold & draw**: Hold bow with one hand, pull string with the other (mouse simulation in Editor).
3. **Focus (RASA)**: Aim, hold steady for a brief moment; subtle feedback indicates stability. Do **not** force long holds or punish natural tremor.
4. **Release**: Arrow follows a readable ballistic path. One gesture = one arrow; lost tracking must not accidentally shoot.
5. **Result**: Clear hit/miss feedback, bandulan reaction, score, and remaining arrows.
6. **Next arrow**: Automatically reset shot state. After 12 shots, display result/retry.

**First 30 seconds:** first shot with no manual; **first 60 seconds:** first satisfying hit is achievable. Game remains playable sitting down.

## 2. One-session design (12 arrows, approx. 3 minutes)

| Stage | Shots | Gameplay | Player learns |
| --- | --- | --- | --- |
| I — TENANG | 1–4 | Large effective aiming forgiveness; bandulan mostly still | Draw, aim, release; discover RASA |
| II — ANGIN | 5–8 | Gentle movement/wind cue; fewer assisted shots | Lead a moving target and time release |
| III — UJIAN RASA | 9–12 | More pronounced but predictable sway; no sudden randomness | Consistency and precise calm release |

Changes in difficulty must be **predictable and readable**, not raw RNG. Avoid making actual hit zones larger than their visual representation without visible explanation.

## 3. Scoring rules (current prototype baseline)

- Bandulan hit: **5 points**
- Central hit: **10 points**, instead of 5
- Stable RASA release: **+3 points on hit**
- Combo (3+ consecutive hits): **+2 points per subsequent hit**
- Miss: **0 points**, resets combo when projectile ends
- Save personal best locally; end-screen shows score, accuracy, central hits, and highest combo (last two require counters not yet present).

The RASA bonus is **an accessible optional reward**, not a mandatory action; keep the player in control of release timing. Future target size/assist adjustments should be tuned from actual playtests.

## 4. Prioritized delivery

### P0 — v0.5: Make every shot satisfying (Oct 31 – Nov 2)
- [ ] Verify v0.4 in Unity: 12 shots, 3 stages, combo, restart, and Console clean.
- [x] Implement release timing guard (min duration + draw distance) and state reset after lost tracking. **Pending Unity validation.**
- [ ] Tune bow power, speed and trajectory from actual Unity playtesting. **Implemented:** hitbox narrowed to match visual bandulan and center bonus limited to central section; gameplay balancing still pending.
- [x] Implement distinct **hit**, **bullseye**, **miss** feedback (colors + short SFX). **Pending Unity validation.**
- [x] Implement shot-ready hint, shot-release cue and stable RASA indicator refresh. **Pending Unity validation.**
**Acceptance:** A tester completes 12 shots without input or progression bugs; projectile reactions and points match what is seen.

### P0 — v0.6: Complete the playable 3-minute loop (Nov 3 – 5)
- [x] Implement in-world introduction (auto-dismiss on draw; no menu). **Pending Unity validation.**
- [x] Implement brief BABAK II/III transition banner (~1.25 sec). **Pending Unity validation.**
- [x] Implement results (score, hits/12 in HUD, accuracy, central hits, highest combo, RASA bonus and new-record indicator), plus replay via R/gesture. **Pending Unity validation.**
- [x] Add replay input arm/release guard and reset of session UI/stats; mid-round R shortcut preserved. **Pending Unity validation, especially input races.**
- [ ] Confirm hands-only completion on actual Quest hardware. The replay uses IJemparinganInput but Quest rig is not integrated/tested.
**Acceptance:** New tester can understand and finish a round without verbal explanation; replay works at least 3 consecutive times.

### P0 — v0.7: Make RASA the signature mechanic (Nov 6 – 8)
- [x] Implement bow/hand-aim direction (when valid pinch geometry exists) in Quest adapter and compare bow aim + bow-position motion against smoothed focus anchors; desktop uses mouse-simulated pose. **Pending Unity and Quest validation.**
- [x] Implement a six-step focus indicator alongside the bow, visible only when draw strength exceeds 35%. **Pending Unity visual verification.**
- [x] Implement configurable tolerance (3.5°/8cm), ~0.58sec progression, decay rather than hard reset for small wobbles, and 0.22sec release grace. **Threshold calibration with real players remains pending.**
- [x] Early valid release still fires normally without RASA bonus; stable successful hits receive +3. **Pending Unity validation.**
- [x] Implement small optional predictive marker in Stage II/III based on the same deterministic target sway. **Actual lead/timing accuracy and legibility still require playtesting.**
**Acceptance:** 3 testers can explain when/why they earned RASA bonus, and do not need to freeze unnaturally.

### P1 — v0.8: Delight & authenticity (Nov 9 – 11)
- [ ] Add short original/licensed wooden string twang, bandulan hit and gentle ambient audio.
- [ ] Improve arrow-in-target readability, camera-safe feedback, pendopo silhouette and minimal cultural labels.
- [ ] Add personalized end-of-round title based on *accuracy + composure*, not simply raw score (avoid ungrounded claims about historical grading).
- [ ] Make miss effects respectful, immediate and quiet; avoid excessive punishment or visual clutter.
**Acceptance:** First 30 seconds are immediately understandable and visually interesting, with no distractions during aiming.

### P0 — v0.9: Balance, usability, and freeze (Nov 12 – 14)
- [ ] Playtest 3 full rounds on a real Quest 3/3S with **hands only** and no prompts from the developer.
- [ ] Confirm both-handed gestures are comfortable seated and recover safely from tracking loss.
- [ ] Record where players miss and why; tune assist, power, target movement, timing and UI legibility.
- [ ] Fix blockers; freeze gameplay rules once the loop is consistent.
**Acceptance:** 3 uninterrupted complete runs, no accidental release, no controller dependence, and a good first hit within the first minute for a novice (tuning target, not guarantee).

### Submission window (Nov 15 – 18)
Gameplay **feature freeze**. Only fix crashes/blockers and record actual headset footage. APK, Meta release channel, Devpost and related assets are managed by the separate submission/technical roadmap.

## 5. Scope control

**Must-have:** draw/release, predictable arrow flight, bandulan hit detection, RASA feedback, 12-shot 3-stage loop, score/results/replay, hands-only usability.

**Nice-to-have only if P0 passes:** additional bow variants, alternate arena theme, challenge medals, advanced wind simulation.

**Explicitly out of scope for hackathon:** multiplayer, online leaderboard, inventory/shop, RPG progression, voice/chatbot/agent, 20+ levels, generative AI features, complex character animation.

## 6. Decision gates

- **Nov 2:** If 12-shot desktop gameplay still has blocking bugs, stop art work and fix the loop.
- **Nov 6:** If Quest two-hand release is not stable, simplify gesture geometry/aim assist, **not** gameplay depth.
- **Nov 11:** If new players do not understand RASA, simplify its feedback or remove the scoring bonus while keeping accurate archery.
- **Nov 14:** Freeze mechanics. Prioritize performance, stability, and truthful submission footage.

## Next work item

Complete [v0.5](PLAYTEST_V05.md), [v0.6](PLAYTEST_V06.md) and [v0.7 RASA](PLAYTEST_V07.md) playtests in Unity Editor. v0.7 features are **implemented but not runtime-verified**. After those pass, validate Quest hand interaction and proceed to v0.8 audio/art polish. Stop expanding scope if there are blocking bugs.

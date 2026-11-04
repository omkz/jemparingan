# JEMPARINGAN — RASA

Mixed Reality archery prototype inspired by Javanese jemparingan, targeting Meta Quest 3 / 3S for the 2026 Meta VR Start Developer Competition.

**Current stage: v0.9 gameplay hardening and regression checks built on v0.8; desktop-editor prototype only.** Not yet validated as a Quest APK.

## Open and play

- Unity editor version recorded in `ProjectSettings/ProjectVersion.txt`: **6000.2.14f1**.
- Use **Tools → Jemparingan → Create Optimized Scene v0.2** to create a new demo scene at `Assets/Jemparingan/Scenes/JemparinganOptimized_v02.unity`.
- Click Play. **Hold left mouse** to pull the string, **release** to shoot, **hold right mouse + move mouse** to aim; press **R** to reset the round.
- Set **Active Input Handling = Both** if the legacy mouse input APIs do not respond.

## Optional environment preview (separate branch)

The `feature/safe-environment-preview-v1` branch includes a standalone outdoor Joglo environment:

- **Lighting v3:** golden-hour procedural skybox, warm daylight and distance fog.
- **Landscape v4:** more organic sawah groups, banana plants and layered mesh mountains.
- **Playable Environment v5:** one live bandulan at 10/20/30 m, desktop mouse input (including Web builds), and **16 arrows across four rounds**, finishing with **ROUND IV — EXTREME SWING**. The original v0.9 indoor scene remains 12 arrows / three stages.

The generator creates scenes under `Assets/Jemparingan/Scenes/` using
**Tools > Jemparingan > Environment > Create Joglo Preview v2** (scenic) or
**Create Playable Environment v5** (interactive). The existing `JemparinganOptimized_v02`
scene is not overwritten. See [Environment v3–v5 Guide](docs/ENVIRONMENT_V3_V5.md).
The final round adds four shots (arrows 13–16). The bandulan moves 1.8× farther and about 1.35× faster than Round III, with deterministic turns and smooth round transitions. No wind-force mechanic or score bonus change. **Existing Playable v5 scenes work without regeneration**: the outdoor shot count is upgraded at runtime. For the Unity Play browser version, rebuild and publish a new Web build to update the hosted game.

These changes are source-reviewed, not Unity/Quest-tested in this environment.

## Gameplay and visual features (v0.9)

- A 12-shot round with three difficulty stages, combo scoring, bullseye/stability bonus, and local best-score persistence.
- Reusable arrow pool, swept arrow-tip collision, modest aim assist, and trajectory preview.
- Procedural pendopo, bow, and bandulan, with simple synthesized audio.
- **v0.3:** bandulan recoil and a reusable eight-spark impact burst; arrows briefly stick to the bandulan before returning to the existing pool. No imported VFX package required.
- **v0.4:** pendopo backdrop, drapes and lanterns, supporting frame/plaque behind bandulan, small bow ornaments, phase label for three stages, 12-shot progress pips, and combo callout.
- **v0.5:** minimum sustained draw before firing, stable RASA hint refresh, clearer ready/release/miss feedback, different hit/bullseye/miss colors, and a smaller target collision zone aligned to the visible bandulan.
- **v0.6:** in-world introduction (dismissed by first draw), brief stage transition banner, end-of-round results (score, accuracy, bullseye, highest combo, RASA bonus), correct new-record detection, and replay via R (Editor) or release-and-hold string gesture (~0.7s).
- **v0.7:** six-step RASA charge indicators beside bow, smoothed aim/hand-position stability with tremor tolerance and short release grace, ready chime, and optional predicted target position (small green dot) in stages II/III. The Quest hand adapter now uses the bow-string hand axis while both are pinched, but **has not been hardware tested**.
- **v0.8:** original low-volume procedural wind ambience, different wooden impact sounds for hit vs bullseye, gentle stage-change cue, brief floating score feedback, short pooled arrow trails, and end-of-round titles based on accuracy and RASA composure. No third-party art/audio asset or external SDK required.
- **v0.9:** safeguards against double releases when restarting with the string held; arrow pool capacity scales to configured shots; exact trajectory preview now matches arrow gravity integration; deterministic score/stage/accuracy helpers and a **21-check Unity Editor gameplay QA** menu.
- Supplemental imported OBJ/MTL models in `Assets/Models/`; the demo builds its own simple geometry at runtime.

## Source layout

- `Assets/Scripts/JemparinganGame.cs` — gameplay, scoring, and procedural scene setup.
- `Assets/Scripts/JemparinganArrow.cs`, `JemparinganTarget.cs` — arrow flight and hit testing.
- `Assets/Scripts/JemparinganArt.cs` — shared materials and procedural geometry.
- `Assets/Scripts/DesktopBowInput.cs` — mouse-only editor controls.
- `Assets/Scripts/MetaQuestBowInput.cs` — future Quest hand input adapter; **not yet wired into a tested headset build**.
- `Assets/Scripts/Editor/JemparinganEditorMenu.cs` — scene creation menu.

## Gameplay roadmap

See [Gameplay Roadmap](docs/GAMEPLAY_ROADMAP.md) for the three-stage 12-arrow loop, RASA mechanic, gameplay milestones, and acceptance gates.

**Current gate:** [v0.9 Regression Gate](docs/PLAYTEST_V09.md): run **Tools > Jemparingan > Run Gameplay QA v0.9** (21 deterministic checks) then complete three real Play Mode rounds. Also see [v0.8](docs/PLAYTEST_V08.md), [v0.7](docs/PLAYTEST_V07.md), [v0.6](docs/PLAYTEST_V06.md) and [v0.5](docs/PLAYTEST_V05.md). In Edit mode run **Tools > Jemparingan > Run Target Hitbox Checks** before Play Mode testing. A code commit is not evidence of successful Unity compilation.

## Quest integration (next gate)

The v0.9 Editor QA has been reported successful by the developer; this does **not** establish a working Quest build. The next step is a **hands-only Quest feasibility build**, not new gameplay content.

See **[Quest OpenXR, Meta XR hands and passthrough setup](docs/QUEST_SETUP_V10.md)**. A new read-only Editor menu, **Tools > Jemparingan > Quest > Validate Active Scene**, checks package/scene prerequisites. It does not install SDKs, create an APK or test a headset. Keep the existing desktop scene intact.

## Next milestone: Quest integration

- **Confirmed from user:** Unity compiler errors were resolved after removing duplicate rule classes. This does **not** establish three clean gameplay rounds or a Quest APK.
- Complete the v0.9 regression gate (21 checks + three full 12-shot rounds).
- Next follow the [Quest Integration Guide](docs/QUEST_INTEGRATION.md): install Android/OpenXR/Meta XR SDK, make a **separate** Quest scene with hand prefabs and passthrough, then test the APK on a real device.
- Run **Tools > Jemparingan > Check Quest Prerequisites** after pulling; this is an advisory preflight, **not** an automated Quest installer.
- The developer uses Linux Mint: Meta Link and XR Simulator are not available there, and Meta's Linux Editor support remains unofficial. Prefer on-device APK testing; use a supported build environment if Meta SDK compatibility blocks Linux.
- Quest hand tracking, passthrough, Android build and headset performance verification remain **not complete**.

## Git workflow

Pull updates with `git pull origin main` after committing/stashing your local changes. Do not commit Unity-generated `Library/`, `Temp/`, `Logs/` or compiled APKs. **Commit Unity `.meta` files** with their assets.

## Hackathon

https://start-developer-competition-26.devpost.com/

Submission deadline: November 18, 2026, noon Pacific time. An eventual Quest build must meet the contest's hands-first requirements. Do not submit this editor prototype as a finished Quest experience.

## v0.9 verification (Unity Editor)

1. Run the existing scene `JemparinganOptimized_v02` after pulling. No regeneration is necessary because stage art is created at runtime.
2. Confirm the dark backdrop and target plaque improve visibility; ensure no decoration blocks the aiming line.
3. Fire 12 arrows: progress pips should advance, stage labels switch after shots 4 and 8, and points/combo update on hit.
4. Press R: effects, progress and stage should reset.
5. Check the Unity Console for compile errors and Play Mode errors. **These changes have only been reviewed statically, not runtime-tested in Unity or Quest.**

See [v0.9 regression checks](docs/PLAYTEST_V09.md). All v0.5–v0.9 versions still need real Unity Editor verification; passing static source checks is not a gameplay test.

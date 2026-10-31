# JEMPARINGAN — RASA

Mixed Reality archery prototype inspired by Javanese jemparingan, targeting Meta Quest 3 / 3S for the 2026 Meta VR Start Developer Competition.

**Current stage: v0.7 RASA focus and moving-target lead cue built on v0.6; desktop-editor prototype only.** Not yet validated as a Quest APK.

## Open and play

- Unity editor version recorded in `ProjectSettings/ProjectVersion.txt`: **6000.2.14f1**.
- Use **Tools → Jemparingan → Create Optimized Scene v0.2** to create a new demo scene at `Assets/Jemparingan/Scenes/JemparinganOptimized_v02.unity`.
- Click Play. **Hold left mouse** to pull the string, **release** to shoot, **hold right mouse + move mouse** to aim; press **R** to reset the round.
- Set **Active Input Handling = Both** if the legacy mouse input APIs do not respond.

## Gameplay and visual features (v0.7)

- A 12-shot round with three difficulty stages, combo scoring, bullseye/stability bonus, and local best-score persistence.
- Reusable arrow pool, swept arrow-tip collision, modest aim assist, and trajectory preview.
- Procedural pendopo, bow, and bandulan, with simple synthesized audio.
- **v0.3:** bandulan recoil and a reusable eight-spark impact burst; arrows briefly stick to the bandulan before returning to the existing pool. No imported VFX package required.
- **v0.4:** pendopo backdrop, drapes and lanterns, supporting frame/plaque behind bandulan, small bow ornaments, phase label for three stages, 12-shot progress pips, and combo callout.
- **v0.5:** minimum sustained draw before firing, stable RASA hint refresh, clearer ready/release/miss feedback, different hit/bullseye/miss colors, and a smaller target collision zone aligned to the visible bandulan.
- **v0.6:** in-world introduction (dismissed by first draw), brief stage transition banner, end-of-round results (score, accuracy, bullseye, highest combo, RASA bonus), correct new-record detection, and replay via R (Editor) or release-and-hold string gesture (~0.7s).
- **v0.7:** six-step RASA charge indicators beside bow, smoothed aim/hand-position stability with tremor tolerance and short release grace, ready chime, and optional predicted target position (small green dot) in stages II/III. The Quest hand adapter now uses the bow-string hand axis while both are pinched, but **has not been hardware tested**.
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

**Current gate:** [v0.7 RASA Playtest](docs/PLAYTEST_V07.md), plus [v0.6](docs/PLAYTEST_V06.md) and [v0.5](docs/PLAYTEST_V05.md) regression checks. In Edit mode run **Tools > Jemparingan > Run Target Hitbox Checks** before Play Mode testing. A code commit is not evidence of successful Unity compilation.

## Next milestone

Verify that focus progressively charges on a steady meaningful draw, drains during deliberate re-aiming, and grants +3 only for stable successful hits. Run an entire 12-shot round and replay to confirm v0.5/v0.6 behavior remains intact. Quest hand tracking, passthrough, Android build and actual headset performance testing remain **not complete**.

## Git workflow

Pull updates with `git pull origin main` after committing/stashing your local changes. Do not commit Unity-generated `Library/`, `Temp/`, `Logs/` or compiled APKs. **Commit Unity `.meta` files** with their assets.

## Hackathon

https://start-developer-competition-26.devpost.com/

Submission deadline: November 18, 2026, noon Pacific time. An eventual Quest build must meet the contest's hands-first requirements. Do not submit this editor prototype as a finished Quest experience.

## v0.7 verification (Unity Editor)

1. Run the existing scene `JemparinganOptimized_v02` after pulling. No regeneration is necessary because stage art is created at runtime.
2. Confirm the dark backdrop and target plaque improve visibility; ensure no decoration blocks the aiming line.
3. Fire 12 arrows: progress pips should advance, stage labels switch after shots 4 and 8, and points/combo update on hit.
4. Press R: effects, progress and stage should reset.
5. Check the Unity Console for compile errors and Play Mode errors. **These changes have only been reviewed statically, not runtime-tested in Unity or Quest.**

See [v0.7 RASA test cases](docs/PLAYTEST_V07.md) for the new acceptance checklist. v0.5, v0.6 and v0.7 all still need real Unity Editor verification.

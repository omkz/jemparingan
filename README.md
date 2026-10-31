# JEMPARINGAN — RASA

Mixed Reality archery prototype inspired by Javanese jemparingan, targeting Meta Quest 3 / 3S for the 2026 Meta VR Start Developer Competition.

**Current stage: v0.6 complete-round UI and replay flow built on v0.5; desktop-editor prototype only.** Not yet validated as a Quest APK.

## Open and play

- Unity editor version recorded in `ProjectSettings/ProjectVersion.txt`: **6000.2.14f1**.
- Use **Tools → Jemparingan → Create Optimized Scene v0.2** to create a new demo scene at `Assets/Jemparingan/Scenes/JemparinganOptimized_v02.unity`.
- Click Play. **Hold left mouse** to pull the string, **release** to shoot, **hold right mouse + move mouse** to aim; press **R** to reset the round.
- Set **Active Input Handling = Both** if the legacy mouse input APIs do not respond.

## Gameplay and visual features (v0.6)

- A 12-shot round with three difficulty stages, combo scoring, bullseye/stability bonus, and local best-score persistence.
- Reusable arrow pool, swept arrow-tip collision, modest aim assist, and trajectory preview.
- Procedural pendopo, bow, and bandulan, with simple synthesized audio.
- **v0.3:** bandulan recoil and a reusable eight-spark impact burst; arrows briefly stick to the bandulan before returning to the existing pool. No imported VFX package required.
- **v0.4:** pendopo backdrop, drapes and lanterns, supporting frame/plaque behind bandulan, small bow ornaments, phase label for three stages, 12-shot progress pips, and combo callout.
- **v0.5:** minimum sustained draw before firing, stable RASA hint refresh, clearer ready/release/miss feedback, different hit/bullseye/miss colors, and a smaller target collision zone aligned to the visible bandulan.
- **v0.6:** in-world introduction (dismissed by first draw), brief stage transition banner, end-of-round results (score, accuracy, bullseye, highest combo, RASA bonus), correct new-record detection, and replay via R (Editor) or release-and-hold string gesture (~0.7s).
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

**Current gate:** [v0.6 Playtest Checklist](docs/PLAYTEST_V06.md) (also see [v0.5 checks](docs/PLAYTEST_V05.md)). In Edit mode, run **Tools > Jemparingan > Run Target Hitbox Checks** before Play Mode testing. A code commit is not evidence of successful Unity compilation.

## Next milestone

Verify the full 12-shot loop in Unity Editor: intro, 3-stage progression, scoring, finish results and replay via R or hold gesture. Confirm that score and saved best are correct and no input gets stuck. Quest hand tracking, passthrough, Android build and actual headset performance testing remain **not complete**.

## Git workflow

Pull updates with `git pull origin main` after committing/stashing your local changes. Do not commit Unity-generated `Library/`, `Temp/`, `Logs/` or compiled APKs. **Commit Unity `.meta` files** with their assets.

## Hackathon

https://start-developer-competition-26.devpost.com/

Submission deadline: November 18, 2026, noon Pacific time. An eventual Quest build must meet the contest's hands-first requirements. Do not submit this editor prototype as a finished Quest experience.

## v0.6 verification (Unity Editor)

1. Run the existing scene `JemparinganOptimized_v02` after pulling. No regeneration is necessary because stage art is created at runtime.
2. Confirm the dark backdrop and target plaque improve visibility; ensure no decoration blocks the aiming line.
3. Fire 12 arrows: progress pips should advance, stage labels switch after shots 4 and 8, and points/combo update on hit.
4. Press R: effects, progress and stage should reset.
5. Check the Unity Console for compile errors and Play Mode errors. **These changes have only been reviewed statically, not runtime-tested in Unity or Quest.**

See [v0.6 test cases](docs/PLAYTEST_V06.md) for the full acceptance checklist. Both v0.5/v0.6 are pending real Unity Editor testing.

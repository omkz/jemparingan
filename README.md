# JEMPARINGAN — RASA

Mixed Reality archery prototype inspired by Javanese jemparingan, targeting Meta Quest 3 / 3S for the 2026 Meta VR Start Developer Competition.

**Current stage: v0.3 game-feel pass, extending v0.2 gameplay and procedural art; desktop-editor prototype only.** Not yet validated as a Quest APK.

## Open and play

- Unity editor version recorded in `ProjectSettings/ProjectVersion.txt`: **6000.2.14f1**.
- Use **Tools → Jemparingan → Create Optimized Scene v0.2** to create a new demo scene at `Assets/Jemparingan/Scenes/JemparinganOptimized_v02.unity`.
- Click Play. **Hold left mouse** to pull the string, **release** to shoot, **hold right mouse + move mouse** to aim; press **R** to reset the round.
- Set **Active Input Handling = Both** if the legacy mouse input APIs do not respond.

## Gameplay and visual features (v0.3)

- A 12-shot round with three difficulty stages, combo scoring, bullseye/stability bonus, and local best-score persistence.
- Reusable arrow pool, swept arrow-tip collision, modest aim assist, and trajectory preview.
- Procedural pendopo, bow, and bandulan, with simple synthesized audio.
- **v0.3:** bandulan recoil and a reusable eight-spark impact burst; arrows briefly stick to the bandulan before returning to the existing pool. No imported VFX package required.
- Supplemental imported OBJ/MTL models in `Assets/Models/`; the demo builds its own simple geometry at runtime.

## Source layout

- `Assets/Scripts/JemparinganGame.cs` — gameplay, scoring, and procedural scene setup.
- `Assets/Scripts/JemparinganArrow.cs`, `JemparinganTarget.cs` — arrow flight and hit testing.
- `Assets/Scripts/JemparinganArt.cs` — shared materials and procedural geometry.
- `Assets/Scripts/DesktopBowInput.cs` — mouse-only editor controls.
- `Assets/Scripts/MetaQuestBowInput.cs` — future Quest hand input adapter; **not yet wired into a tested headset build**.
- `Assets/Scripts/Editor/JemparinganEditorMenu.cs` — scene creation menu.

## Next milestone

Focus on feel and polish in the editor: verify that all 12 shots, scoring, stage changes and restart work. Confirm that a hit flashes the bandulan, shows a brief spark burst, leaves the arrow embedded for about a second, and that pressing R clears old effects. Shots that miss should still expire normally. Quest hand tracking, passthrough, Android build and actual headset performance testing are postponed, **not complete**.

## Git workflow

Pull updates with `git pull origin main` after committing/stashing your local changes. Do not commit Unity-generated `Library/`, `Temp/`, `Logs/` or compiled APKs. **Commit Unity `.meta` files** with their assets.

## Hackathon

https://start-developer-competition-26.devpost.com/

Submission deadline: November 18, 2026, noon Pacific time. An eventual Quest build must meet the contest's hands-first requirements. Do not submit this editor prototype as a finished Quest experience.

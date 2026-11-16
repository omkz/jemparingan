# Jemparingan — agent guidance

Unity **6000.3.27f1**, URP, OpenXR. Two game modes: **Traditional** and **Modern**.

## Preserve
- Desktop scene: `Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity`.
- Archived Meta Quest scene: `Assets/Jemparingan/Scenes/JemparinganQuest.unity` (do not overwrite).
- XR-only playtest: `Assets/Jemparingan/Scenes/JemparinganXRIOnly.unity`, generated from the desktop scene.
- `IJemparinganInput` is shared by desktop mouse and Unity XR Hands. Keep shot physics, scoring, and rules shared.
- Use Unity OpenXR + XR Hands + XRI; Meta XR SDK is removed. Quest device behavior is unverified.

## Work
- Prefer small, focused diffs and existing helpers over new abstractions.
- No dead code, duplicate features, speculative workarounds, or unnecessary dependencies.
- Keep comments, README, and docs concise; describe actual behavior and limitations, not task history.
- Preserve Unity `.meta` GUIDs, scene references, and serialized field names when refactoring.
- Never regenerate an authored scene, delete QA evidence, or merge to `main` without approval.

## Verify
- Compile in Unity; run **Tools > Jemparingan > Run Gameplay QA (Traditional + Modern)**.
- Playtest both desktop modes: shoot, hit/miss, restart, and 12-arrow results.
- For XR, validate pinch/release and tracking-loss cancellation separately. Editor simulation is **not** proof of Quest device or APK success.
- Record tests as *passed*, *failed*, or *not run*. Never claim runtime, headset, or performance success from source review alone.

Current setup: [README.md](README.md). QA and historical guides: [docs/README.md](docs/README.md).

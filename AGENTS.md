# Jemparingan — agent guidance

Unity **6000.3.27f1**, URP, OpenXR. Two game modes: **Traditional** and **Modern**.

## Preserve
- Desktop scene: `Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity`.
- Quest scene: `Assets/Jemparingan/Scenes/JemparinganQuest.unity`.
- XRI Editor playtest is a **separate, generated scene**; never overwrite source scenes.
- `IJemparinganInput` is shared by desktop mouse, Unity XR Hands, and Meta OVRHand. Keep projectile physics, scoring, and game rules shared.
- Hands-only Quest interaction remains **unverified on a physical device**.

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

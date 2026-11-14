# XRI Hand Simulator — smoke test (Editor only)

This checks simulated **head/hands/pinch** without Meta XR Simulator, OpenXR frame submission, or Jemparingan's `OVRHand` adapter. Do **not** treat it as Quest device QA.

## Setup (Unity 6000.3)

1. Check out `feat/unity-xri-hand-simulator-smoke` and open Unity. Let Package Manager resolve `com.unity.xr.interaction.toolkit` **3.5.1** and `com.unity.xr.hands` **1.8.1**. Unity will refresh `Packages/packages-lock.json`; commit that lockfile only after local resolution.
2. **Close Meta XR Simulator** and leave OpenXR runtime simulation disabled. This test uses Unity's simulated input in the Editor.
3. `Window > Package Manager > XR Interaction Toolkit > Samples`: import **Starter Assets** and the **XR Device Simulator / XR Interaction Simulator** sample (sample name varies by version).
4. `Tools > Jemparingan > Simulator > Create/Open XRI Hand Smoke Scene`. The menu creates a separate `JemparinganXRIHandSmoke.unity` scene from the imported official prefabs, with a small grabbable cube. It refuses to overwrite existing scenes or discard unsaved changes.
5. Press **Play** and focus **Game**. The simulator's on-screen controls show the current input bindings. Typically `Tab` changes modes, `[` and `]` select hands, pressing a bracket twice cycles Controller/Hand mode, and `M` toggles the hand pinch pose. Move the hands toward the cube and try grabbing it.

**Pass:** Game View renders and simulated hands respond to position changes and pinch. **Fail:** compile errors, no hands, no hand pose change, or frozen Editor; capture `Editor.log` and the first Console error.

## Scope / constraints

- Does **not** modify the desktop gameplay scene, the Quest scene, `MetaQuestBowInput`, Android build settings, or the global `Use XR Interaction Simulator in scenes` setting.
- The current Quest input reads **Meta OVRHand**, not Unity **XR Hands**. Simulated hands in this scene **will not control the Jemparingan bow** yet. Only after this smoke passes should we add a separate XR Hands input adapter to the actual bow logic.
- The scene builder depends on official sample assets imported locally under `Assets/Samples/XR Interaction Toolkit/...`. Do not commit large imported samples unless the team intentionally decides to vendor them.
- Physical Quest headset testing still required for validation of tracking confidence, real pinch ergonomics, and XR rendering.

Official: https://docs.unity.cn/Packages/com.unity.xr.interaction.toolkit@3.5/manual/xr-interaction-simulator-overview.html

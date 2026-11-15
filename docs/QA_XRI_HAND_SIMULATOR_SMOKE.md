# XRI Hand Simulator — smoke test (Editor only)

This checks simulated **head/hands/pinch** without Meta XR Simulator, OpenXR frame submission, or Jemparingan's `OVRHand` adapter. Do **not** treat it as Quest device QA.

## Setup (Unity 6000.3, Linux or Windows)

1. Check out `feat/unity-xri-hand-simulator-smoke`, open Unity, and allow Package Manager to resolve `com.unity.xr.interaction.toolkit` **3.5.1** and `com.unity.xr.hands` **1.8.1**. Commit `Packages/packages-lock.json` only after local resolution.
2. Do **not** launch Meta XR Simulator. In Linux, the Meta `OVRPlugin` native library can throw `DllNotFoundException`; this separate smoke test does not require OVRHand.
3. In `Window > Package Manager > XR Interaction Toolkit > Samples`, import **Starter Assets**, **Hands Interaction Demo**, and **XR Interaction Simulator** (not **XR Device Simulator (Legacy)**). In `XR Hands > Samples`, also import **HandVisualizer**.
4. If you already created `Assets/Jemparingan/Scenes/JemparinganXRIHandSmoke.unity` using the old controller-only prefab: **Stop Play**, save any scene changes, open that scene, then run `Tools > Jemparingan > Simulator > Upgrade Open XRI Smoke Scene to Hands`. This replaces only the controller-only XR Origin with the official **XR Interaction Hands Setup** prefab.
5. For a new project workspace without a smoke scene: use `Tools > Jemparingan > Simulator > Create/Open XRI Hand Smoke Scene`; it now requires **XR Interaction Hands Setup** to prevent creating a controller-only test.
6. Press **Play**, focus **Game**, press `Tab` to enter device manipulation mode, then press `[` twice in succession to cycle the left device from Controller to Hand; `]` twice similarly for the right device. Select a hand and press `M` to toggle pinch. Use the on-screen simulator overlay to confirm whether the active device reads **Hand**, and try the cube. The key map can be customized in the imported sample.

**Pass:** active hand mode is shown, visible simulated hands move, pinch occurs, and the grabbable cube responds. **Fail:** no hand mode, no hand visuals, no hand movement, compile errors, or frozen Editor; send the first Console error and note which symptom occurred.

## Scope / constraints

- Does **not** modify the desktop gameplay scene, the Quest scene, `MetaQuestBowInput`, Android build settings, or the global `Use XR Interaction Simulator in scenes` setting.
- An existing smoke scene is deliberately **not** overwritten when opened. Use the explicit upgrade command to switch the old controller-only rig to the hands-enabled rig.
- The current Quest input reads **Meta OVRHand**, not Unity **XR Hands**. Simulated hands in this scene **will not control the Jemparingan bow** yet. Only after this smoke passes should we add a separate XR Hands input adapter to the actual bow logic.
- The scene builder depends on official sample assets imported locally under `Assets/Samples/XR Interaction Toolkit/...`. Do not commit large imported samples unless the team intentionally decides to vendor them.
- Physical Quest headset testing still required for validation of tracking confidence, real pinch ergonomics, and XR rendering.

Official: https://docs.unity.cn/Packages/com.unity.xr.interaction.toolkit@3.5/manual/xr-interaction-simulator-overview.html

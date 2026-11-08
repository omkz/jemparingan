# Quest VR simulator stabilization v1 — focused patch review

Base: `codex/quest-vr-scene-v1`. Branch: `fix/quest-xr-simulator-stabilization-v1`.

## Changes

1. `JemparinganGame.UsesDesktopInput()` no longer forces mouse/keyboard for **every** Windows standalone build. Android player stays hands-first; WebGL stays desktop; Editor and Windows standalone honor the scene's existing serialized `preferQuestHandsInEditor` switch (false in the desktop scene; true in the Quest scene). The serialized field name is intentionally preserved so authored scene values survive.
2. `JemparinganQuestEditorXR` now also enables the existing Quest-only transient Editor OpenXR initialization when the minimal `JemparinganOpenXRSmoke.unity` scene is opened. The original desktop scene still does not start XR automatically.
3. `Tools → Jemparingan → Quest VR v1 → Create/Open Minimal OpenXR Smoke Scene` generates an isolated scene consisting of a URP XR-enabled camera, one cube and one light. It has no `OVRManager`, tracked hands, gameplay, or authored environment. It refuses to overwrite a pre-existing smoke scene and rejects dirty open scenes.
4. Desktop Input System QA records frame number, game time, desktop adapter state, active mouse device, actual button/pose state, draw state and shot count at hold/release. It reselects the virtual mouse while sending events. New output is `docs/validation/quest-vr-v1/desktop-input-regression-stabilization.json`, leaving earlier evidence intact.

## Testing and limits

- **Source review**: Completed against the actual GitHub branch contents.
- **GitHub writes**: Committed to a separate feature branch; no original desktop scene, Quest scene, or package file modified.
- **Unity compilation**: **NOT TESTED** in this environment. Must run on the Windows machine with Unity 6000.3.27f1 and the packages listed in `QA_REPORT_QUEST_VR_V1.md`.
- **Meta XR Simulator frame output**: **NOT TESTED after patch**. The prior QA continues to show `No frames received`, Direct3D11 first-frame stall, and Vulkan `Failed to allocate memory: -2` / swapchain creation failure on Intel UHD 620.
- **Desktop input regression**: **NOT TESTED after patch**. Existing retry reported zero real shots, and the cause remains unconfirmed.
- **Android APK**: **NOT RETESTED**; existing `TaskCanceledException` still needs a full Gradle/Unity build log.

**Do not claim that this patch resolves the GPU/simulator swapchain failure.** It adds isolation and fixes a separate input routing defect.

## Windows steps (no headset required)

1. Preserve local changes; fetch and check out `fix/quest-xr-simulator-stabilization-v1` from origin.
2. Open Unity, let compilation finish, and resolve any errors before Play.
3. Stop Play Mode. Select `Tools → Jemparingan → Quest VR v1 → Configure Editor Simulator OpenXR`.
4. Select `Tools → Jemparingan → Quest VR v1 → Create/Open Minimal OpenXR Smoke Scene`.
5. Activate Meta XR Simulator; press Play in the smoke scene. This isolates the **OpenXR frame submission** path, **not** head tracking or hand input.
6. Record the Unity Console, Meta XR Simulator logs and whether the cube appears in the simulator:
   - **Smoke PASS / Quest FAIL**: investigate OVRManager, Quest XR rig, or complex scene/rendering.
   - **Smoke FAIL with the same swapchain error**: prioritize graphics driver, GPU/runtime compatibility, simulator host environment; do not rewrite gameplay.
7. For desktop QA: exit Play, deactivate the simulator, open the original desktop scene, enter Play, then choose `Tools → Jemparingan → Quest VR v1 → Run Desktop Input System Regression`. Inspect the new stabilization JSON for `testMouseIsCurrent`, `mousePressed`, `poseStringPinched`, `wasDrawing`, `lastDraw` and `shots` before identifying the cause.

Notes: The standalone Windows **Editor** path already uses transient XR startup. A Windows standalone **player build** additionally needs a build-specific configuration that enables XR startup; this patch fixes input selection but does not add or validate such a player build. Do not switch off XR for the smoke test or use legacy Oculus XR Plugin.

## Acceptance gates before merging

- Unity Editor compilation passes.
- Original desktop scene still uses mouse/keyboard; the Quest scene selects hands as expected in Editor.
- Re-run desktop Input System QA and inspect any remaining FAIL with instrumentation.
- Isolated smoke frame succeeds or failure is documented with exact host/runtime logs.
- Do not merge without explicit user approval.

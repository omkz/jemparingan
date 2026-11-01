# JEMPARINGAN — RASA: Quest hands-only integration gate

**Status:** v0.9 gameplay reportedly passed the user's Unity test on 31 Oct 2026. Quest integration has **not** been completed, compiled for Android, or validated on a physical headset. The desktop demo scene must remain intact.

## Technical baseline

- **Unity 6000.2.14f1**, as pinned in `ProjectSettings/ProjectVersion.txt`.
- Use **Unity OpenXR**, **not the deprecated Oculus XR Plugin**.
- Meta XR **All-in-One SDK / Core SDK** supplies `OVRCameraRig`, `OVRHand`, `OVRManager`, and `OVRPassthroughLayer`.
- The current repository `Packages/manifest.json` did **not** contain OpenXR, XR Plugin Management, or Meta XR packages as of this milestone. Do not assume dependencies are already installed.
- The Meta Quest adapter in `Assets/Scripts/MetaQuestBowInput.cs` is compiled against actual `OVRHand` **only with** `META_XR_SDK` defined. Otherwise it deliberately returns no hand pose.
- Current Quest aiming derives from a provisional hand-to-hand vector. Real ergonomic validation is mandatory; this must not be described as a production-ready input solution.

## First build on Linux Mint + Unity

1. In Unity Hub add **Android Build Support**, Android SDK/NDK and OpenJDK to the installed Unity Editor, if absent.
2. In Unity choose **File > Build Profiles** (Meta Quest or Android) and switch build target to Quest/Android. Keep a copy of your existing scene for desktop.
3. In **Edit > Project Settings > XR Plug-in Management**, install/enable **OpenXR** for Android. Follow XR Project Validation, and enable the Meta XR feature group under OpenXR.
4. In **Window > Package Manager**, install Meta XR All-in-One SDK (Package by Name: `com.meta.xr.sdk.all`) or the officially supported Core + Interaction SDK combination. Follow the Meta **Project Setup Tool**, resolve all required issues, and restart Unity if prompted.
5. Once imported, add `META_XR_SDK` to **Player > Other Settings > Scripting Define Symbols** for Android. This is a local project integration step because the SDK is currently absent from this repo. Only add the symbol once the SDK types resolve, or C# compilation will fail.
6. Confirm Unity Console has no errors **before building any Quest scene**.

Official references:
- https://developers.meta.com/vr/documentation/unity/unity-tutorial-hello-world-manual-steps/
- https://developers.meta.com/vr/documentation/unity/unity-xr-plugin/
- https://developers.meta.com/vr/documentation/unity/unity-handtracking-hands-setup/

**Linux note:** Meta Quest Link/PCVR editor workflows may be Windows-specific. Use USB ADB and Android Build & Run on a physical Quest from Linux to verify; do not assume Link is available on Linux.

## Keep desktop and Quest scenes separate

1. In Unity duplicate **`Assets/Jemparingan/Scenes/JemparinganOptimized_v02.unity`** using Save As or a scene copy, to **`Assets/Jemparingan/Scenes/JemparinganQuest_v10.unity`**. Do not overwrite the original desktop scene.
2. In the Quest copy, delete the old root-level **Main Camera** and add **OVRCameraRig** (Meta XR Core prefab).
3. Under `OVRCameraRig/TrackingSpace/LeftHandAnchor` add **OVRHandPrefab**. Under `RightHandAnchor`, add a second hand prefab, **set OVRHand/OVRSkeleton/OVRMesh to Right** as the Meta docs direct, and keep hand tracking visuals enabled. Set the rig tracking origin and hand tracking support for a comfortable seated interaction; the competition's gameplay must be **usable hands-only**, with no controller-only input needed.
4. On the `Jemparingan Game` object, locate **MetaQuestBowInput**, assign the `leftHand` and `rightHand` references to the corresponding OVRHand components. Keep `JemparinganGame` attached, and ensure there is only one instance.
5. On **OVRManager** enable hand tracking (**Hands Only** when testing the hands-first requirement) and enable **Passthrough Supported** / passthrough. Add **OVRPassthroughLayer** as an **Underlay**, and set the XR center camera background to black with **alpha 0** so the real room remains visible.
6. In **File > Build Profiles**, include only the Quest scene for the headset test build. Configure Android ARM64 and IL2CPP as directed by Meta's Project Setup Tool.
7. Check the active Quest scene using **Tools > Jemparingan > Quest > Validate Active Scene**. The audit is read-only, and logs missing SDK, missing rig/hands/passthrough, duplicate desktop camera, and missing left/right OVRHand references. Fix every blocker and review warnings before Build & Run.

The existing `BuildScene()` creates a miniature stylized arena **relative to `Camera.main`** at startup. Its HUD and aim are not yet spatially anchored, and the bow's hand-to-hand direction/geometry has not been calibrated on headset. Treat this as **first hardware feasibility**, not a finished Mixed Reality implementation.

## First physical Quest hands-only test — acceptance

1. Headset has hand tracking enabled. Start build with controllers put away; ensure passthrough shows the real room behind the arena.
2. Both rendered hands visible and tracking accurately; left-hand pinch establishes the bow, right-hand pinch pulls; *releasing right-hand pinch fires exactly once* when sufficiently drawn.
3. Force tracking loss briefly: game must **not** fire. After tracking resumes, allow a fresh draw and fire successfully.
4. Shoot at least 10 arrows consecutively, including hits and misses. Check arrows follow hands, **not the head gaze**.
5. Complete all 12 shots, see stage transitions, RASA bonus and results, and restart **without controllers**.
6. Check seated reach (no extended uncomfortable two-hand poses), UI distance, and render performance on actual Quest 3/3S.
7. Profile frame timing on device; **do not claim 60 FPS until measured**.

### Hard stop conditions

- Quest compilation errors or conflicting XR providers
- Two active cameras / broken stereo rendering
- Right hand produces left-hand tracking, or missing pinch input
- Accidental shot after tracking loss or while resetting
- No passthrough underlay, unreadable text, or out-of-reach interactions
- Missing launch/build settings or untestable APK

## After first hardware test

Move from this checklist to targeted fixes in **MetaQuestBowInput** and the world-space HUD/arena placement as needed. Do not add multiplayer, online ranking, or new game art before hands-only gameplay is reliable. Commit generated Unity `.meta` files when adding imported SDK assets/scenes, but never commit `Library/`, `Temp/`, or the built APK unless explicitly desired.

**Important:** The setup and actual APK still require access to the local Unity Editor, Meta XR packages, and a physical Meta Quest headset. No automatic action in the GitHub repo can substitute for those steps.

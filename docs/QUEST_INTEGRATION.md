# JEMPARINGAN — RASA / Quest Integration (first device APK)

**Stage:** preflight prepared on GitHub. **Not yet implemented/validated:** Android OpenXR provider, Meta SDK import, OVRCameraRig, hand prefabs, passthrough, Quest APK, real device playtests. Do not present desktop gameplay as a tested Quest app.

## Important: Linux Mint limitation

The developer uses Linux Mint. Meta's official requirements for its standalone development tooling list Windows and limited macOS support, **not Linux**. Meta has acknowledged and fixed some Linux SDK editor compilation issues in Core SDK **v205+**, but **Link and XR Simulator are not officially available on Linux**. Do not spend hackathon time trying to use Link on Linux. Prefer building an Android APK directly and physically testing it on the Quest, while treating Meta editor behavior on Linux as potentially unsupported. If the Meta SDK cannot compile or install on Linux, use a supported Windows environment for XR integration/build and keep the tested desktop scene untouched.

Refs:
- [Meta development requirements](https://developers.meta.com/vr/documentation/unity/unity-development-requirements/)
- [Meta investigation: Linux SDK support](https://developers.meta.com/vr/support/investigations/921649334062450/)
- [Meta Unity manual setup appendix](https://developers.meta.com/vr/documentation/unity/unity-tutorial-hello-world-manual-steps/)

## Step 0 — Lock desktop acceptance first

- Pull `main` and check no red Unity Console errors.
- Run **Tools > Jemparingan > Run Gameplay QA v0.9** (expected 21 pure-rule/hitbox checks).
- Complete **three full 12-shot Editor rounds**, including miss, RASA, stage transitions, result panel, R restart and gesture replay. **User confirmation of compiler success is not evidence these rounds have passed.**
- Commit the tested Unity-generated `.meta` assets with their corresponding scripts when possible; `Library/`, `Temp/`, `Logs/` stay ignored.

## Step 1 — Install supported build environment

1. Use Unity **6000.2.14f1** (as pinned in this repository).
2. In Unity Hub, add **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK** to that Editor installation.
3. In **File > Build Profiles**, install/enable **Meta Quest** (or Android), then switch to it.
4. Install **XR Plug-in Management** and **Unity OpenXR Plugin** through Unity Package Manager if missing.
5. In **Edit > Project Settings > XR Plug-in Management**, enable **OpenXR** under the Android/Quest tab, and run project validation.
6. In Android OpenXR settings, enable required **Meta Quest Support** and **Hand Tracking** features for the selected package versions. Check the current feature-group names; they vary with XR package version.
7. In **Window > Package Management > Package Manager**, the official Meta guide offers **Add Package By Name: `com.meta.xr.sdk.all`**. Choose a Meta XR SDK version compatible with Unity 6 and OpenXR, not the deprecated Oculus XR Plugin. Read and address package errors before proceeding.
8. Run **Window > Meta > Tools > Project Setup Tool** and resolve all Required/Outstanding Issues for the Android target. Review optional suggestions separately.

Refs:
- [Unity XR Plug-in Management on Quest](https://developers.meta.com/vr/documentation/unity/unity-xr-plugin/)
- [Official Meta manual project setup](https://developers.meta.com/vr/documentation/unity/unity-tutorial-hello-world-manual-steps/)

**Do not add SDK-specific scripting symbols blindly.** Existing `MetaQuestBowInput.cs` uses `#if META_XR_SDK` to keep desktop Unity compilable without SDK. Define `META_XR_SDK` **only after** Meta XR Core SDK is installed and `OVRHand` resolves; otherwise the symbol will cause compile errors.

## Step 2 — Create a separate Quest scene (do not overwrite the desktop scene)

1. Keep `Assets/Jemparingan/Scenes/JemparinganOptimized_v02.unity` as the known desktop baseline.
2. Duplicate the scene **inside the Unity Editor** to create `Assets/Jemparingan/Scenes/JemparinganQuest.unity`; keep its matching `.meta` file.
3. Remove the desktop **Main Camera** from the Quest scene. Keep **Jemparingan Game**, its scripts and stage light.
4. Add one **OVRCameraRig** prefab (or Meta's official Camera Rig Building Block). Ensure exactly one active main XR camera, an appropriate `MainCamera` tag, and one AudioListener.
5. Add hand prefabs/components under the rig: left under **LeftHandAnchor**, right under **RightHandAnchor**. Assign correct hand type and inspect that both hands are tracked.
6. Enable hand tracking in the rig/OVRManager; allow hands end-to-end, without requiring controllers. Prefer Meta's documented Hand Tracking Building Block and verify its resulting setup.
7. Add **Passthrough Building Block** (or compatible Meta passthrough layer) in the Quest scene. Configure camera/passthrough background according to current Meta docs. The physical environment must remain visible and the virtual pendopo must be spatially meaningful.
8. Select **Jemparingan Game > MetaQuestBowInput** and assign the left and right `OVRHand` component references.
9. Enable the `META_XR_SDK` scripting define in the Android player settings **only when Core SDK has been successfully imported**. This is required for the existing conditional OVRHand adapter; do not define it in the plain desktop project.
10. Add **JemparinganQuest.unity** to the Build Profiles scene list and use the Quest scene for Android builds.

Refs:
- [Meta hand prefabs / anchors](https://developers.meta.com/vr/documentation/unity/unity-handtracking-hands-setup/)
- [Meta passthrough getting started](https://developers.meta.com/vr/documentation/unity/unity-passthrough-gs/)
- [Meta Building Blocks](https://developers.meta.com/vr/documentation/unity/unity-building-blocks-overview/)

## Step 3 — Check before Android Build & Run

Use **Tools > Jemparingan > Check Quest Prerequisites** after opening the Quest scene. It reports:
- Active build target (Android), XR management/OpenXR package presence, Meta Core SDK/rig assets;
- Enabled build scene and whether the active scene appears to contain OVRCameraRig;
- Multiple active/inactive cameras in the open scene.

Warnings are **setup hints**. This tool cannot verify OpenXR feature groups, correct OVRHand binding, passthrough display, SDK runtime compatibility or actual performance. Run the **Meta Project Setup Tool** and headset checks separately.

## Step 4 — First APK / hands-only smoke test

1. Connect the Quest 3/3S via USB-C with **Developer Mode** enabled. Authorize the device for USB debugging.
2. Open **File > Build Profiles > Meta Quest (Android)** and choose **Development Build** for the first APK, then **Build and Run** (or Build APK and install with ADB if supported in your environment).
3. In the headset, with controllers set aside and hand tracking enabled, check:
   - Passthrough shows the real room; pendopo is readable, not opaque black.
   - Both hands are tracked. Left pinch holds the bow; right pinch pulls the string; **one release = one arrow**.
   - Losing tracking never fires accidentally.
   - Aiming uses hand/arrow geometry, not solely gaze direction.
   - A full 12-shot round can finish and replay *using hands only*.
   - No visual or audio discomfort; verify measured frame timing and report it accurately.
4. Record actual issues and a short gameplay clip, not only a desktop screen recording. Prioritize interaction blockers over additional art.

[Official Meta Android build guide](https://developers.meta.com/vr/documentation/unity/unity-build/)

## Go / no-go

**Go:** desktop QA and real rounds passed; Quest scene builds and launches on headset; hands-only bow/arrow loop and passthrough work; no accidental shots on tracking loss; target/score/results remain readable seated.

**No-go:** project compile errors, absent XR provider, duplicate cameras, gesture requiring controllers, passthrough not visible, inability to finish a round, or claims of Quest readiness without a device test.

The competition's final APK/release-channel/Devpost obligations are tracked separately. Do **not** mistake the new preflight menu for an SDK installer or automatically ready-to-build scene.

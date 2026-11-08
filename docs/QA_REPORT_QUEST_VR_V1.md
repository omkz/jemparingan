# Quest VR Scene v1 — QA report

Date: 2026-11-09 (Asia/Jakarta). **Scene and Editor runtime foundation verified. Device hand tracking, stereo output, comfort and performance remain untested.**

## Checkout and preserved work

- Repository: `omkz/jemparingan`; baseline `468149c` on `chore/urp-migration-v1`.
- Feature branch created: `codex/quest-vr-scene-v1`.
- The checkout already contained uncommitted Unity/URP upgrades, Meta XR packages, XR settings, and generated SDK resources. These were retained. No reset, stash, scene regeneration, merge, or PR closure was performed.
- Original scene: `Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity`. Its file bytes match the initial backup; Git reports no content change from the committed scene. Windows checkout SHA-256: `3dc62cc8b8818fb842484d695932f0e3b4dcc2a2ed1d945499189be557f6d623`.
- Unity: **6000.3.27f1**, Windows. Resolved dependencies: URP **17.3.0**, XR Management **4.7.0**, Unity OpenXR **1.16.1**, Meta XR All-in-One/Core **207.0.1**, XR Hands **1.7.2**, Input System **1.20.1**. The deprecated Oculus XR loader is not used.

## Actual results

| Check | Status | Evidence / limits |
|---|---|---|
| Isolated feature branch | PASS | Branch created from `468149c`, preserving the dirty checkout. |
| Installed packages compile before scene construction | PASS | Editor initially compiled; adapter/setup compilation also completed successfully after correcting the Meta project-config API. Final MCP probe: `isCompiling=false`, `scriptCompilationFailed=false`. |
| Android tooling installed | PASS | Editor reports Android supported. Installed SDK platforms: 34, 35, 36, 37.0; NDK 27.2.12479018; OpenJDK 17.0.18. Tool presence does not prove a successful build. |
| Android Unity OpenXR loader | PASS | Assigned `UnityEngine.XR.OpenXR.OpenXRLoader`, initialize on startup enabled. |
| Stereo and platform configuration | PASS | Single Pass Instanced, Vulkan, ARM64, IL2CPP, minimum Android API 32, application ID `com.omkz.jemparingan`. |
| Meta/hand OpenXR features | PASS | Baseline configuration enables MetaXRFeature, MetaXRFoveationFeature, HandTracking, MetaHandTrackingAim, HandInteractionProfile and disables Android controller profiles. The current local checkout also has a user change enabling the Android Oculus Touch profile; it is preserved and excluded from the simulator fix commit. Reapply the hands-only configuration before a submission build. |
| Hands-only / no passthrough configuration | PASS | Meta project configuration: HandsOnly, Quest 3/3S targets, passthrough None. No passthrough implementation added. |
| OpenXR project validation | PASS | Zero reported Android issues; see `validation/quest-vr-v1/configuration.json`. |
| Desktop original scene preservation | PASS | Original scene file remains unchanged; file hash matches baseline. |
| Existing gameplay rules | PASS | Unity MCP ran existing gameplay QA: **38/38** deterministic rule/hitbox checks passed. Does not replace Play Mode input testing. |
| Existing Traditional target zones | PASS | Existing molo, jangga, awak, and miss checks passed through Unity MCP. |
| Interrupted gesture gate | PASS | **17/17** synthetic checks execute the production C# gate through PowerShell Add-Type. Startup-held input, invalid tracking, reacquisition, dropping the bow with string held, and explicit interruption require neutral input before another gesture. No device input was tested. See `validation/quest-vr-v1/gesture-gate.json`. |
| Separate `JemparinganQuest.unity` scene | PASS | Saved at `Assets/Jemparingan/Scenes/JemparinganQuest.unity`, with its own `.meta` GUID. Original scene preserved. |
| One XR camera / AudioListener and two tracked hands in saved Quest scene | PASS | One OVRCameraRig, center-eye camera enabled for URP XR, monoscopic disabled, per-eye cameras disabled, one active AudioListener, left/right OVRHand assignments, no missing scripts or enabled desktop input. See `scene-validation.json`. Stereo output itself requires hardware. |
| Environment/material preservation in Quest scene | PASS | All **561 original transforms**, their active states, renderer material references and light settings match the desktop scene. Copied scene has 579 transforms including the new XR hierarchy. See `environment-preservation.json`. |
| Quest Editor visual smoke check | PASS | Seated camera and both gameplay modes captured and inspected: `quest-seated-edit.png`, `quest-traditional-play.png`, `quest-modern-play.png`. Runtime reports zero unsupported/error materials. No claim of device stereo or unchanged shared color-space rendering. |
| Traditional/Modern runtime rounds with Quest rig | PASS | **12 shots per mode**, hit zones/rings, misses, all three stages and results passed in Play Mode. Near-target `Shoot` injection exercises actual projectile Update/collision/scoring, bypassing gestures and aiming. See `runtime-checks.json`. |
| Ten consecutive desktop input-driven shots / exactly one arrow per release | FAIL (latest retry) | The original foundation run passed **10 releases per mode**, retained in `desktop-input-regression.json`. The 2026-11-09 simulator-fix retry selected the menu but recorded zero shots for all releases and failed held-reset checks; see `desktop-input-regression-retry.json`. Cause is not yet established. Desktop Play initially reported no active XR loader and its scene file is unchanged. Do not treat the earlier PASS as proof that this dirty checkout currently passes interaction regression. |
| Exactly one arrow per tracked-hand pinch release | NOT TESTED | Gesture gate checks and unavailable-hand draw cancellation pass, but no end-to-end tracked pinch/device test. |
| Quest-only Editor OpenXR startup | PASS | `JemparinganQuestEditorXR` supplies transient Standalone startup settings for the Quest scene. Actual trials identify Meta XR Simulator and reach `XR_SESSION_STATE_FOCUSED`; saved desktop startup stays disabled. Standalone hand features enabled. |
| Meta XR Simulator rendering on this host | FAIL | Simulator 207.1 on Intel UHD 620 / driver 31.0.101.2135: Direct3D 11 stalls before first frame; direct Vulkan frontend reports memory allocation `-2` and swapchain-image creation failure, then disconnects. Compatibility transfer and Operator opt-out trials did not fix it. See `validation/quest-vr-v1/simulator-checks.json` and `SIMULATOR_SETUP.md`. |
| Simulator end-to-end left bow / right string gestures | NOT TESTED | Both simulator inputs set to Hand with shared input disabled, but no rendered frame or successfully verified tracked pinch shot. |
| Android development APK | FAIL | Both-input warning fixed, but the subsequent actual build failed with one error / nine warnings: `TaskCanceledException: A task was canceled.` No APK exists at the requested output. See `validation/quest-vr-v1/android-build.json`. |
| Headset/device/performance validation | NOT TESTED | Elevated `adb devices -l` returned an empty device list. No device deployment, real hand tracking, stereo image, comfort or frame-rate measurements. |

## Implementation

`Assets/Scripts/Editor/JemparinganQuestSetup.cs` exposes explicit menus under **Tools → Jemparingan → Quest VR v1**:

1. **Configure Android OpenXR** — executed. Configures Android separately, and disables automatic XR startup for Standalone so the desktop scene can launch without an HMD.
2. **Create Quest Scene Copy** — executed. Copies the authored scene with `AssetDatabase.CopyAsset`; refuses to overwrite an existing Quest scene or discard unsaved scenes. It preserves environment/target content, disables desktop input and the original camera/listener only in the copy, and adds the SDK `OVRCameraRig` and two `OVRHandPrefab` instances. Uses OpenXR hand skeleton/mesh types and a separate URP material for the SDK-rendered hands.
3. **Validate Open Quest Scene** — executed, PASS. Checks URP XR rendering, monoscopic off, one active listener, one rig, both adapter hand assignments, no enabled desktop input, no passthrough, missing scripts, Android validation errors, and the desktop scene hash (also accepts canonical LF content on other checkout platforms). Writes `scene-validation.json`.
4. **Build Android Development APK** — validates first and builds an explicit Quest-only scene list to `Builds/QuestVRv1/JemparinganQuest-dev.apk`; does not replace the default desktop build scene. The default scene list retains desktop enabled and Quest disabled. Use this menu or an explicit Quest-only scene list, not an ordinary desktop build. The automated attempt uses Unity MCP `manage_build` with that same explicit list.
5. **Configure Editor Simulator OpenXR** — enables Standalone OpenXR and hand features. Editor uses Standalone XR settings even with Android selected. The saved desktop startup flag is retained; the Quest-only Editor hook uses transient settings and cleans them up on Play exit/reload. This fixes missing XR initialization but does not resolve the simulator graphics failure on this host.

The seated origin is the existing authored viewpoint `(0, 1.35, 1.25)`, with EyeLevel tracking and full positional/rotational tracking. This avoids adding physical floor-to-eye height twice. The center-eye camera is configured for URP XR; the per-eye cameras are disabled. Comfort and recenter behavior require a headset test.

The hand adapter now rejects invalid/low-confidence tracking, system gestures, inactive hand components, application pause/focus loss, and invalid hand separation. It requires both pinches neutral after interruption. While drawing, aim comes from the hand-to-hand axis independently of head direction. Releasing the string retains that last valid direction. The game clears draw state on disable and suppresses its desktop cosmetic hands on the Quest input route. Existing desktop input routing remains in place.

Both gameplay modes remain in the existing game code. The Quest scene installer preserves `initialOutdoorMode`; this milestone does not add a new hands-operated mode-selection menu. The default is Traditional. Modern can be selected through the serialized initial mode or existing `ChooseMode` API when testing.

Android requires a single input backend. `JemparinganDesktopControls` routes the existing desktop mouse buttons, menu/reset keys and mouse-look axes to Input System while retaining a legacy fallback for projects without it. The original desktop scene remains unchanged; only two source aliases connect the existing gameplay to this bridge. Mouse X/Y retain the legacy project's 0.1 axis sensitivity.

## Limitations and next steps

The separate Quest scene exists and validates successfully. The Android build was attempted and failed; APK availability remains blocked pending a successful rebuild.

Shared project settings now include the existing Unity 6.3 migration and Meta/OpenXR initialization changes. Unity/Meta also serialized renderer/quality settings and linear color-space/stereo settings during setup. The desktop scene hash alone does **not** prove unchanged desktop rendering; perform a visual regression check before claiming desktop visual parity.

Next hands-only interaction step: on Quest 3/3S, calibrate the actual bow grip/string pinch positions, comfortable draw distance and aim assist. Verify a normal release produces exactly one arrow; losing either hand, invoking a system gesture, removing the headset, pausing, or losing focus produces zero arrows and requires both pinches released before rearming. Test miss recovery, moving targets, replay, both gameplay modes and a hands-operated mode selector. Record measured frame times and actual device video.

## Reproduction and attribution

- `Tools/Invoke-UnityMcp.ps1` talks to the installed Unity MCP Editor TCP bridge, discovering its port from per-project metadata (fallback 6400), using its documented framing from package source. Domain reload can stop the bridge; port discovery does not restart it. Editor scene operations and existing QA menus use MCP. The local Pipeline bridge was used to start MCP and bootstrap compile symbols because no Unity MCP tool was initially exposed in the chat.
- `Tools/Invoke-Unity.ps1` discovers the local Pipeline descriptor and uses its token internally; it does not print credentials. `Tools/QuestVrBootstrap.cs` preserves the desktop hash and enables `META_XR_SDK` for Android/Standalone.
- Run logic checks: `./Tools/Test-QuestGestureGate.ps1`.
- Run preservation/runtime checks through the additional Quest VR v1 menus; the desktop input regression menu expects Play Mode in the original desktop scene. Tests use virtual Input System devices and restore them afterward.
- New hand prefabs/components reference the installed **Meta XR Core SDK**, under its [Oculus SDK License](https://developers.meta.com/horizon/licenses/oculussdk/). No SDK source, meshes or package assets were copied into the repository. Joglo/environment attribution is unchanged.
- Official references: [Meta camera rig](https://developers.meta.com/vr/documentation/unity/unity-ovrcamerarig/), [hand setup](https://developers.meta.com/vr/documentation/unity/unity-handtracking-hands-setup/), [manual OpenXR project setup](https://developers.meta.com/vr/documentation/unity/unity-tutorial-hello-world-manual-steps/).
- Input API reference: [Unity's migration guide](https://docs.unity.cn/Packages/com.unity.inputsystem%401.13/manual/Migration.html).

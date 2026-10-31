# JEMPARINGAN — RASA / Unity Starter Kit

**Status: source-code prototype, NOT a compiled APK or validated Quest build.**

Goal: verify the core loop before creating ornate 3D assets: hold a virtual bow, pull a string, release an arrow, hit a swinging bandulan, and score.

## 1. Create Unity project

- Recommended: **Unity 6.1+**, **Universal 3D (URP)** template and Android Build Support.
- In Unity: `Edit > Project Settings > Player > Other Settings > Active Input Handling`: choose **Both**, for the desktop debug input.
- Copy the `Assets/Jemparingan` directory from this archive into your Unity project's `Assets/` folder.
- In Unity, choose **Tools > Jemparingan > Create Prototype Scene**. Save and press Play.
- Controls: Hold **left mouse button** to pull the string (longer hold = more power), **release** to shoot. Hold **right mouse button** and move mouse to aim. Press **R** to reset score.
- Generated geometry is placeholder-only. A hit increments the score.

## 2. Meta Quest setup (after the desktop loop works)

Follow the current official Meta project setup documentation:
https://developers.meta.com/vr/documentation/unity/unity-tutorial-hello-world-manual-steps/

- Enable Unity OpenXR on Android and Meta Quest features (do **not** use deprecated Oculus XR Plugin).
- Add Meta XR All-in-One SDK using Package Manager (`com.meta.xr.sdk.all`) and fix required warnings in `Window > Meta > Tools > Project Setup Tool`.
- Add `META_XR_SDK` under Player > Scripting Define Symbols for Standalone and Android, after installing the SDK.
- **Replace the prototype Main Camera** with the Meta `OVRCameraRig`, mark its center-eye camera as **MainCamera**, and add OVRHand prefabs for left/right anchors per Meta documentation.
- Select the `Jemparingan Game` object and link `MetaQuestBowInput.leftHand` and `.rightHand` to the respective OVRHand components.
- On `Jemparingan Game`, enable `Prefer Quest Hands In Editor` if testing via Quest Link or simulator in the Unity Editor. In a Quest APK it selects Quest hands automatically.
- Configure passthrough on the rig with `OVRPassthroughLayer` per Meta docs. This starter **does not configure passthrough automatically**.
- Build using the Meta Quest Android profile, validate hand tracking and arrow release on device. Do not submit the kit as-is.

Meta hand setup docs:
https://developers.meta.com/vr/documentation/unity/unity-handtracking-hands-setup/

Meta passthrough docs:
https://developers.meta.com/vr/documentation/unity/unity-passthrough-gs/

## 3. Important limitations

- The aiming vector is currently the headset's forward direction. Next step: derive the bow's orientation from both hands, add target assist, and validate ergonomics.
- Bandulan is procedural / simplified rather than a historically accurate 3D asset.
- No hand-tracking confidence gating beyond tracked status, no onboarding, physics/audio polish, accessibility, local high-score storage, or actual Quest QA yet.
- Scripts have been statically checked for file completeness but **cannot be runtime tested here without Unity**.
- Quest XR Simulator officially supports Windows and Apple Silicon macOS; Linux desktop is not a supported simulator OS in Meta docs. Linux users should validate options before relying on simulator.

## 4. Hackathon checklist

- Competition: https://start-developer-competition-26.devpost.com/
- Deadline: **18 Nov 2026, 12:00 PST (19 Nov 03:00 WIB)**.
- **Hands-first end-to-end** is required; mouse is a prototype-only input.
- Unity build: APK in release channel `Competition`, then submit **Invite URL** to Devpost.
- Video under 3 minutes, public YouTube/Vimeo URL, and submission form.
- Must have Meta Developer Access and Meta VR Start membership eligibility.
- Only Quest 3/3S performance is judged, target >=60 fps.

## 5. Stop/go test

**Pass:** Two-hand pinch works for 10 consecutive shots in a real Quest/simulator and a new player hits the bandulan at least once without a long tutorial. **Fail:** Hand occlusion or UI prevents finishing the complete loop. Fix this before investing in art.

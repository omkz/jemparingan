# Instructions for coding agents — JEMPARINGAN: RASA

You are building a **Meta Quest 3 / Quest 3S mixed-reality archery game** for the 2026 Meta VR Start Developer Competition.

## Hard constraints
- Unity 6.1+, URP, **Unity OpenXR** (not deprecated Oculus XR Plugin).
- Hands-first end-to-end. On Quest both-hand pinch/gesture must work without controllers.
- Make the experience comfortable while seated; keep gestures within a small reach envelope.
- Scope a 3-minute polished loop first. No unnecessary backends, logins or AI services.
- Maintain >= 60 FPS on Quest 3/3S where possible; do not claim performance without measurements.
- Use assets with clear usage rights and preserve source/license attribution.
- Do not claim working APK or Quest testing unless actually built/tested.

## Current structure
- `DesktopBowInput` = editor mock input. NOT intended for Quest submission.
- `MetaQuestBowInput` = OVRHand pinch adapter; active only with `META_XR_SDK` defined after installing Meta SDK.
- `JemparinganGame` = generates placeholder bandulan and arrows; input and basic scoring.
- `JemparinganArrow` = minimal projectile mechanics.

## Build order
1. Confirm Unity project compiles on target Editor version. Fix errors without switching to obsolete SDKs.
2. Set up `OVRCameraRig`, Meta hand prefabs, and passthrough (official Meta instructions); ensure there is no duplicate camera.
3. In headset/simulator, validate left pinch holds bow and right pinch pulls/releases, including tracking-loss fallback.
4. Make bow aiming independent of head direction, with forgiving assist and clear visual cue.
5. Add proper bandulan, arena/pendopo stylized low-poly art, audio and onboarding. Prioritize polish over feature count.
6. Run on device with no controller; record actual gameplay and produce APK for Meta release channel `Competition`.

## Acceptance tests for stage 1
- Scene opens, editor can fire 10 consecutive arrows; hit increments score.
- Quest end-to-end interaction works with hands only; controls never require a controller.
- Pinch release fires exactly one arrow. Tracking loss never fires an unintended shot.
- Hit detection is robust for a moving target; gameplay continues after a miss.

## Links
- Competition: https://start-developer-competition-26.devpost.com/
- Unity setup: https://developers.meta.com/vr/documentation/unity/unity-tutorial-hello-world-manual-steps/
- Hands: https://developers.meta.com/vr/documentation/unity/unity-handtracking-hands-setup/

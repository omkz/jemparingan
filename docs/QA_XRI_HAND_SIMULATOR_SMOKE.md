# Jemparingan XR Hands — Editor playtest

**Goal:** simulate hand pinches and fire real Jemparingan arrows in Linux/Windows Unity Editor. This is **not** headset QA.

## Setup

1. Check out `feat/unity-xri-hand-simulator-smoke`. Unity 6000.3.27f1 resolves **XR Interaction Toolkit 3.5.1** and **XR Hands 1.8.1**.
2. In **Package Manager > XR Interaction Toolkit > Samples**, import **Starter Assets**, **Hands Interaction Demo**, **XR Interaction Simulator** (not Legacy). In **XR Hands > Samples**, import **HandVisualizer**. Sample assets remain local.
3. In Unity, run **Tools > Jemparingan > Simulator > Create/Open Jemparingan XRI Playtest**. It copies `JemparinganQuest.unity` once to `JemparinganXRIPlaytest.unity`, replaces only the camera/input rig in the copy, and **never overwrites** either existing scene. Do not run the Meta XR Simulator.
4. Press **Play** and focus the **Game** window. Check the Console for `Jemparingan input route: XR Hands`. Use the simulator overlay: press `[` twice to select left **Hand**, `]` twice for right **Hand**, `M` for pinch, and `WASD` to reposition the selected device. Start with both pinches released.
5. Left pinch holds the bow; move the right hand back while pinching to draw, then release the **right** pinch to shoot. Switch Traditional/Modern using the serialized `initialOutdoorMode` on `JemparinganGame` in the **playtest copy**, not the Quest source.

**Pass:** both hands are tracked, bow follows left hand, right pinch draws, right release shoots once, scoring advances, and losing tracking does **not** fire. Also recheck the desktop scene separately before merge.

## Notes

- The original **desktop** and **Quest** scenes, Meta `OVRHand` adapter, and Android configuration remain untouched. The XRI input adapter is active **only when attached to a game object** (here, in the generated playtest copy).
- If an older controller-only `JemparinganXRIHandSmoke.unity` exists, use **Tools > Jemparingan > Simulator > Upgrade Open XRI Smoke Scene to Hands**. That Cube scene tests hand interaction only; use the **Jemparingan XRI Playtest** to test archery.
- Meta SDK may still log `DllNotFoundException: OVRPlugin` through MRUK on Linux; that is separate from the XR Hands input test. Do not modify `Library/PackageCache`.
- No Unity Editor compilation, end-to-end hand shot, Android build or headset test has been run by the GitHub code changes alone. Validate before merging.

# Jemparingan Editor playtest

Unity 6000.3; `feat/unity-xri-hand-simulator-smoke`. This is **Editor QA only**, not a Quest headset test.

## Setup

1. Import **Hands Interaction Demo**, **XR Interaction Simulator**, and **Starter Assets** from XR Interaction Toolkit samples; import **HandVisualizer** from XR Hands samples.
2. Run **Tools > Jemparingan > Simulator > Create/Open Jemparingan XRI Playtest**. It creates a separate copy of the Quest scene once. Existing scenes are never overwritten.
3. **Stop Play** before switching input. Use one of the following menus (the choice is saved in the playtest scene):

| Mode | Menu | Test |
| --- | --- | --- |
| **Keyboard (easier)** | **Use Spacebar Test Mode** | Focus Game; **hold Space** ~0.6s to draw, **release** to fire |
| **XR Hands** | **Use XR Hands Mode** | Left pinch holds bow; right pinch + hand movement draws; right release fires |

Spacebar mode uses an **Editor-only** input adapter. It temporarily disables the XRI simulator to avoid its own `Space` shortcut; the actual projectile physics, targets, score and Traditional/Modern modes are unchanged. No hand tracking is tested in keyboard mode.

To test **Traditional** or **Modern**, set `initialOutdoorMode` on `JemparinganGame` in the playtest scene Inspector before Play. The camera stays at the simulated seated viewpoint.

## XR Hands controls

Focus Game and confirm both simulated devices are **Hands** in the on-screen XRI overlay. The default overlay shows left **Shift+M** and right **M** for pinch. Move the selected hand with **WASD**. Start with both pinches released. Disabling or losing tracking cancels a draw; it never fires.

## Checks

- Keyboard: hold/release yields **one arrow per release**; `ARROWS` and score update; short presses do not shoot.
- XR Hands: left pinch activates bow; right pinch draws; release shoots once; tracking loss cancels safely.
- Desktop gameplay remains unchanged. Original Quest scene, Meta `OVRHand` adapter, and Android settings remain unchanged.

**Known:** Meta native plugins may still log `OVRPlugin`/`MetaXRAudioUnity` errors on Linux independently of Editor keyboard input. Do not modify PackageCache.

**Not yet verified:** Unity recompilation, an end-to-end Spacebar shot, Android build, or Quest device runtime.

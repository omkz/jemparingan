# Jemparingan XR Hands — Editor playtest

Use Unity 6000.3 on branch `feat/unity-xri-hand-simulator-smoke`. This is an **Editor test**, not a Quest headset test.

1. Import XR Interaction Toolkit samples **Starter Assets**, **Hands Interaction Demo**, and **XR Interaction Simulator** (not Legacy); import **HandVisualizer** from XR Hands.
2. Run **Tools > Jemparingan > Simulator > Create/Open Jemparingan XRI Playtest**. The playtest is a separate copy of the Quest scene, created once; existing scenes are not overwritten.
3. Press **Play**, focus Game, and confirm both simulator devices show **Hand** in the overlay. Start with both pinches released.
4. **Left pinch (Shift+M)** holds the bow; **right pinch (M)** holds the string; move the right hand backward with the simulator's movement keys; release right pinch to fire. Use the simulator overlay for current bindings.
5. Set `initialOutdoorMode` on `JemparinganGame` in the playtest scene to test **Traditional** or **Modern**.

**Pass:** holding the left pinch shows the bow; right pinch draws; releasing right once fires at most one arrow; score and arrows update; tracking loss cancels the draw without shooting.

**Notes:** This test reuses real projectile physics and scoring. The **desktop mouse controls** remain the straightforward way to test gameplay without gestures; a second keyboard shooting mode is intentionally not included. The original Quest scene, OVRHand adapter, Android settings, and desktop scene are unchanged. Meta native plugins may log `OVRPlugin`/`MetaXRAudioUnity` errors in Linux Editor. Do not modify PackageCache.

**Not verified:** Unity compilation after latest changes, end-to-end XR pinch shots, Android APK, or a real Quest device.

# XR Hands playtest (Unity Editor)

Open the existing `Assets/Jemparingan/Scenes/JemparinganXRIPlaytest.unity` locally on branch `chore/jemparingan-cleanup-v1`. To create it the first time, use **Tools > Jemparingan > Simulator > Create/Open Jemparingan XRI Playtest**. Original desktop and Quest scenes are not overwritten.

**Requires:** XR Interaction Toolkit 3.5, XR Hands 1.8; import XRI samples **Starter Assets**, **Hands Interaction Demo**, **XR Interaction Simulator**, and XR Hands sample **HandVisualizer**.

## Test

1. Press **Play**; focus Game view. Confirm the simulator overlay shows **Hand** for both left and right devices.
2. Begin with **both pinches released**. Press **Shift+M** for left pinch: the bow should appear.
3. Press **M** for right pinch. Move the right hand backward (select with `]`, then `S`) while keeping left pinch held; a close starting position should no longer cancel the gesture.
4. Draw at least 10 cm, then press **M** again to release right pinch. Expect **one shot**; repeat in Traditional and Modern.
5. Release both pinches and retry. Interrupt a draw by disabling a simulated hand; expect **no shot**, then release both pinches before starting again.

## Feedback

- **PULL RIGHT HAND BACK** — hands are less than 8 cm apart; move the right hand without restarting the gesture.
- **RELEASE BOTH PINCHES** — interrupted gesture must return to neutral before another shot.
- **LEFT/RIGHT HAND NOT TRACKED** — activate the indicated simulated hand.
- **LEFT/RIGHT FINGERS NOT TRACKED** — pinch-joint data is unavailable.
- **HANDS TOO FAR - RESET PINCHES** — hand separation exceeds 1 m; release both and reposition.

The desktop mouse mode is the existing gameplay test; this scene tests **XR gesture input**, not a second keyboard shooting scheme. Meta native plugin errors on Linux may still be unrelated. **Editor simulation is not proof of an Android APK or Quest device tracking.**

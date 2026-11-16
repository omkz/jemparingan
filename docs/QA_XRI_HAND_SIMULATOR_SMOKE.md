# XR Hands playtest (Unity Editor)

On branch `chore/jemparingan-cleanup-v1`, stop Play Mode and run **Tools > Jemparingan > Simulator > Create/Open Jemparingan XRI Playtest**. This creates or opens the **new** `JemparinganXRIOnly.unity` from the desktop scene. The old Meta/Quest/XRI scenes are untouched. Expect `XRI PLAYTEST READY` in Console.

**Requires:** XR Interaction Toolkit 3.5, XR Hands 1.8; import XRI samples **Starter Assets**, **Hands Interaction Demo**, **XR Interaction Simulator**, and XR Hands sample **HandVisualizer**.

## Test

The XR-only Editor scene reuses the existing **Hold bow with left hand** text location for contextual steps (hold bow → pinch string → draw/aim → release). There is no extra simulator panel. After a real shot, that label briefly shows **SHOT #N LAUNCHED**, and the Console logs `Jemparingan XR: SHOT #N LAUNCHED`. **DRAW 100% alone does not mean a shot fired**.

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

The desktop mouse mode remains the gameplay baseline. This scene tests **XR gesture input**, not a second keyboard shooting mode. The Meta XR SDK is not required. **Editor simulation is not proof of an Android APK or Quest device tracking.**

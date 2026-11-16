# XR Hands playtest (Unity Editor)

On branch `chore/jemparingan-cleanup-v1`, stop Play Mode and run **Tools > Jemparingan > Simulator > Create/Open Jemparingan XRI Playtest**. This creates or opens the **new** `JemparinganXRIOnly.unity` from the desktop scene. The old Meta/Quest/XRI scenes are untouched. Expect `XRI PLAYTEST READY` in Console.

**Requires:** XR Interaction Toolkit 3.5, XR Hands 1.8; import XRI samples **Starter Assets**, **Hands Interaction Demo**, **XR Interaction Simulator**, and XR Hands sample **HandVisualizer**.

## Test

When the **XR Hands input adapter is active** in an Editor playtest (including older local XRI playtest scenes), the game reuses the **Hold bow with left hand** world-space text location; no separate panel appears. Unity Console logs `Jemparingan XR contextual guide: ENABLED` to confirm activation. It shows two-line contextual steps: **Shift+M** to hold the bow, **M** to grab the string, **]** to select the right hand and **S / A / D** to draw/aim, then **M** to release the right pinch while the left remains held. Tracking and invalid gesture messages include recovery instructions (release both pinches to reset). After a real shot the same label briefly displays **SHOT #N LAUNCHED**, with a matching Console log. **DRAW 100% does not confirm a shot.**

1. Press **Play**; focus Game view. Confirm the simulator overlay shows **Hand** for both left and right devices.
2. Begin with **both pinches released**. Press **Shift+M** for left pinch: the bow should appear.
3. Press **M** for right pinch. Move the right hand backward (select with `]`, then `S`) while keeping left pinch held; a close starting position should no longer cancel the gesture.
4. Draw at least 10 cm, then press **M** again to release right pinch. Expect **one shot**; repeat in Traditional and Modern.
5. Release both pinches and retry. Interrupt a draw by disabling a simulated hand; expect **no shot**, then release both pinches before starting again.

## View and aiming

After XR simulated tracking initializes, the Editor playtest recenters the simulated eye at the authored seated viewpoint (1.35 m above the floor in outdoor v05), aligns yaw and pitch toward the bandulan, and logs `Jemparingan XR view: SEATED | eye Y=... | target Y=... | aim error=... deg`. To recenter again during Play Mode, use **Tools > Jemparingan > Simulator > Recenter Seated XR View (Play Mode)**. This is Editor-only and does not modify the scene or real Quest tracking. If the disabled desktop MainCamera reference is missing, calibration is skipped with a warning.

For manual HMD adjustment in the XRI 3.5 simulator: press `H` to select the HMD, use the arrow keys (pitch/yaw) or hold the right mouse button and move the mouse to look down toward the target; `R` resets the selected device. Switch to the right hand (`]`) to move the string hand again. The XR Editor also temporarily shows larger trajectory dots while drawing. The dots indicate the actual projectile path, **not** the headset's central gaze.

## Feedback

- **PULL RIGHT HAND BACK** — hands are less than 8 cm apart; move the right hand without restarting the gesture.
- **RELEASE BOTH PINCHES** — interrupted gesture must return to neutral before another shot.
- **LEFT/RIGHT HAND NOT TRACKED** — activate the indicated simulated hand.
- **LEFT/RIGHT FINGERS NOT TRACKED** — pinch-joint data is unavailable.
- **HANDS TOO FAR - RESET PINCHES** — hand separation exceeds 1 m; release both and reposition.

The desktop mouse mode remains the gameplay baseline. This scene tests **XR gesture input**, not a second keyboard shooting mode. The Meta XR SDK is not required. **Editor simulation is not proof of an Android APK or Quest device tracking.**

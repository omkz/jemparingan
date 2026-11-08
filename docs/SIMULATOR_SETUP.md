# Quest scene in Meta XR Simulator

Use the installed standalone **Meta XR Simulator**, with Unity OpenXR. No legacy Oculus XR loader or passthrough scene is required.

1. Stop Unity Play Mode.
2. Run **Tools → Jemparingan → Quest VR v1 → Configure Editor Simulator OpenXR** once. This enables the Standalone OpenXR loader and hand features that Editor Play Mode uses, including when Android is the selected build target.
3. Activate the installed simulator through **Meta → Meta XR Simulator → Activate**, or its existing runtime toggle.
4. Open `Assets/Jemparingan/Scenes/JemparinganQuest.unity` and press Play. Keep Unity's Game view open and visible.
5. In the simulator's Inputs panel, select **Hand** for both **Left input** and **Right input**, and enable both. Turn **Use same input for both sides** off: the left bow pinch must stay held while the right string pinch releases. The simulator's default Controller inputs do not provide the OVRHand poses used by this game.
6. Begin with both pinch gestures released. Pinch with the left hand to hold the bow, then pinch/draw/release with the right hand. After tracking loss or an interrupted gesture, release both pinches before starting another draw.

The saved Standalone **Initialize XR on Startup** flag remains off for desktop compatibility. `JemparinganQuestEditorXR` creates temporary startup settings only for the Quest scene and restores the original instance on exit. Android continues to use its configured automatic XR startup.

If the simulator reports **No frames received**, check whether Unity is paused, whether the Game view is visible, and whether the Editor is responding. A connected OpenXR runtime alone is not proof that frame submission or gestures work. Do not switch to an obsolete XR backend to address this.

## Verified local blocker (2026-11-09)

On this Windows machine (Intel UHD Graphics 620, driver 31.0.101.2135, simulator 207.1), the original failure had two parts:

- The Quest scene did not start Editor OpenXR because saved Standalone startup was disabled. The scene-specific startup hook and Standalone hand feature configuration address this; actual logs now identify Meta XR Simulator and reach the focused session state.
- **Frame rendering still fails.** Direct3D 11 stalls before the first frame. Starting the Editor with `-force-vulkan` avoids that stall, but the simulator frontend logs `Failed to allocate memory: -2`, `Error creating swapchain image`, then disconnects. A local `disable_interop` configuration and disabling the optional Operator layer did not produce a working frame. These are unsuccessful diagnostic trials, not recommended fixes. Installed simulator files and the user's global Operator setting were preserved.

Do not repeatedly press Play on a frozen Editor. The next diagnostic is a minimal OpenXR/Meta hand sample on this same simulator, followed by comparison on another supported graphics machine. This separates host/runtime graphics allocation from the authored environment. A real Quest device is also needed for archery gesture and seated comfort acceptance. Neither simulator frames nor a hands-only shot have passed on this host yet.

See [Meta's simulator setup](https://developers.meta.com/vr/documentation/unity/xrsim-getting-started/) and the project's [Quest QA report](QA_REPORT_QUEST_VR_V1.md) for actual validation status. Simulator results do not constitute headset testing.

# JEMPARINGAN — RASA | Safe Environment Preview v1

This is a **separate Unity Editor-only 3D blockout**, not a replacement for the v0.9 gameplay scene. It does not require Blender, the Meta XR SDK, or a Quest headset.

## Switch to the isolated branch

From your local repository:

```bash
git status --short
git fetch origin
git switch --track origin/feature/safe-environment-preview-v1
```

If the branch already exists locally, run `git switch feature/safe-environment-preview-v1` instead. Commit/stash any unrelated local work before switching.

## Generate and inspect the preview

1. Open the project in Unity **6000.2.14f1** and wait for compilation.
2. Use **Tools > Jemparingan > Environment > Create Safe Preview v1**.
3. The editor creates and opens `Assets/Jemparingan/Scenes/JemparinganEnvironmentPreview_v01.unity`.
4. Open the **Game** tab (or press Play) to see the viewpoint from a **seated camera**. Press Play again to exit Play Mode.
5. In Edit Mode, use **Tools > Jemparingan > Environment > Set Target 10m / 20m / 30m** to move the single bandulan. Save the scene with Ctrl+S if you want to retain the chosen distance.
6. Check the Console and capture a Game-view screenshot to compare against the environment concept image.

The current preview includes an ~8×8 m open-sided pendopo, wooden floor and joglo roof, a courtyard, 10/20/30 m lane markers, a traditional hanging bandulan, rice paddies, bamboo fence, tropical trees, distant mountains, blue sky, and warm directional lighting.

## Guardrails

- Existing `JemparinganOptimized_v02.unity`, `JemparinganGame.cs`, scoring, and input **are unchanged**.
- The generator uses a new scene path and **requires confirmation before overwriting its own preview scene**.
- It prompts to save any modified scene before switching, preventing silent scene loss.
- Its output is ordinary editable scene objects, mesh assets, and shared materials under `Assets/Jemparingan/EnvironmentPreview/`.
- This is an **art/layout blockout**, not polished final art. The bow/shot mechanics are intentionally absent in this preview.
- Do **not** use the older **Tools > Jemparingan > Create Optimized Scene v0.2** command to preview the environment; that is a different legacy scene generator.
- The 10/20/30 m targets are spatial references. Existing arrow physics has **not** been tuned for those distances.

## If the scene looks wrong

Do not change the original gameplay scene. Capture the Unity Game view and Console errors. The entire experiment is isolated on `feature/safe-environment-preview-v1`; you can return to the original code with `git switch main`.

## Validation status

The editor script was source-reviewed and isolated from the gameplay files in GitHub. **It has not been compiled or visually tested in the Unity Editor or Quest here.** Please use the screenshot and any Console errors to drive the next iteration. After the blockout is accepted, replace its placeholder models with authored Blender assets and integrate the environment into a copy of the gameplay scene.

Unity-generated `.unity`, `.mat`, `.asset` and matching `.meta` files will initially exist locally only. Commit them **only after** the environment layout has been inspected and accepted.

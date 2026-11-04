# Traditional Mode v1 — Mataram-inspired experience

This is **a separate mode in the existing Playable Environment v5 scene**. It is an educational/game adaptation, **not a claim of exact kraton or competition rules**. Classic Mode, 16 arrows, 4 rounds, original red/gold target, RASA combo, and best score remain intact.

## Play

1. Pull `feature/safe-environment-preview-v1`; open `Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity` and press Play.
2. A **Choose Your Mode** overlay appears for Editor and Web builds. Click **Classic** or **Traditional**, or press **1** / **2**.
3. Traditional has **12 arrows** and a single steady session. Scoring uses the visible sections of the bandulan:
   - **Molo** (red head): **3 points**
   - **Jangga** (yellow neck): **2 points**
   - **Awak** (white body): **1 point**
   - Miss: 0 points
4. The bandulan stays still, combo/RASA arcade bonus is not awarded, records use a separate `PlayerPrefs` key (`JemparinganTraditionalBest`), and the intro/results/HUD reflect the selected mode.
5. On desktop/Web, **left mouse** draws/releases, **right mouse + mouse movement** aims, **Shift** makes aiming finer, **R** restarts, **T** optionally enables the strong **practice sight** (off by default in Traditional), and **M** reopens mode selection **before your first arrow or after results**.
6. In Classic, the existing 16-arrow / 4-round experience, score bonuses, and Extreme Swing continue normally. Switching back restores its target visuals and scoring.
7. Quest builds do not use the desktop IMGUI overlay. For an early experimental Quest scene, set `initialOutdoorMode` to **Traditional** in the `JemparinganGame` Inspector; actual Quest gameplay remains unverified.

## Visual approach

- Keeps the seated-height camera and the existing Joglo environment.
- Adds an independent **red/yellow/white bandulan**. The original classic red/gold target is hidden only while Traditional is selected; both use the same shot system.
- Adds a **simple stylized silhouette of folded legs and cloth** below the first-person view. It is not a fully rigged body or verified cross-legged IK.
- Angles the gandewa **about 70 degrees from its Classic vertical visual orientation**. The underlying projectile launch direction and physics are deliberately unchanged.
- Uses the existing 10-meter default lane for accessible desktop play; the environment editor can also select 20 or 30 meters. The target is deliberately visually larger than some historical bandulan examples, and the hit tolerance is forgiving enough for first-person demo play.

## Cultural grounding and limits

The [Museum Sonobudoyo overview](https://sonobudoyo.jogjaprov.go.id/id/tulisan/read/jemparingan-tradisi-panahan-penuh-filosofi-dari-yogyakarta) discusses seated jemparingan, gandewa, jemparing, the bandulan and the focus/feel behind the practice. A [Dinas Kebudayaan DIY article](https://budaya.jogjaprov.go.id/berita/detail/233-disbud-dan-kraton-ngayogyakarta-hadiningrat-gelar-lomba-panahan-gaya-mataram) documents a specific kraton-associated competition using 3/2/1 points for head/neck/body, plus a -1 penalty for a bottom feature and 4 arrows per round. Rules vary between organizations and events.

Traditional v1 implements **only the three visible positive scoring zones** in a shorter 12-arrow game session; the bottom penalty, full 20-round gladhen format, exact bow posture/hand alignment, costume, arrow-rest technique, bell and historically sized 30-meter targets are **not yet implemented**. They should be validated with experienced practitioners before calling this a faithful simulation. The name *Traditional Mode* is not a certification of authenticity.

## Visual Polish v2 — seated pose, drawing hands, and target-zone fidelity

Traditional-only polish builds on v1 without creating new scenes or modifying Classic gameplay:

- **World-anchored seated legs:** the crossed-thigh and crossed-shin shapes, cloth folds, muted sarong trim, and feet stay at the original shooting spot in the Joglo. Unlike v1, they do **not rotate with the aiming camera**. They sit near the lower edge of a typical first-person view and become easier to inspect by looking slightly downward.
- **First-person drawing arms:** reusable sleeves and hands track the existing bow grip and drawn string. The right forearm/hand transitions toward the string during a draw and returns toward the body after release. These are procedural view models, not skeletal IK or anatomically accurate hand tracking. No extra objects are instantiated per shot.
- **Gandewa presentation:** the current Traditional-only ~70° visual tilt stays, referencing one documented horizontal-gandewa style; variation exists between communities. Launch position, player inputs, trajectory, and the Classic bow remain untouched.
- **Bandulan boundaries:** `JemparinganTarget.TraditionalBottom`, `TraditionalNeckBottom`, `TraditionalHeadBottom`, and `TraditionalTop` now define **both the three cylinder meshes and their scoring regions**. The scored point matches the red/yellow/white section in the 3D visual. Hits beyond the visible ends are rejected even if they graze the previous swept-axis tolerance.
- **Hit feedback:** only the colored section struck flashes gold, then returns to its own red, yellow, or white material. Mode switching and restart restore target materials.
- **QA extension:** Edit-mode gameplay QA tests lower neck/head edges, off-target tips, as well as the earlier 3–2–1 scoring and Classic regression checks.

**Cultural reference:** [Keraton Yogyakarta — Jemparingan Gaya Mataram](https://www.kratonjogja.id/kagungan-dalem/14-jemparingan-gaya-mataram/) describes a classical seated cross-legged, horizontal-bow form, and notes contemporary variants with a more vertical bow. This game is **still a stylized first-person adaptation**, not a faithful animation reference or certified traditional competition simulation.

**Visual playtest needed in Unity:** check from a seated camera that (a) the legs do not pop into the central bandulan sight line, (b) the sleeves meet the bow handle and string smoothly as draw power changes, (c) camera yaw rotates the view but does not drag the seated legs around, and (d) molo/jangga/awak hits award 3/2/1 with the correct flash. Verify Classic afterward. Source/static checks cannot prove the appearance.

## Verification

- **Editor static QA**: `Tools → Jemparingan → Run Gameplay QA v0.9`. Extended checks cover traditional scoring values, zone hit detection and a return to Classic hit detection.
- **Play Mode acceptance**: open mode menu, choose Classic, shoot and score; press M before shot or after results, choose Traditional; verify white/yellow/red target, folded-leg silhouette, stationary target, 3/2/1 points, 12 shot pips and result record; switch back to Classic and verify 16 shots and normal bonuses.
- **Browser QA**: build Web using Playable v5, test the clickable overlay, desktop controls and score. **Rebuild and republish on Unity Play**; source commits do not update existing uploads.
- **Not yet verified in this environment**: Unity C# compilation, in-Editor rendering, live projectile/zone alignment, WebGL build, or Meta Quest device gameplay. Avoid claiming these passed solely from static source checks.

**Do not regenerate an existing scene just to add modes**. Both modes are created at runtime, retaining scene and environment customizations.

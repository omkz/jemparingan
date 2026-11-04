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

## Verification

- **Editor static QA**: `Tools → Jemparingan → Run Gameplay QA v0.9`. Extended checks cover traditional scoring values, zone hit detection and a return to Classic hit detection.
- **Play Mode acceptance**: open mode menu, choose Classic, shoot and score; press M before shot or after results, choose Traditional; verify white/yellow/red target, folded-leg silhouette, stationary target, 3/2/1 points, 12 shot pips and result record; switch back to Classic and verify 16 shots and normal bonuses.
- **Browser QA**: build Web using Playable v5, test the clickable overlay, desktop controls and score. **Rebuild and republish on Unity Play**; source commits do not update existing uploads.
- **Not yet verified in this environment**: Unity C# compilation, in-Editor rendering, live projectile/zone alignment, WebGL build, or Meta Quest device gameplay. Avoid claiming these passed solely from static source checks.

**Do not regenerate an existing scene just to add modes**. Both modes are created at runtime, retaining scene and environment customizations.

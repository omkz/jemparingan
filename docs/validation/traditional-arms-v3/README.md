# Traditional hand and arm correction v3

Validated in the existing `Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity`, Unity **6000.2.14f1**, on 2026-11-04. Scene assets were not regenerated or saved. All PNGs here are actual **Unity MCP `capture_game_view`, source=screen** captures, viewed during the iteration. The Editor's frame count and game time advanced during testing.

## Causes and changes

The old left palm was a sphere at an offset in camera coordinates, rather than a shaped hand around the handle. The right palm lerped toward another offset, returned to an unrelated resting position, and was updated before the string. Fixed elbows let the capsules change apparent bone length. Arms inherited camera pitch/yaw while the lower body stayed world-anchored; the old hips were 82 cm ahead of the camera and each thigh spanned about 1.3 m.

- `JemparinganGame.cs`: bind the visual to the existing leather handle and string marker; pose it **after** updating the strings/nocked arrow. Traditional-only 12 cm string brace avoids placing the resting nock inside the handle. The desktop Traditional bow artwork moves 18 cm closer and 10 cm inward; authoritative input, arrow launch origin, aim, assistance, gravity and scoring still use the unchanged input pose. The 70-degree bow cant is retained because both hand contact frames now follow its real orientation. Existing lower-body anchors are brought under the torso; no environment edits.
- `JemparinganTraditionalArcher.cs`: original lightweight hand meshes, fingers around the grip, index/middle fingers on opposite sides of the nock, fixed 36 cm upper arms and 34 cm forearms, analytical two-bone elbows and continuous sleeve meshes. Everything is calculated in one world-anchored seat frame. Upper-torso yaw follows the bow smoothly without inheriting camera pitch. Release opens the fingers, follows through briefly, and returns to the nock over 0.32 s. Arms hide on input loss and round completion; the whole body hides in Classic. Meshes are created once and destroyed with their owner; animation reuses vertex arrays. No colliders, rigging package, external models or textures were added.
- `JemparinganArt.cs`: reduce smoothness for the Traditional cloth/skin tones only.
- `Tools/TraditionalArmsValidation.cs`: in-memory MCP QA helper outside `Assets`; never part of a player build. Logs and validates actual rendered sleeve lengths/contact transforms. Also supports controlled camera rotation and camera aiming for real mouse-release shots.

## Game View evidence

| State | Capture |
|---|---|
| Previous body / floating right hand | [Before](before-rest.png) |
| Rest / both hands on their attachments | [Rest](after-rest.png) |
| Half power | [Half draw](after-half-draw.png) |
| Full power | [Full draw](after-full-draw.png) |
| Immediately after release / fingers open | [Release](after-release.png) |
| Camera yaw -25 degrees | [Left](after-camera-left.png) |
| Camera yaw +25 degrees | [Right](after-camera-right.png) |
| Look down 45 degrees | [Down](after-look-down.png) |
| Traditional results | [12-arrow result](traditional-complete.png) |
| Classic after switching back | [Classic](classic-rest.png) |
| Classic fourth round | [Extreme Swing](classic-extreme-swing.png) |
| Classic results | [16-arrow result](classic-complete.png) |

The before image is from the initial running session with a different camera angle. After images preserve the actual Game View aspect ratio. The HUD/target are clear of the body in the normal shooting view. Existing environment pillars can occlude part of the HUD at large yaw; environment geometry is unchanged.

## Checks and reproducibility

`visual-metrics.json` contains the actual Play Mode measurements at every requested pose. Left grip error and right nock error during rest/drawing are zero; rendered bone lengths remain 0.36 / 0.34 m within float precision. The seat stays at zero yaw/pitch while the camera turns. During release the hand intentionally separates from the springing string, then returns; its release-frame nock distance is not an attachment failure.

Compilation completed with no errors. `JemparinganEditorMenu.RunGameplayQA()` passed **39 deterministic rule/hitbox checks**. Traditional finished with **12 arrows, 9 hits, 18 points** (three hits in each colored zone and three misses). Classic finished with **16 arrows, 11 hits, 105 points**; its sway advanced through four rounds to **0.27 m / 2.35** in Extreme Swing. The Traditional body stayed hidden in Classic and the Classic bow position matched the original input exactly (zero error). The new owned render meshes total **606 vertices / 1,152 triangles across five meshes**, measured in Play Mode; this is geometry size, not a frame-rate benchmark.

Gameplay evidence is in `gameplay-metrics.json`. Traditional used 12 real X11 mouse presses/releases into the focused Game View. Classic had a successful native-input shot, but sustained OS focus proved unreliable; its complete 16-shot regression invokes the existing `Shoot` method through MCP and waits for real projectile/target simulation. This verifies Classic runtime progression/scoring, not 16 end-to-end mouse interactions. The helper adjusts only the existing desktop draw timer for reproducible power; it aims the camera for zone tests without changing projectile/target parameters. The runner restores saved best-score values after completed sequences. An interrupted sequence can still record a local Traditional best of 18 through the normal delayed results flow.

For a manual repeat: enter Play Mode, choose Traditional, focus Game View, hold left mouse and use `script_run` with `file=Tools/TraditionalArmsValidation.cs`, `entry=TraditionalArmsValidation.DrawProgress`, `args=[0.55263158]` for half power or `[1.0]` for full power. The helper freezes `Time.timeScale` for capture. Release the mouse, set `Time.timeScale=1`, and allow 0.4 s for follow-through. `Snapshot` verifies rendered lengths/attachments; `Rotate` takes `[yaw,pitch]`. Restore `Time.timeScale=1` and stop Play Mode when done. The helper requires actual held input and refuses to fake it.

QA runner issues encountered and resolved: the final missed arrow needed its full three-second lifetime before checking results; the operating system needed to keep the Game View focused for legacy input. Neither required gameplay changes. The complete Classic regression uses the explicitly documented method-invocation fallback.

## Limits

Hands and lower-body clothing remain stylized procedural geometry rather than a production rigged avatar. This is calibrated to the existing seated desktop camera; extreme reaches can use shoulder protraction and full tracked-avatar calibration is still future work. Quest hand tracking, device performance, WebGL builds and an APK were **not** tested. No FPS claim is made. Unity Pipeline 0.10.0-exp.1 was installed locally for MCP; the pre-existing package/settings edits are excluded from the gameplay commit.

Final Editor state: Play Mode stopped, Run In Background override restored to off, original auto-tick setting retained. Temporary MCP/native-input controller scripts live under `/tmp`, outside the project. The scene remains clean and unchanged.

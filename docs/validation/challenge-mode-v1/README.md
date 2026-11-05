# Jemparingan — Challenge Mode v1 Validation

Validated in `Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity` with Unity 6000.2.14f1 via Unity Pipeline MCP.

## Overview

Challenge Mode v1 adds structured archery missions, performance medals, and high-replayability gameplay built on top of the polished Traditional first-person bow presentation without duplicating physics or projectile systems.

### 1. Challenge Missions

| Mission | Arrows | Bandulan Movement | Target Zone Objective | Medal Thresholds |
|---|---|---|---|---|
| **Precision** | 8 | Stationary (`sway = 0`, `speed = 0`) | Molo (top red head) | 🥉 1 Molo • 🥈 2 Molo • 🥇 3 Molo |
| **Moving Target** | 10 | Horizontal pendulum swing (`sway = 0.08m`, `speed = 0.9`) | Any valid bandulan hit | 🥉 2 Hits • 🥈 4 Hits • 🥇 6 Hits |

- **Separation of Rules**: Precision only counts hits landing in the Molo zone toward its mission objective. Other zone hits (Jangga/Awak) still register as hits and grant points, but do not count toward the medal. Moving Target counts any valid hit on the moving target.
- **Configurability**: Target swing amplitude and speed are serialized on `JemparinganGame` (`movingChallengeSway`, `movingChallengeSpeed`) and clamped within safe ranges in `JemparinganChallengeRules`.
- **Persistent Medals**: High scores and best medals (`None`, `Bronze`, `Silver`, `Gold`) are persisted per-mission in `PlayerPrefs`.

### 2. UI Architecture

- **Main Mode Selection**: Extended with three clean choices (`CLASSIC`, `TRADITIONAL`, `CHALLENGE`) accessible via mouse click or hotkeys (`1`, `2`, `3`).
- **Mission Selection Screen**: Clean card layout displaying mission descriptions, arrow counts, medal requirements, personal bests, and start buttons (`1: Precision`, `2: Moving Target`, `M/Esc: Back`).
- **Unobtrusive In-Game HUD**: Displays active mission, remaining arrows, objective hits, and current medal progress in the top-left corner without obstructing the bow or target sightline.
- **Post-Mission Results Screen**: Interactive panel summarizing earned medals, total hits, accuracy, and score, with buttons for `RETRY (R)`, `MISSIONS (C)`, and `MODES (M)`.

### 3. Game View Evidence

| State | Screenshot |
|---|---|
| **Mission Selection** | [challenge-mission-select.png](challenge-mission-select.png) |
| **Precision Gameplay** | [precision-gameplay.png](precision-gameplay.png) |
| **Moving Target Gameplay** | [moving-target-gameplay.png](moving-target-gameplay.png) |
| **Results Screen** | [challenge-results.png](challenge-results.png) |

### 4. Verification & QA Checks

- **Deterministic QA Suite**: `Tools/Jemparingan/Run Gameplay QA v0.9` passed all 56 deterministic rule, hitbox, and medal threshold checks.
- **Precision Validation**: Tested 8 consecutive shots. Verified that an Awak hit (shot 4) did not increment `challengeObjectiveHits`, and that completing 8 shots with 7 Molo hits awarded the Gold Medal (`shots=8, finished=true, lastMedal=Gold`).
- **Moving Target Validation**: Tested 10 consecutive shots against the swinging bandulan. Verified smooth pendulum motion (`sway=0.08, speed=0.9`) and full round completion (`shots=10, hits=10, finished=true, lastMedal=Gold`).
- **State Reset & Mode Isolation**:
  - `Retry` cleanly resets arrow counts, hits, scores, and objective progress.
  - Returning to `Traditional` restores 12 arrows, stationary target, and Mataram scoring.
  - Returning to `Classic` restores 16 arrows, 4-stage sway progression, and arcade combo scoring.
- **Console Status**: 0 errors, 0 warnings, clean compilation.

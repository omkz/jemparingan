using System.Collections;
using UnityEngine;

// V0.9: regression hardening, deterministic scoring, and safer shot pool lifecycle.
// Quest integration is deliberately left for the later phase.
public class JemparinganGame : MonoBehaviour
{
    [Header("Input — keep from V0.1")]
    [SerializeField] private DesktopBowInput desktopInput;
    [SerializeField] private MetaQuestBowInput questInput;
    [SerializeField] private bool preferQuestHandsInEditor;

    public enum GameMode { Traditional = 1, Modern = 3 }
    [Header("Outdoor game mode (Traditional / Modern)")]
    [SerializeField] private GameMode initialOutdoorMode = GameMode.Traditional;
    private GameMode _mode = GameMode.Traditional;
    private bool _modeChosen;
    private GameObject _traditionalBandulanVisual;
    private Renderer _traditionalBandulanBody;
    private Transform _traditionalPosture;
    private JemparinganTraditionalArcher _traditionalArcher;
    private Renderer[] _seatedBodyRenderers;
    private Transform _modernTargetsRoot, _bandulanSuspension;
    // The scene has an authored preview frame (two uprights and a crossbar).
    // Hiding only the runtime bandulan leaves this gawang in Modern Mode.
    private GameObject _sceneBandulanFrame;
    private bool _sceneBandulanFrameInitiallyActive;
    private JemparinganModernTarget[] _modernTargets;
    // Reuse the outdoor scene's 16-shot arrow and HUD pool.
    [SerializeField, Range(6, 16)] private int modernArrowCount = 12;
    [Header("Shared three-level progression")]
    // Traditional keeps one bandulan. Level 3 adds a slight deterministic
    // variation to the pendulum rhythm, not random motion.
    [SerializeField, Range(.02f, .20f)] private float traditionalLevel2Sway = .08f;
    [SerializeField, Range(.3f, 1.5f)] private float traditionalLevel2Speed = .75f;
    [SerializeField, Range(.08f, .30f)] private float traditionalLevel3Sway = .16f;
    [SerializeField, Range(.8f, 2.5f)] private float traditionalLevel3Speed = 1.12f;
    [Header("Modern progressive targets")]
    [SerializeField] private bool modernProgressiveStages = true;
    [SerializeField, Range(0f, .45f)] private float modernMovingTargetTravel = .26f;
    [SerializeField, Range(.3f, 1.8f)] private float modernMovingTargetSpeed = .85f;
    private int _stage;
    // Desktop-only presentation timing. These do not gate aiming or shots.
    private float _stageIntroStartedAt = -1f;
    private float _stageIntroUntil = -1f;

    [Header("Modern first-person rig v2 (desktop visual only)")]
    [SerializeField] private bool modernViewmodelRigV2 = true;
    [SerializeField, Range(.60f, 1.00f)] private float modernBowForwardDistance = .78f;
    [SerializeField, Range(-.10f, .40f)] private float modernBowRightOffset = .20f;
    [SerializeField, Range(-.05f, .40f)] private float modernBowDownOffset = .18f;
    [SerializeField, Range(.70f, 1.10f)] private float modernBowVisualScale = .94f;
    // Used only by the older non-screenshot v2 preset. The screenshot
    // calibration instead solves the actual bow projection and smoothly
    // changes the real camera FOV for modest Modern aim zoom.
    [SerializeField, Range(45f, 85f)] private float modernViewmodelFov = 65f;

    [Header("Modern screenshot calibration (desktop only)")]
    // Preserve the screenshot's grip placement and projected bow height,
    // but use a modest, fixed roll for modern target archery. The old
    // landmark-derived ~17-degree lean looked excessive with a real sight.
    [SerializeField] private bool modernUseReferenceFraming = true;
    [Header("Shared desktop first-person grip and draw hands")]
    [SerializeField] private bool modernUseNewHands = true;
    // Cosmetic only. Disable if the right pinch obstructs the view.
    [SerializeField] private bool modernShowRightDrawHand = true;
    private JemparinganModernHands _modernHands;
    [Header("Modern lightweight release and impact feel (cosmetic only)")]
    [SerializeField] private bool modernLightweightGameFeel = true;
    [SerializeField, Range(.2f, 2f)] private float modernReleaseRecoilDegrees = 1.1f;
    [Header("Modern idle breathing sway (cosmetic viewmodel only, no wind)")]
    [SerializeField] private bool modernSubtleBreathingSway = true;
    [SerializeField, Range(0f, .50f)] private float modernIdleSwayDegrees = .28f;
    [SerializeField, Range(3f, 7f)] private float modernBreathingCycleSeconds = 4.5f;
    [Header("Modern full-draw aim wobble (optical sight and projectile linked)")]
    [SerializeField] private bool modernAimWobble = true;
    [SerializeField, Range(0f, .35f)] private float modernAimWobbleDegrees = .10f;
    private float _modernWobbleBlend;
    private Vector3 _modernLastCalibratedAimRay;
    private bool _modernLastCalibratedAimRayValid;
    private float _modernBreathingWeight = 1f;
    private float _modernReleaseFeedbackAt = -1f;
    private bool _modernDrawCuePlayed;
    private AudioClip _modernDrawTensionSound, _modernReleaseSound;
    [SerializeField, Range(.45f, .75f)] private float modernReferenceGripViewportX = .554f;
    [SerializeField, Range(.15f, .45f)] private float modernReferenceGripViewportY = .270f;
    [SerializeField, Range(.50f, .95f)] private float modernReferenceGripDepth = .70f;
    [SerializeField, Range(.90f, 1.30f)] private float modernReferenceTipViewportY = 1.075f;
    [SerializeField, Range(-12f, 12f)] private float modernBowCantDegrees = -2f;
    // A previously saved scene may still serialize the old -6° default.
    // Migrate only that historical default; preserve other user tuning.
    [SerializeField, HideInInspector] private bool modernUprightV3Migrated;
    [SerializeField, Range(48f, 75f)] private float modernReferenceAimFov = 60f;
    [Header("Modern full-draw then precision aim (desktop only)")]
    [SerializeField, Range(.80f, 1f)] private float modernPrecisionDrawThreshold = .96f;
    [SerializeField, Range(0f, .20f)] private float modernPreAimRightShift = .09f;
    [SerializeField, Range(0f, .15f)] private float modernPreAimLowering = .055f;
    [SerializeField, Range(2f, 14f)] private float modernPrecisionPoseSpeed = 7f;
    [SerializeField] private bool modernDebugCameraCrosshair = false;
    // 3D sight is attached to the cosmetic bow and follows its framing.
    // Keep HUD G-marker optional for alignment comparisons only.
    [SerializeField] private bool modernUsePhysicalSight = true;
    private JemparinganModernBowSight _modernPhysicalSight;
    private float _modernDrawPower;
    private float _modernPrecisionPose;
    private bool _modernSightZeroed;
    private Camera _playerCamera;
    private float _playerBaseFov = 65f;
    private float _modernReferenceCalculatedScale = 1f;

    [Header("Modern FPS bow poses (idle / drawing / full draw)")]
    [SerializeField, Range(.08f, .35f)] private float modernIdleRightShift = .22f;
    [SerializeField, Range(.08f, .35f)] private float modernIdleLowering = .22f;
    [SerializeField, Range(.0f, .20f)] private float modernIdleForwardShift = .08f;
    [SerializeField, Range(.70f, 1f)] private float modernIdleBowScale = .90f;
    [SerializeField, Range(2f, 10f)] private float modernPoseTransitionSpeed = 4.8f;
    private float _modernRaisedPose;
    private TextMesh _introGoalLabel, _introTitleLabel;

    [Header("Experimental Modern Arrow Cam (desktop only)")]
    [SerializeField] private bool modernEveryShotArrowCam = true;
    private JemparinganArrowFollowCamera _arrowCam;
    private bool _restoreAfterArrowCam;

    private bool IsTraditionalMode() { return _mode == GameMode.Traditional; }
    private bool IsModernMode() { return useOutdoorEnvironment && _mode == GameMode.Modern; }
    // Both desktop modes use the same first-person bow, hand rig, sight,
    // input, audio and feedback. Their targets and score callbacks differ.
    private bool UsesSharedDesktopBow()
    {
        return useOutdoorEnvironment && _modeChosen && UsesDesktopInput() &&
            (IsTraditionalMode() || IsModernMode());
    }
    private bool UsesTraditionalPresentation()
    {
        return IsTraditionalMode() || IsModernMode();
    }

    private string BestKey()
    {
        if (IsModernMode()) return "JemparinganModernBestScore";
        return IsTraditionalMode() ? "JemparinganTraditionalBest" : "JemparinganRasaBest";
    }

    [Header("Gameplay tuning")]
    [SerializeField, Range(6, 30)] private int arrowsPerRound = 12;
    [SerializeField, Range(0.06f, 0.3f)] private float minimumDraw = 0.10f;
    [SerializeField, Range(0.25f, 0.6f)] private float maximumDraw = 0.40f;
    [SerializeField, Range(4f, 30f)] private float minimumArrowSpeed = 7.5f;
    [SerializeField, Range(6f, 35f)] private float maximumArrowSpeed = 11f;
    [SerializeField, Range(0f, 10f)] private float simulatedGravity = 2.25f;
    [SerializeField, Range(0f, 1f)] private float aimAssist = 0.40f;
    [SerializeField] private bool trajectoryPreview = true;
    [SerializeField, Range(0.05f, 0.4f)] private float minimumHoldSeconds = 0.14f;

    [Header("RASA focus (tune from player tests)")]
    [SerializeField, Range(0.3f, 1.0f)] private float focusTimeSeconds = 0.58f;
    [SerializeField, Range(1.5f, 8f)] private float focusAngularTolerance = 3.5f;
    [SerializeField, Range(0.03f, 0.18f)] private float focusPositionTolerance = 0.08f;

    [Header("Atmosphere (quiet, original procedural audio)")]
    [SerializeField, Range(0f, 0.15f)] private float ambientWindVolume = 0.045f;

    // Optional bridge for the separate outdoor VR preview. Defaults keep the
    // original v0.9 scene fully self-contained and at its original 2.65m distance.
    [Header("Outdoor environment v5 (optional; disabled in legacy scenes)")]
    [SerializeField] private bool useOutdoorEnvironment = false;
    [SerializeField] private Transform outdoorTargetAnchor;
    [SerializeField, Range(1.6f, 4.0f)] private float outdoorArrowLifetime = 3.0f;

    private const int ArrowPoolSize = 14;
    private const int PreviewDotCount = 8;
    private Transform _bow, _stringMarker, _focusRoot;
    private Transform _desktopPracticeSight;
    private Renderer[] _practiceSightRenderers;
    private TextMesh _practiceSightLabel;
    private bool _desktopTrainingEnabled = true;
    // Cached sight state keeps TextMesh updates event-driven, not every frame.
    private bool _practiceSightStateKnown, _practiceSightWasAligned, _practiceSightWasDrawing;
    // Modern desktop uses a separate minimal IMGUI. Keep the old
    // world-space text HUD hidden there, but retain it for other modes.
    private GameObject _worldSpaceHudRoot, _worldSpaceHelpRoot;
    private Renderer[] _focusRenderers;
    private Transform _upperString, _lowerString;
    // Cosmetic arrow follows the bowstring in outdoor mode.
    // It never replaces the real pooled projectile launched by Shoot().
    private Transform _nockedArrow;
    private JemparinganTarget _target;
    private JemparinganArrow[] _arrows;
    private Transform[] _previewDots;
    private TextMesh _scoreLabel, _infoLabel, _feedbackLabel, _titleLabel, _phaseLabel;
    private TextMesh _resultsTitle, _resultsDetails, _resultsHint, _stageBanner;
    private GameObject _introPanel, _resultsPanel;
    private Renderer[] _shotPipRenderers;
    private AudioSource _audio, _ambientAudio;
    private AudioClip _shotSound, _hitSound, _bullseyeSound, _missSound, _focusSound, _stageSound;
    private TextMesh _impactLabel;
    private Vector3 _impactLabelBasePosition = new Vector3(.38f, -.02f, -.18f);
    private float _impactLabelUntil, _impactLabelStart;
    private Vector3 _lastAim = Vector3.forward;
    private bool _ready, _wasDrawing, _wasBowHeld, _roundOver, _finished;
    private bool _replayArmed, _awaitStringRelease;
    private float _lastDraw, _feedbackUntil, _drawStartedAt;
    private float _focusProgress, _focusReadyUntil = -1f;
    private Vector3 _focusAimAnchor, _focusBowAnchor;
    private int _lastFocusStep = -1;
    private bool _focusCuePlayed;
    private float _replayHoldStartedAt = -1f, _stageBannerUntil;
    private int _shots, _hits, _score, _best, _poolCursor;
    private int _previousBest, _bullseyes;
    private int _moloHits, _janggaHits, _awakHits;
    // Reuse the shared score HUD for a brief Traditional-only score pulse.
    // No new text, UI object, or per-shot allocation is required.
    private float _traditionalImpactAt = -1f;
    private JemparinganTarget.TraditionalZone _traditionalImpactZone;
    private string _lastInfo = "";
    private int _lastPowerStep = -1;
    private bool _lastStableSignal;

    // Web browser builds are mouse/keyboard demos. Keep Quest hand tracking
    // reserved for native Quest builds and opt-in Editor tests.
    private bool UsesDesktopInput()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return true;
#else
        return Application.isEditor && !preferQuestHandsInEditor;
#endif
    }

    // LMB alone holds the string and aims with the mouse. Once the fast
    // draw animation reaches full power, settle into the calibrated sight.
    // The axis marker is a temporary debug reference, not a ballistic sight.
    private void SetModernDesktopMouseControl(bool enabled)
    {
        if (desktopInput != null)
            desktopInput.ModernSingleButtonAim = enabled && UsesDesktopInput();
    }

    private bool ModernPrecisionAimRequested()
    {
        return UsesSharedDesktopBow() && modernViewmodelRigV2 && modernUseReferenceFraming &&
            !_finished && !_roundOver && !_awaitStringRelease &&
            (_arrowCam == null || !_arrowCam.IsActive) &&
            Input.GetMouseButton(0) &&
            _modernDrawPower >= modernPrecisionDrawThreshold;
    }

    // Zoom after held LMB reaches full draw. No RMB modifier is required.
    // Changes projection, not the real projectile path or camera aim direction.
    private void UpdateModernCameraFov()
    {
        if (!UsesDesktopInput() || _playerCamera == null) return;
        bool zoom = ModernPrecisionAimRequested();
        // A customized scene with a narrower base FOV must never zoom out.
        float targetFov = zoom
            ? Mathf.Min(modernReferenceAimFov, _playerBaseFov)
            : _playerBaseFov;
        _playerCamera.fieldOfView = Mathf.MoveTowards(
            _playerCamera.fieldOfView, targetFov, 48f * Time.deltaTime);
    }

    private void RestoreModernCameraFov()
    {
        if (_playerCamera != null) _playerCamera.fieldOfView = _playerBaseFov;
    }

    private void OnDisable()
    {
        RestoreModernCameraFov();
        HideModernPhysicalSight();
        HideModernHands();
        SetModernDesktopMouseControl(false);
        _modernReleaseFeedbackAt = -1f;
        _modernBreathingWeight = 1f;
        _modernWobbleBlend = 0f;
        _modernLastCalibratedAimRayValid = false;
    }

    private void OnDestroy()
    {
        // Only the new Modern one-shot clips are owned here. Existing audio
        // clip lifetimes remain unchanged for the legacy game modes.
        if (_modernDrawTensionSound != null) Destroy(_modernDrawTensionSound);
        if (_modernReleaseSound != null) Destroy(_modernReleaseSound);
    }

    private IEnumerator Start()
    {
        if (desktopInput == null) desktopInput = GetComponent<DesktopBowInput>();
        if (questInput == null) questInput = GetComponent<MetaQuestBowInput>();
        yield return null;
        _ready = BuildScene();
        if (_ready && useOutdoorEnvironment)
        {
            // Desktop/Web use an explicit menu. For a future native Quest
            // scene, the Inspector selects the initial mode without IMGUI.
            _modeChosen = !UsesDesktopInput();
            ChooseMode(initialOutdoorMode == GameMode.Modern
                ? GameMode.Modern : GameMode.Traditional);
            if (UsesDesktopInput())
            {
                _modeChosen = false;
                SetModernDesktopMouseControl(false);
            }
        }
    }

    private void OnGUI()
    {
        if (!_ready || !useOutdoorEnvironment || !UsesDesktopInput())
            return;

        if (!_modeChosen)
        {
            DrawMainModeSelectGUI();
            return;
        }

        if (_finished) DrawModernResultsGUI();
        else DrawModernInGameHUD();
    }

    private void DrawMainModeSelectGUI()
    {
        float width = Mathf.Min(560f, Screen.width - 24f);
        float height = 245f;
        Rect panel = new Rect((Screen.width - width) * .5f,
            (Screen.height - height) * .5f, width, height);
        GUI.Box(panel, "");
        GUIStyle header = new GUIStyle(GUI.skin.label);
        header.fontSize = 21;
        header.fontStyle = FontStyle.Bold;
        header.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(panel.x + 15f, panel.y + 14f, width - 30f, 36f),
            "JEMPARINGAN", header);
        GUI.Label(new Rect(panel.x + 15f, panel.y + 53f, width - 30f, 24f),
            "CHOOSE YOUR MODE", GUI.skin.box);
        float buttonWidth = (width - 45f) / 2f;
        if (GUI.Button(new Rect(panel.x + 15f, panel.y + 91f, buttonWidth, 103f),
            "TRADITIONAL\n3 LEVELS  |  12 ARROWS\n3-2-1 SCORING"))
            ChooseMode(GameMode.Traditional);
        if (GUI.Button(new Rect(panel.x + 30f + buttonWidth, panel.y + 91f,
            buttonWidth, 103f),
            modernProgressiveStages
                ? "MODERN ARCHERY\n3 LEVELS  |  " + modernArrowCount +
                    " ARROWS\nRING SCORING"
                : "MODERN ARCHERY\n3 ROUND TARGETS\nRING SCORING"))
            ChooseMode(GameMode.Modern);
        GUI.Label(new Rect(panel.x + 15f, panel.y + 205f, width - 30f, 24f),
            "Keys: 1 Traditional  |  2 Modern", GUI.skin.box);
    }



    // Shared three-level arrow budget: Traditional uses 12 (four per level),
    // while Modern may use an Inspector-selected count.
    private int StageStart(int stage)
    {
        return Mathf.CeilToInt(Mathf.Clamp(stage, 0, 3) *
            arrowsPerRound / 3f);
    }

    private int StageEnd(int stage)
    {
        return StageStart(stage + 1);
    }

    private bool AnyArrowFlying()
    {
        if (_arrows == null) return false;
        for (int i = 0; i < _arrows.Length; i++)
            if (_arrows[i] != null && _arrows[i].IsFlying)
                return true;
        return false;
    }

    private bool UsesProgressiveStages()
    {
        return useOutdoorEnvironment && _modeChosen &&
            (IsTraditionalMode() || (IsModernMode() && modernProgressiveStages));
    }

    private void AdvanceStage()
    {
        if (_stage >= 2) return;
        _stage++;
        // If a player starts a new draw while an old projectile is
        // finishing, release must not accidentally create a new shot.
        ClearDrawState();
        _awaitStringRelease = UsesDesktopInput() && Input.GetMouseButton(0);
        if (IsModernMode())
        {
            // Preserve Modern's existing target behavior.
            if (_modernTargets != null && _modernTargets.Length > 1 &&
                _modernTargets[1] != null)
                _modernTargets[1].SetLateralMotion(_stage == 2,
                    modernMovingTargetTravel, modernMovingTargetSpeed);
        }
        else if (IsTraditionalMode() && _target != null)
        {
            // Phase and rope remain continuous; level 3 has a subtle
            // deterministic rhythm shift in addition to the larger arc.
            _target.SetSway(
                _stage == 1 ? traditionalLevel2Sway : traditionalLevel3Sway,
                _stage == 1 ? traditionalLevel2Speed : traditionalLevel3Speed,
                false, _stage == 2 ? .08f : 0f);
        }
        _stageIntroStartedAt = Time.time;
        _stageIntroUntil = Time.time + 1.25f;
        // Both desktop modes use the same concise IMGUI stage transition.
        // World-space banners remain only for non-desktop/Quest input.
        if (_stageBanner != null && !UsesDesktopInput())
        {
            _stageBanner.text = "LEVEL " + (_stage + 1) + "/3";
            _stageBannerUntil = Time.time + 1.5f;
        }
        if (_audio != null && _stageSound != null)
            _audio.PlayOneShot(_stageSound, .23f);
        UpdateHUD();
    }

    // DrawTexture avoids adding textures, materials, cameras or UI canvases.
    // Every call restores GUI.color, keeping other IMGUI modes unaffected.
    private static void DrawModernPanelRect(Rect rect, Color tint)
    {
        Color previous = GUI.color;
        GUI.color = tint;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static GUIStyle ModernLabelStyle(int fontSize,
        FontStyle weight, TextAnchor alignment, Color tint)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.fontStyle = weight;
        style.alignment = alignment;
        style.normal.textColor = tint;
        return style;
    }

    private static Color ModernStageAccent(int stage)
    {
        return stage == 0 ? new Color(.37f, .86f, .74f) :
            stage == 1 ? new Color(.96f, .73f, .38f) :
            new Color(.54f, .76f, 1f);
    }

    // Minimal Modern heads-up display: only the level, score and arrows.
    // No persistent instructions, ring guide or objective paragraphs
    // competing with the target and the bow-mounted optical sight.
    private void DrawModernInGameHUD()
    {
        float panelWidth = Mathf.Min(312f, Screen.width - 24f);
        if (panelWidth < 210f) return;
        Rect panel = new Rect(12f, 12f, panelWidth, 96f);
        float innerWidth = panelWidth - 24f;
        float colWidth = innerWidth / 3f;
        Color white = new Color(.94f, .97f, .99f);
        Color muted = new Color(.66f, .78f, .82f);
        Color accent = UsesProgressiveStages() ?
            ModernStageAccent(_stage) : ModernStageAccent(0);
        GUIStyle caption = ModernLabelStyle(11, FontStyle.Normal,
            TextAnchor.MiddleCenter, muted);
        GUIStyle number = ModernLabelStyle(25, FontStyle.Bold,
            TextAnchor.MiddleCenter, white);

        DrawModernPanelRect(panel, new Color(.035f, .065f, .10f, .84f));
        DrawModernPanelRect(new Rect(panel.x, panel.y,
            panelWidth, 3f), accent);
        for (int column = 0; column < 3; column++)
        {
            float columnX = panel.x + 12f + column * colWidth;
            string heading = column == 0 ? "LEVEL" :
                column == 1 ? "SCORE" : "ARROWS";
            string value = column == 0 ?
                (UsesProgressiveStages()
                    ? (_stage + 1) + "/3" : "RANGE") :
                column == 1 ? _score.ToString() :
                Mathf.Max(0, arrowsPerRound - _shots).ToString();
            GUI.Label(new Rect(columnX, panel.y + 9f,
                colWidth, 20f), heading, caption);
            GUI.Label(new Rect(columnX, panel.y + 29f,
                colWidth, 35f), value, number);
        }

        // A short underline gives hit confirmation without text or an
        // extra HUD widget. Zone color matches the existing bandulan FX.
        if (IsTraditionalMode() && _traditionalImpactAt >= 0f)
        {
            float age = Time.time - _traditionalImpactAt;
            if (age >= 0f && age < .46f)
            {
                Color impactTint = _traditionalImpactZone ==
                    JemparinganTarget.TraditionalZone.Molo
                    ? new Color(.99f, .80f, .34f) :
                    _traditionalImpactZone ==
                    JemparinganTarget.TraditionalZone.Jangga
                        ? new Color(.97f, .78f, .45f)
                        : new Color(.49f, .88f, .75f);
                float fade = Mathf.Pow(1f - age / .46f, 2f);
                DrawModernPanelRect(new Rect(
                    panel.x + 12f + colWidth * 1.20f,
                    panel.y + 67f, colWidth * .60f, 3f),
                    new Color(impactTint.r, impactTint.g,
                        impactTint.b, fade * .95f));
            }
        }

        // Three silent indicators communicate level progression with color.
        if (UsesProgressiveStages())
        {
            float gap = 5f, width = (innerWidth - 2f * gap) / 3f;
            for (int stage = 0; stage < 3; stage++)
            {
                Color tint = stage == _stage ?
                    ModernStageAccent(stage) :
                    stage < _stage
                        ? new Color(.31f, .57f, .50f, .75f)
                        : new Color(.31f, .40f, .47f, .60f);
                DrawModernPanelRect(new Rect(panel.x + 12f +
                    stage * (width + gap), panel.y + 76f, width, 5f), tint);
            }
        }

        if (ModernPrecisionAimRequested() && modernDebugCameraCrosshair)
            DrawModernDebugCrosshair();
        DrawModernStageTransition();
    }

    private void DrawModernStageTransition()
    {
        if (!UsesProgressiveStages() || _stageIntroStartedAt < 0f ||
            Time.time >= _stageIntroUntil) return;

        float fadeElapsed = Time.time - _stageIntroStartedAt;
        float opacity = Mathf.Clamp01(fadeElapsed / .15f) *
            Mathf.Clamp01((_stageIntroUntil - Time.time) / .28f);
        float bannerWidth = Mathf.Min(220f, Screen.width - 24f);
        if (bannerWidth < 160f) return;
        Rect banner = new Rect((Screen.width - bannerWidth) * .5f,
            Mathf.Max(24f, Screen.height * .12f), bannerWidth, 48f);
        Color accent = ModernStageAccent(_stage);
        DrawModernPanelRect(banner, new Color(.035f, .065f, .10f,
            .88f * opacity));
        DrawModernPanelRect(new Rect(banner.x, banner.y, 3f,
            banner.height), new Color(accent.r, accent.g,
            accent.b, opacity));
        GUIStyle heading = ModernLabelStyle(20, FontStyle.Bold,
            TextAnchor.MiddleCenter, new Color(.96f, .98f, 1f, opacity));
        GUI.Label(new Rect(banner.x + 8f, banner.y + 4f,
            bannerWidth - 16f, 40f),
            "LEVEL " + (_stage + 1), heading);
    }

    private void DrawModernDebugCrosshair()
    {
        // Camera forward is NOT the same as the gravity-affected impact.
        // Used to calibrate physical sight placement later.
        float x = Screen.width * .5f, y = Screen.height * .5f;
        Color saved = GUI.color;
        GUI.color = new Color(0f, .10f, .14f, .84f);
        GUI.DrawTexture(new Rect(x - 10f, y - 1.5f, 20f, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 1.5f, y - 10f, 3f, 20f), Texture2D.whiteTexture);
        GUI.color = new Color(.32f, 1f, .87f, .96f);
        GUI.DrawTexture(new Rect(x - 8f, y - 1f, 16f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 1f, y - 8f, 2f, 16f), Texture2D.whiteTexture);
        GUIStyle hint = new GUIStyle(GUI.skin.label);
        hint.fontSize = 10;
        hint.alignment = TextAnchor.MiddleCenter;
        hint.normal.textColor = new Color(.70f, 1f, .91f);
        GUI.Label(new Rect(x - 118f, y + 12f, 236f, 20f),
            _modernSightZeroed ? "ZEROED TO TARGET FACE" :
                "AXIS ONLY - NO TARGET LOCK", hint);
        GUI.color = saved;
    }

    // Shared result layout; Traditional gets one compact scoring-zone row.
    // Modern retains its original 250 px result card unchanged.
    private void DrawModernResultsGUI()
    {
        float panelWidth = Mathf.Min(390f, Screen.width - 24f);
        if (panelWidth < 240f) return;
        bool traditional = IsTraditionalMode();
        float panelHeight = traditional ? 290f : 250f;
        Rect panel = new Rect((Screen.width - panelWidth) * .5f,
            (Screen.height - panelHeight) * .5f,
            panelWidth, panelHeight);
        float x = panel.x + 18f, contentWidth = panelWidth - 36f;
        Color white = new Color(.95f, .98f, 1f);
        Color muted = new Color(.70f, .80f, .84f);
        GUIStyle heading = ModernLabelStyle(19, FontStyle.Bold,
            TextAnchor.MiddleCenter, white);
        GUIStyle scoreStyle = ModernLabelStyle(39, FontStyle.Bold,
            TextAnchor.MiddleCenter, white);
        GUIStyle statStyle = ModernLabelStyle(13, FontStyle.Normal,
            TextAnchor.MiddleCenter, muted);
        DrawModernPanelRect(panel, new Color(.035f, .065f, .10f, .96f));
        DrawModernPanelRect(new Rect(panel.x, panel.y, panelWidth,
            4f), ModernStageAccent(_stage));
        GUI.Label(new Rect(x, panel.y + 13f, contentWidth, 28f),
            traditional ? "TRADITIONAL" : "COMPLETE", heading);
        GUI.Label(new Rect(x, panel.y + 48f, contentWidth, 53f),
            _score.ToString(), scoreStyle);
        GUI.Label(new Rect(x, panel.y + 107f, contentWidth, 21f),
            (_score > _previousBest ? "NEW BEST" : "BEST") +
            "  " + Mathf.Max(_score, _previousBest), statStyle);
        int accuracy = JemparinganScoreRules.AccuracyPercent(_hits, _shots);
        GUI.Label(new Rect(x, panel.y + 135f, contentWidth, 22f),
            "ACCURACY  " + accuracy + "%   •   HITS  " +
            _hits + "/" + _shots, statStyle);
        if (traditional)
        {
            // A single row summarizes the three bandulan scoring zones;
            // nothing extra appears while the player is aiming.
            GUI.Label(new Rect(x, panel.y + 165f, contentWidth, 23f),
                "MOLO " + _moloHits + "   •   JANGGA " + _janggaHits +
                "   •   AWAK " + _awakHits, statStyle);
        }
        float buttonY = traditional ? 224f : 184f;
        float gap = 12f;
        float buttonWidth = (contentWidth - gap) * .5f;
        Color savedBackground = GUI.backgroundColor;
        GUI.backgroundColor = new Color(.46f, .83f, .71f);
        if (GUI.Button(new Rect(x, panel.y + buttonY,
            buttonWidth, 44f), "RETRY"))
        {
            ResetRound();
            _awaitStringRelease = UsesDesktopInput() && Input.GetMouseButton(0);
        }
        GUI.backgroundColor = new Color(.75f, .82f, .88f);
        if (GUI.Button(new Rect(x + buttonWidth + gap,
            panel.y + buttonY, buttonWidth, 44f), "MODES"))
            ReturnToModeSelect();
        GUI.backgroundColor = savedBackground;
    }

    private void SetSceneBandulanFrameVisible(bool visible)
    {
        if (_sceneBandulanFrame == null) return;
        _sceneBandulanFrame.SetActive(visible && _sceneBandulanFrameInitiallyActive);
    }

    private void SetSeatedBodyVisible(bool visible)
    {
        if (_seatedBodyRenderers == null) return;
        foreach (Renderer renderer in _seatedBodyRenderers)
            if (renderer != null) renderer.enabled = visible;
    }

    // Both outdoor desktop modes use the same minimal IMGUI.
    // Non-desktop Quest paths keep their world-space labels.
    // Decorative background boards are no longer constructed anywhere.
    private void SyncWorldSpaceHudVisibility()
    {
        // The compact IMGUI HUD replaces all the first-person TextMesh panels
        // on desktop in both modes; retain world-space UI on Quest only.
        bool visible = !(useOutdoorEnvironment && UsesDesktopInput());
        if (_worldSpaceHudRoot != null &&
            _worldSpaceHudRoot.activeSelf != visible)
            _worldSpaceHudRoot.SetActive(visible);
        if (_worldSpaceHelpRoot != null &&
            _worldSpaceHelpRoot.activeSelf != visible)
            _worldSpaceHelpRoot.SetActive(visible);
    }

    private void StartModernMode()
    {
        HideModernHands();
        if (!modernUprightV3Migrated)
        {
            if (Mathf.Approximately(modernBowCantDegrees, -6f))
                modernBowCantDegrees = -2f;
            modernUprightV3Migrated = true;
        }
        _mode = GameMode.Modern;
        _modeChosen = true;
        // The 3D sight is now the default; HUD crosshair is opt-in
        // per Modern entry for test/compare via G.
        modernDebugCameraCrosshair = false;
        SetModernDesktopMouseControl(true);
        arrowsPerRound = modernArrowCount;
        _desktopTrainingEnabled = false;
        if (_target != null) _target.gameObject.SetActive(false);
        if (_bandulanSuspension != null)
            _bandulanSuspension.gameObject.SetActive(false);
        SetSceneBandulanFrameVisible(false);
        if (_modernTargetsRoot != null)
            _modernTargetsRoot.gameObject.SetActive(true);
        if (_traditionalPosture != null)
            _traditionalPosture.gameObject.SetActive(true);
        SetSeatedBodyVisible(false); // Retain only the lightweight grip/forearm.
        HideTraditionalArms();
        HideDesktopPracticeSight();
        _lastInfo = "";
        ResetRound();
        _awaitStringRelease = UsesDesktopInput() && Input.GetMouseButton(0);
    }

    public void ChooseMode(GameMode next)
    {
        if (!useOutdoorEnvironment || !_ready) return;
        if (next != GameMode.Modern) next = GameMode.Traditional;
        RestoreModernCameraFov();
        HideModernPhysicalSight();
        HideModernHands();
        SetModernDesktopMouseControl(false);
        _mode = next;
        _modeChosen = true;
        // Return every shared scene element to its original state before
        // entering any mode; modern targets are isolated from bandulan modes.
        if (_modernTargetsRoot != null)
            _modernTargetsRoot.gameObject.SetActive(false);
        if (_target != null) _target.gameObject.SetActive(true);
        if (_bandulanSuspension != null)
            _bandulanSuspension.gameObject.SetActive(true);
        SetSceneBandulanFrameVisible(true);
        SetSeatedBodyVisible(true);
        if (next == GameMode.Modern)
        {
            StartModernMode();
            return;
        }

        arrowsPerRound = 12;
        _desktopTrainingEnabled = false;
        SetModernDesktopMouseControl(UsesSharedDesktopBow());
        SetSeatedBodyVisible(!UsesSharedDesktopBow());
        if (_traditionalBandulanVisual != null)
            _traditionalBandulanVisual.SetActive(true);
        _target.SetBody(_traditionalBandulanBody, JemparinganArt.Tone.BandulanWhite);
        if (_traditionalPosture != null)
            _traditionalPosture.gameObject.SetActive(true);
        HideTraditionalArms();
        _lastInfo = "";
        HideDesktopPracticeSight();
        ResetRound();
        // Mouse selection is a UI click, never a shot.
        _awaitStringRelease = UsesDesktopInput() && Input.GetMouseButton(0);
    }



    public void ReturnToModeSelect()
    {
        RestoreModernCameraFov();
        HideModernPhysicalSight();
        HideModernHands();
        SetModernDesktopMouseControl(false);
        if (_modernTargetsRoot != null)
            _modernTargetsRoot.gameObject.SetActive(false);
        if (_target != null) _target.gameObject.SetActive(true);
        if (_bandulanSuspension != null)
            _bandulanSuspension.gameObject.SetActive(true);
        SetSceneBandulanFrameVisible(true);
        SetSeatedBodyVisible(true);
        if (_traditionalPosture != null)
            _traditionalPosture.gameObject.SetActive(false);
        _modeChosen = false;
        HideTraditionalArms();
        HideDesktopPracticeSight();
        ResetRound();
        _awaitStringRelease = UsesDesktopInput() && Input.GetMouseButton(0);
    }



    private void Update()
    {
        if (!_ready) return;
        UpdateModernCameraFov();
        if (useOutdoorEnvironment && UsesDesktopInput())
        {
            if (!_modeChosen)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) ChooseMode(GameMode.Traditional);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) ChooseMode(GameMode.Modern);
                return;
            }

            if (Input.GetKeyDown(KeyCode.M) &&
                (_shots == 0 || _finished || IsModernMode()))
            {
                ReturnToModeSelect();
                return;
            }

            // Toggle the temporary camera-axis marker independently of zoom.
            if (IsModernMode() && Input.GetKeyDown(KeyCode.G))
            {
                modernDebugCameraCrosshair = !modernDebugCameraCrosshair;
                ShowFeedback(modernDebugCameraCrosshair
                    ? "CAMERA SIGHT DEBUG ON"
                    : "CAMERA SIGHT DEBUG OFF");
            }

            // In-Play A/B comparison without changing any projectile state.
            if (IsModernMode() && Input.GetKeyDown(KeyCode.V))
            {
                modernViewmodelRigV2 = !modernViewmodelRigV2;
                _modernRaisedPose = Input.GetMouseButton(0) ? 1f : 0f;
                _modernPrecisionPose = 0f;
                _modernBreathingWeight = 1f;
                _modernWobbleBlend = 0f;
                _modernLastCalibratedAimRayValid = false;
                HideModernHands();
                if (_modernPhysicalSight != null)
                    _modernPhysicalSight.ResetCalibration();
                RestoreModernCameraFov();
                ShowFeedback(modernViewmodelRigV2
                    ? "MODERN FPS POSES  |  IDLE / DRAW / AIM"
                    : "VIEWMODEL V1  |  ORIGINAL FRAMING");
            }

        }
        if (UsesDesktopInput() && Input.GetKeyDown(KeyCode.R) && _arrows != null)
        {
            // Restart during a held string must not turn that release into a shot.
            bool stringWasHeld = _wasDrawing || Input.GetMouseButton(0);
            ResetRound();
            _awaitStringRelease = stringWasHeld;
        }
        // During Modern Arrow Cam, the real projectile continues
        // its normal lifecycle. Pause only the player's draw/aim update.
        if (_arrowCam != null && _arrowCam.IsActive)
        {
            HideModernHands();
            if (UsesDesktopInput() && Input.GetKeyDown(KeyCode.Escape))
                _arrowCam.Stop();
            else
                return;
        }
        if (_restoreAfterArrowCam)
        {
            _restoreAfterArrowCam = false;
            ClearDrawState();
            _awaitStringRelease = UsesDesktopInput() && Input.GetMouseButton(0);
        }

        if (useOutdoorEnvironment && UsesDesktopInput() &&
            !UsesSharedDesktopBow() && Input.GetKeyDown(KeyCode.T))
        {
            _desktopTrainingEnabled = !_desktopTrainingEnabled;
            ShowFeedback(_desktopTrainingEnabled
                ? "AIM TRAINING ON" : "AIM TRAINING OFF");
            if (_desktopPracticeSight != null && !_desktopTrainingEnabled)
                _desktopPracticeSight.gameObject.SetActive(false);
        }
        if (_finished)
        {
            HideDesktopPracticeSight();
            HandleResultInput();
            return;
        }
        if (_stageBanner != null && _stageBannerUntil > 0f && Time.time >= _stageBannerUntil)
        {
            _stageBanner.text = "";
            _stageBannerUntil = 0f;
        }
        if (_feedbackLabel != null && _feedbackUntil > 0f && Time.time >= _feedbackUntil)
        {
            _feedbackLabel.text = "";
            _feedbackUntil = 0f;
        }
        UpdateImpactLabel();
        // Stage changes wait for the outgoing arrow to resolve, so the
        // departing shot is scored with the stage in which it was fired.
        // Do not accept a premature follow-up shot during this boundary.
        if (UsesProgressiveStages() && !_roundOver &&
            _stage < 2 && _shots >= StageEnd(_stage))
        {
            if (AnyArrowFlying()) return;
            AdvanceStage();
        }
        if (_roundOver)
        {
            bool anyFlying = false;
            for (int i = 0; i < _arrows.Length; i++) if (_arrows[i].IsFlying) { anyFlying = true; break; }
            // End the current frame after showing results; input must not overwrite the restart hint.
            if (!anyFlying) { FinishRound(); return; }
        }

        IJemparinganInput input = UsesDesktopInput()
            ? (IJemparinganInput)desktopInput : questInput;
        if (input == null || !input.TryReadPose(out JemparinganHandPose pose))
        {
            HideTraditionalArms();
            if (_bow != null) _bow.gameObject.SetActive(false);
            HideDesktopPracticeSight();
            HidePreview();
            ClearDrawState();
            SetInfo("Hand tracking unavailable. Try again.");
            return;
        }
        if (_roundOver)
        {
            HideTraditionalArms();
            _bow.gameObject.SetActive(false);
            HideDesktopPracticeSight();
            HidePreview();
            return;
        }
        if (!pose.BowPinched)
        {
            HideTraditionalArms();
            _bow.gameObject.SetActive(false);
            HideDesktopPracticeSight();
            HidePreview();
            ClearDrawState();
            SetInfo("Hold the bow with your left hand.");
            return;
        }

        _bow.gameObject.SetActive(true);
        if (_awaitStringRelease)
        {
            if (pose.StringPinched)
            {
                SetInfo("RELEASE STRING TO START");
                return;
            }
            _awaitStringRelease = false;
        }
        if (_introPanel != null && _introPanel.activeSelf && pose.StringPinched)
            _introPanel.SetActive(false);
        Vector3 aim = pose.AimDirection.sqrMagnitude > 0.01f ? pose.AimDirection.normalized : _lastAim;
        _lastAim = aim;
        // Resolve input once before positioning the cosmetic Modern bow:
        // the visual pose depends on draw progression, the real shot does not.
        bool drawing = pose.StringPinched;
        float distance = Mathf.Clamp(
            Vector3.Dot(pose.BowPosition - pose.StringPosition, aim),
            0f, maximumDraw);
        float power = Mathf.InverseLerp(minimumDraw, maximumDraw, distance);
        _modernDrawPower = UsesSharedDesktopBow() ? power : 0f;
        // A new draw takes priority over the previous arrow's recoil.
        if (UsesSharedDesktopBow() && drawing) _modernReleaseFeedbackAt = -1f;
        // One short low-level creak per pull, not a continuously playing
        // sound or a per-frame AudioClip allocation.
        if (UsesSharedDesktopBow() && modernLightweightGameFeel
            && drawing && power >= .35f && !_modernDrawCuePlayed)
        {
            _modernDrawCuePlayed = true;
            if (_audio != null && _modernDrawTensionSound != null)
                _audio.PlayOneShot(_modernDrawTensionSound, .20f);
        }
        if (!drawing) _modernDrawCuePlayed = false;
        Quaternion bowRotation = Quaternion.LookRotation(aim, Vector3.up);
        if (UsesTraditionalPresentation())
        {
            // Keep the gandewa recognizably canted without laying it across
            // the sightline. Arrow flight still follows pose.AimDirection.
            bowRotation *= Quaternion.Euler(0f, 0f, -8f);
        }
        // Keep the working shot origin separate from the Modern visual pose.
        // This original 0.70m / 0.15m / -0.16m anchor remains the basis for
        // trajectory, assist, preview and Arrow Cam even when V2 is enabled.
        Vector3 gameplayBowPosition = pose.BowPosition;
        Vector3 visualBowPosition = pose.BowPosition;
        if (UsesTraditionalPresentation() && UsesDesktopInput() && Camera.main != null)
        {
            Transform eye = Camera.main.transform;
            gameplayBowPosition = eye.position + aim * .70f
                + eye.right * .15f - eye.up * .16f;
            visualBowPosition = gameplayBowPosition;
            if (UsesSharedDesktopBow() && modernViewmodelRigV2)
            {
                // A game-first three-state pose, not a small static offset.
                // Idle rests down/right. Aiming or starting the draw raises
                // the bow; full draw settles into the final stable sightline.
                ApplyModernFpsBowPose(eye, aim, drawing, power,
                    ref visualBowPosition, ref bowRotation);
            }
        }
        float visualBowScale = 1f;
        if (UsesSharedDesktopBow() && modernViewmodelRigV2
            && Camera.main != null)
        {
            // Approximate an independent viewmodel FOV without changing the
            // target/world FOV: multiply visual scale by the ratio of tangents.
            // Keep the factor bounded to avoid extreme near-plane clipping.
            float worldFov = Camera.main.fieldOfView;
            float lensFactor = Mathf.Tan(worldFov * Mathf.Deg2Rad * .5f) /
                Mathf.Tan(modernViewmodelFov * Mathf.Deg2Rad * .5f);
            visualBowScale = modernBowVisualScale *
                Mathf.Clamp(lensFactor, .76f, 1.25f);
            // Lowered idle pose is slightly smaller; raise to full size
            // during aiming. A/B V1 still uses untouched 1x scale.
            float raised = Mathf.SmoothStep(0f, 1f, _modernRaisedPose);
            visualBowScale *= Mathf.Lerp(modernIdleBowScale, 1f, raised);
            if (modernUseReferenceFraming)
            {
                // The landmark solver accounts for the real FOV and the
                // upper-tip depth. Never stack the old virtual FOV/boost.
                visualBowScale = _modernReferenceCalculatedScale *
                    Mathf.Lerp(modernIdleBowScale, 1f, raised);
            }
        }
        // Breathing is only a viewmodel pose, on top of the solved frame.
        // Never let the 3D optical + wander once the bow is almost drawn:
        // the established calibrated precision aim must remain trustworthy.
        if (UsesSharedDesktopBow() && modernViewmodelRigV2 &&
            modernUseReferenceFraming && UsesDesktopInput() &&
            modernSubtleBreathingSway)
            ApplyModernBreathingSway(drawing, power,
                ref visualBowPosition, ref bowRotation);

        // Unlike idle breathing (cosmetic only), full-draw aim wobble
        // genuinely shifts the visible +. Below, the projectile solver
        // receives that SAME eye-to-reticle ray. Never change the camera.
        if (UsesSharedDesktopBow() && modernViewmodelRigV2 &&
            modernUseReferenceFraming && UsesDesktopInput() &&
            modernUsePhysicalSight && modernAimWobble)
            ApplyModernOpticalAimWobble(drawing, power,
                ref visualBowPosition, ref bowRotation);
        else
            _modernWobbleBlend = 0f;

        // Very small, damped cosmetic release recoil. Applied ONLY to the
        // bow viewmodel after aim is resolved; it does not rotate the
        // camera, alter gameplayBowPosition, or modify projectile ballistics.
        if (UsesSharedDesktopBow() && modernViewmodelRigV2 &&
            modernLightweightGameFeel && _modernReleaseFeedbackAt >= 0f)
        {
            float elapsed = Time.time - _modernReleaseFeedbackAt;
            if (elapsed < 0f || elapsed >= .20f)
                _modernReleaseFeedbackAt = -1f;
            else
            {
                float envelope = Mathf.Exp(-elapsed * 21f);
                float wobble = Mathf.Sin(elapsed * 50f) * envelope;
                bowRotation *= Quaternion.Euler(
                    modernReleaseRecoilDegrees * wobble,
                    0f, modernReleaseRecoilDegrees * .55f * wobble);
                visualBowPosition -= eyeForwardForModernRecoil(_playerCamera) *
                    (.008f * envelope);
            }
        }
        _bow.localScale = Vector3.one * visualBowScale;
        _bow.SetPositionAndRotation(visualBowPosition, bowRotation);
        PresentModernPhysicalSight();
        Vector3 bowOrigin = (UsesTraditionalPresentation() && UsesDesktopInput())
            ? gameplayBowPosition
            : pose.BowPosition;
        Vector3 launchAim = aim;
        _modernSightZeroed = false;
        if (UsesTraditionalPresentation() && UsesDesktopInput() && Camera.main != null)
        {
            if (IsModernMode())
            {
                // Use the shot's ACTUAL speed, including on the release frame
                // when DesktopBowInput has already reset its live draw pose.
                // The existing gameplay origin is separate from the large
                // cosmetic bow; correct camera parallax AND gravitational
                // arrow-tip drop at the crosshair's target-plane intersection.
                float releasePower = drawing ? power :
                    Mathf.InverseLerp(minimumDraw, maximumDraw, _lastDraw);
                float speed = Mathf.Lerp(minimumArrowSpeed, maximumArrowSpeed,
                    releasePower);
                Vector3 correctedDirection;
                bool crosshairOnBoard;
                // Save the last *visible* sight ray before release. Desktop
                // input resets draw power on MouseUp and the cosmetic bow
                // starts lowering that same frame, but the shot must follow
                // the plus position the player saw while holding LMB.
                bool useReticleAim = false;
                Vector3 reticleRay = Camera.main.transform.forward;
                if (modernAimWobble && modernViewmodelRigV2 &&
                    modernUseReferenceFraming && modernUsePhysicalSight &&
                    _modernPhysicalSight != null)
                {
                    if (drawing)
                    {
                        if (_modernPhysicalSight.TryGetCalibratedRay(
                            Camera.main, out reticleRay))
                        {
                            _modernLastCalibratedAimRay = reticleRay;
                            _modernLastCalibratedAimRayValid = true;
                            useReticleAim = true;
                        }
                        else
                            _modernLastCalibratedAimRayValid = false;
                    }
                    else if (_wasDrawing && _modernLastCalibratedAimRayValid)
                    {
                        reticleRay = _modernLastCalibratedAimRay;
                        useReticleAim = true;
                    }
                }
                else
                    _modernLastCalibratedAimRayValid = false;

                bool zeroed = useReticleAim
                    ? JemparinganModernAim.TryZeroToSightRay(Camera.main,
                        reticleRay, gameplayBowPosition + aim * .25f,
                        speed, simulatedGravity, outdoorArrowLifetime,
                        _modernTargets, out correctedDirection,
                        out crosshairOnBoard)
                    : JemparinganModernAim.TryZeroToCameraRay(Camera.main,
                        gameplayBowPosition + aim * .25f,
                        speed, simulatedGravity, outdoorArrowLifetime,
                        _modernTargets, out correctedDirection,
                        out crosshairOnBoard);
                if (zeroed)
                {
                    launchAim = correctedDirection;
                    _modernSightZeroed = crosshairOnBoard;
                }
                else
                {
                    // Preserve the visible ray (or original camera ray)
                    // even when the requested ballistic arc is unreachable.
                    Vector3 oldFocus = Camera.main.transform.position +
                        (useReticleAim ? reticleRay : aim) * 10f;
                    launchAim = (oldFocus -
                        (gameplayBowPosition + aim * .25f)).normalized;
                }
            }
            else
            {
                // Traditional shares the same calibrated physical sight and
                // ballistic shot origin, but intersects its BANDULAN plane
                // instead of Modern's circular target planes.
                float releasePower = drawing ? power :
                    Mathf.InverseLerp(minimumDraw, maximumDraw, _lastDraw);
                float speed = Mathf.Lerp(minimumArrowSpeed, maximumArrowSpeed,
                    releasePower);
                Vector3 shotOrigin = gameplayBowPosition + aim * .25f;
                Vector3 sightRay = aim;
                if (modernAimWobble && modernViewmodelRigV2 &&
                    modernUseReferenceFraming && modernUsePhysicalSight &&
                    _modernPhysicalSight != null)
                {
                    if (drawing)
                    {
                        if (_modernPhysicalSight.TryGetCalibratedRay(
                            Camera.main, out Vector3 calibratedRay))
                        {
                            _modernLastCalibratedAimRay = calibratedRay;
                            _modernLastCalibratedAimRayValid = true;
                            sightRay = calibratedRay;
                        }
                        else
                            _modernLastCalibratedAimRayValid = false;
                    }
                    else if (_wasDrawing && _modernLastCalibratedAimRayValid)
                        sightRay = _modernLastCalibratedAimRay;
                }
                else
                    _modernLastCalibratedAimRayValid = false;

                Vector3 correctedDirection;
                bool sightOnBandulan;
                if (_target != null &&
                    JemparinganModernAim.TryZeroToBandulanSightRay(
                        Camera.main, sightRay, shotOrigin, speed,
                        simulatedGravity, outdoorArrowLifetime,
                        _target.transform, out correctedDirection,
                        out sightOnBandulan))
                {
                    launchAim = correctedDirection;
                    _modernSightZeroed = sightOnBandulan;
                }
                else
                {
                    // Unreachable shots stay misses; never snap towards a
                    // scoring zone or auto-correct the player's aim.
                    float bandulanDistance = _target != null
                        ? Vector3.Distance(Camera.main.transform.position,
                            _target.transform.position) : 10f;
                    Vector3 sightFocus =
                        Camera.main.transform.position + sightRay * bandulanDistance;
                    launchAim = (sightFocus - shotOrigin).normalized;
                }
            }
        }
        UpdateDesktopPracticeSight(bowOrigin + aim * .25f,
            launchAim, Mathf.Lerp(minimumArrowSpeed, maximumArrowSpeed, power), drawing);

        if (drawing)
        {
            if (!_wasDrawing)
            {
                _drawStartedAt = Time.time;
                BeginFocus(aim, bowOrigin);
            }
            _lastDraw = distance;
            UpdateFocus(aim, bowOrigin, power);
            float pull = Mathf.Clamp(distance, 0f, maximumDraw);
            _stringMarker.localPosition = TraditionalNockLocalPosition(pull);
            UpdateBowStrings(pull);
            UpdateNockedArrow(pull, true);
            ShowPreview(bowOrigin + aim * 0.25f, launchAim, power);
                int step = Mathf.RoundToInt(power * 10f);
            bool calm = Time.time <= _focusReadyUntil;
            // RASA must update even at constant draw power.
            if (step != _lastPowerStep || calm != _lastStableSignal)
            {
                _lastPowerStep = step;
                _lastStableSignal = calm;
                SetInfo("DRAW " + (step * 10) + "%   |   " + (calm ? "RASA: STEADY" : "RASA: FOCUS"));
            }
        }
        else
        {
            _lastPowerStep = -1;
            _lastStableSignal = false;
            _stringMarker.localPosition = TraditionalNockLocalPosition(0f);
            UpdateBowStrings(0f);
            UpdateNockedArrow(0f, true);
            HidePreview();
            SetInfo("READY  •  Hold left mouse to draw; release to shoot. Hold right mouse to aim.");
        }

        // The contact targets must be current before posing hands. The old
        // call ran before the string moved and smoothed the hand away from it.
        UpdateTraditionalPresentation(drawing);

        // One uninterrupted draw-to-release transition fires exactly once.
        // Tracking loss / bow-unpinch resets without firing.
        if (_wasBowHeld && _wasDrawing && !drawing)
        {
            if (_lastDraw >= minimumDraw && Time.time - _drawStartedAt >= minimumHoldSeconds)
            {
                // Hide the attached arrow for this release frame; the pooled
                // projectile is the only arrow visible in flight.
                UpdateNockedArrow(0f, false);
                Shoot(bowOrigin + aim * 0.25f, launchAim, _lastDraw, Time.time <= _focusReadyUntil);
            }
            else
                ShowFeedback("DRAW FURTHER");
            _lastDraw = 0f;
            // One cached reticle ray belongs to exactly one release,
            // including aborted short pulls. Never carry it to next shot.
            _modernLastCalibratedAimRayValid = false;
        }
        if (!drawing) ClearFocus();
        _wasDrawing = drawing;
        _wasBowHeld = true;
    }

    // Result replay: release the string, then hold it for 0.7 seconds.
    // Editor also supports R as an immediate debug shortcut.
    private void HandleResultInput()
    {
        IJemparinganInput input = UsesDesktopInput()
            ? (IJemparinganInput)desktopInput : questInput;
        JemparinganHandPose pose;
        if (input == null || !input.TryReadPose(out pose) || !pose.BowPinched)
        {
            _replayArmed = false;
            _replayHoldStartedAt = -1f;
            return;
        }

        if (!pose.StringPinched)
        {
            _replayArmed = true;
            _replayHoldStartedAt = -1f;
        }
        else if (_replayArmed)
        {
            if (_replayHoldStartedAt < 0f) _replayHoldStartedAt = Time.time;
            if (Time.time - _replayHoldStartedAt >= .70f)
            {
                ResetRound();
                // A held replay gesture cannot immediately count as a shot.
                _awaitStringRelease = true;
                SetInfo("RELEASE STRING TO START");
            }
        }
    }

    private void ClearDrawState()
    {
        _wasDrawing = _wasBowHeld = false;
        _lastDraw = 0f;
        _modernDrawPower = 0f;
        _modernDrawCuePlayed = false;
        _modernLastCalibratedAimRayValid = false;
        _modernWobbleBlend = 0f;
        ClearFocus();
        _drawStartedAt = 0f;
        _lastPowerStep = -1;
        _lastStableSignal = false;
    }

    // Each draw samples AIM and the bow hand, rather than comparing single-frame
    // angular velocity. A slowly moving target can still be followed smoothly.
    private void BeginFocus(Vector3 aim, Vector3 bowPosition)
    {
        _focusAimAnchor = aim;
        _focusBowAnchor = bowPosition;
        _focusProgress = 0f;
        _focusReadyUntil = -1f;
        _focusCuePlayed = false;
        _lastFocusStep = -1;
        _lastStableSignal = false;
    }

    private void ClearFocus()
    {
        _focusProgress = 0f;
        _focusReadyUntil = -1f;
        _focusCuePlayed = false;
        _lastFocusStep = -1;
        if (_focusRoot != null) _focusRoot.gameObject.SetActive(false);
    }

    private void UpdateFocus(Vector3 aim, Vector3 bowPosition, float power)
    {
        if (power < .35f)
        {
            _focusProgress = Mathf.Max(0f, _focusProgress - Time.deltaTime * 2f);
            _focusReadyUntil = -1f;
            _focusAimAnchor = aim;
            _focusBowAnchor = bowPosition;
        }
        else
        {
            float angle = Vector3.Angle(_focusAimAnchor, aim);
            float drift = Vector3.Distance(_focusBowAnchor, bowPosition);
            bool largeMovement = angle > focusAngularTolerance * 2.5f ||
                drift > focusPositionTolerance * 2f;

            if (largeMovement)
            {
                // Reset only on an intentional re-aim or big hand jump.
                _focusProgress = 0f;
                _focusReadyUntil = -1f;
                _focusAimAnchor = aim;
                _focusBowAnchor = bowPosition;
            }
            else
            {
                bool settled = angle <= focusAngularTolerance &&
                    drift <= focusPositionTolerance;
                float change = Time.deltaTime / (settled ? focusTimeSeconds : .35f);
                _focusProgress = Mathf.MoveTowards(_focusProgress, settled ? 1f : 0f, change);
                // Follow deliberate slow movements; don't chase noisy tracking.
                _focusAimAnchor = Vector3.Slerp(_focusAimAnchor, aim, Mathf.Clamp01(Time.deltaTime * 1.7f));
                _focusBowAnchor = Vector3.Lerp(_focusBowAnchor, bowPosition, Mathf.Clamp01(Time.deltaTime * 1.7f));

                if (settled && _focusProgress >= .999f)
                {
                    // A short grace window covers natural release movement.
                    _focusReadyUntil = Time.time + .22f;
                    if (!_focusCuePlayed && _audio != null && _focusSound != null)
                    {
                        _audio.PlayOneShot(_focusSound, .18f);
                        _focusCuePlayed = true;
                    }
                }
            }
        }
        UpdateFocusIndicator(power);
    }

    private void UpdateFocusIndicator(float power)
    {
        if (_focusRoot == null || _focusRenderers == null) return;
        bool visible = power >= .35f && !UsesSharedDesktopBow();
        if (_focusRoot.gameObject.activeSelf != visible)
            _focusRoot.gameObject.SetActive(visible);
        if (!visible) { _lastFocusStep = -1; return; }

        int step = Mathf.Clamp(Mathf.FloorToInt(_focusProgress * _focusRenderers.Length + .001f),
            0, _focusRenderers.Length);
        bool ready = Time.time <= _focusReadyUntil;
        if (step == _lastFocusStep && ready == _lastStableSignal) return;
        _lastFocusStep = step;
        for (int i = 0; i < _focusRenderers.Length; i++)
        {
            JemparinganArt.Tone tone = i < step
                ? (ready ? JemparinganArt.Tone.Gold : JemparinganArt.Tone.Jade)
                : JemparinganArt.Tone.Paper;
            _focusRenderers[i].sharedMaterial = JemparinganArt.Material(tone);
        }
    }

    // Lightweight full-draw motor wobble. The calibrated optical + moves
    // with the actual bow and supplies the SAME direction used by the
    // ballistic zeroing code. This is NOT a camera shake, random miss,
    // or wind; at 0.10 degrees the projected displacement is ~1-3px.
    private void ApplyModernOpticalAimWobble(bool drawing, float drawPower,
        ref Vector3 visualBowPosition, ref Quaternion bowRotation)
    {
        if (!drawing || drawPower < modernPrecisionDrawThreshold ||
            !ModernPrecisionAimRequested() ||
            _modernPhysicalSight == null || !_modernPhysicalSight.HasCalibration ||
            _playerCamera == null || modernAimWobbleDegrees <= 0f)
        {
            _modernWobbleBlend = 0f;
            return;
        }

        // Ease in after the scope first calibrates. No procedural animation
        // runs before that moment, ensuring the initial + is zeroed to the
        // existing camera-axis reference without any transient offset.
        _modernWobbleBlend = Mathf.MoveTowards(
            _modernWobbleBlend, 1f, 2.6f * Time.deltaTime);
        float strength = modernAimWobbleDegrees *
            Mathf.SmoothStep(0f, 1f, _modernWobbleBlend);
        if (strength <= 0f) return;

        // Two smooth, mismatched breathing/muscle periods. The only aim
        // displacement comes from the literal physical sight location;
        // no invisible variation is added to a projectile after zeroing.
        float phase = Time.time * 1.37f;
        float horizontalDegrees = Mathf.Sin(phase + .2f) * strength;
        float verticalDegrees = Mathf.Sin(phase * .73f + 1.2f) *
            strength * .82f;
        Transform eye = _playerCamera.transform;
        float depth = Mathf.Max(.35f, Vector3.Dot(
            visualBowPosition - eye.position, eye.forward));
        visualBowPosition += eye.right * (depth * Mathf.Tan(
            horizontalDegrees * Mathf.Deg2Rad));
        visualBowPosition += eye.up * (depth * Mathf.Tan(
            verticalDegrees * Mathf.Deg2Rad));
        // Small, visible pressure change on the grip; the reticle ray
        // is read AFTER this transform, so the shot stays honest.
        bowRotation *= Quaternion.Euler(
            verticalDegrees * .18f, 0f, horizontalDegrees * .14f);
    }

    // Slowly breathing shooter, not wind and not random aim jitter.
    // Called only for the Modern V2 cosmetic bow before the normal release
    // recoil is composed. Does not touch camera transform, gameplay bow
    // origin, launch aim, projectile, scoring or target hit plane.
    private void ApplyModernBreathingSway(bool drawing, float drawPower,
        ref Vector3 visualBowPosition, ref Quaternion bowRotation)
    {
        // Power fades the effect before full draw. A hard guard guarantees
        // *zero* sway when the optical reticle is being calibrated.
        if (ModernPrecisionAimRequested() || (drawing && drawPower >= .90f))
        {
            _modernBreathingWeight = 0f;
            return;
        }

        float drawFade = drawing
            ? 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(.12f, .90f, drawPower))
            : 1f;
        float raiseFade = Mathf.Lerp(1f, .23f,
            Mathf.SmoothStep(0f, 1f, _modernRaisedPose));
        float targetWeight = drawFade * raiseFade;
        _modernBreathingWeight = Mathf.MoveTowards(_modernBreathingWeight,
            targetWeight, 3.5f * Time.deltaTime);
        float weight = _modernBreathingWeight;
        if (weight <= .0001f || modernIdleSwayDegrees <= 0f) return;

        // One 4.5s breathing cycle plus a smaller, slower arm drift.
        // Continuous sinusoids avoid distracting random jitter.
        float phase = Time.time * (2f * Mathf.PI /
            Mathf.Max(3f, modernBreathingCycleSeconds));
        float inhale = Mathf.Sin(phase);
        float armDrift = Mathf.Sin(phase * .63f + 1.05f);
        float amplitude = modernIdleSwayDegrees * weight;
        bowRotation *= Quaternion.Euler(.86f * inhale * amplitude,
            0f, .58f * armDrift * amplitude);
        if (_playerCamera != null)
            visualBowPosition += _playerCamera.transform.up *
                (.0024f * inhale * weight);
    }

    // Only the Modern desktop viewmodel uses this pose animation. No camera,
    // gameplay bow anchor, draw physics, hit tests or target movement changes.
    // Inspiration: readable three-stage first-person bow framing found in FPS
    // archery games, using Jemparingan's own original procedural geometry.
    private void ApplyModernFpsBowPose(Transform eye, Vector3 aim,
        bool drawing, float drawPower, ref Vector3 visualBowPosition,
        ref Quaternion bowRotation)
    {
        // LMB raises the bow. As its quick draw animation completes, settle
        // automatically onto the calibrated sight axis; mouse look works
        // throughout the hold without RMB.
        _modernRaisedPose = Mathf.MoveTowards(_modernRaisedPose,
            drawing ? 1f : 0f, modernPoseTransitionSpeed * Time.deltaTime);
        float raised = Mathf.SmoothStep(0f, 1f, _modernRaisedPose);
        _modernPrecisionPose = Mathf.MoveTowards(_modernPrecisionPose,
            ModernPrecisionAimRequested() ? 1f : 0f,
            modernPrecisionPoseSpeed * Time.deltaTime);
        float precision = Mathf.SmoothStep(0f, 1f, _modernPrecisionPose);

        Vector3 aimPose = eye.position + aim * modernBowForwardDistance +
            eye.right * modernBowRightOffset - eye.up * modernBowDownOffset;
        Quaternion referenceAimRotation = bowRotation;
        if (modernUseReferenceFraming && _playerCamera != null)
        {
            SolveModernScreenshotBowPose(_playerCamera, out aimPose,
                out referenceAimRotation, out _modernReferenceCalculatedScale);
        }
        // While drawing, hold the grip just outside the calibrated sightline.
        // Completing the pull moves it into the exact screenshot-framed aim
        // pose automatically, without a separate aiming button.
        Vector3 preAimPose = modernUseReferenceFraming
            ? aimPose + eye.right * modernPreAimRightShift -
                eye.up * modernPreAimLowering
            : aimPose;
        Vector3 idle = preAimPose + aim * modernIdleForwardShift +
            eye.right * modernIdleRightShift -
            eye.up * modernIdleLowering;

        // Full draw has a small final settle toward the aiming lane. The
        // screenshot preset already places the grip, so don't over-correct
        // its screen-space anchor on top of this deliberate composition.
        // The actual nock and right hand still follow the visual string.
        float fullDraw = drawing
            ? Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(.35f, .95f, drawPower)) * raised
            : 0f;
        visualBowPosition = Vector3.Lerp(idle, preAimPose, raised);
        if (modernUseReferenceFraming)
            visualBowPosition = Vector3.Lerp(
                visualBowPosition, aimPose, precision);
        if (!modernUseReferenceFraming)
            visualBowPosition += -eye.right * (.035f * fullDraw) +
                eye.up * (.025f * fullDraw);

        // Modern target archery keeps the riser almost upright at full
        // draw (~2 degrees), with a little more relaxed cant at idle.
        // The camera/aim and actual launch direction are still independent.
        if (modernUseReferenceFraming)
            bowRotation = referenceAimRotation *
                Quaternion.Euler(0f, 0f,
                    Mathf.Lerp(-4f, 0f, raised) +
                    Mathf.Lerp(-2f, 0f, precision));
        else
            bowRotation *= Quaternion.Euler(0f, 0f,
                Mathf.Lerp(-11f, 0f, raised) + 2f * fullDraw);
    }

    // Use an almost upright 2-degree target-archery cant, not a
    // lean inherited from the older screenshot composition. Keep the screenshot's
    // grip and upper-tip HEIGHT, then solve scale at a fixed subtle roll.
    // With dy = tipViewportY - gripViewportY, k=2*tan(FOV/2),
    // scaledTipY/(depth + scaledTipZ) = dy*k.
    // Thus scale = dy*k*depth/(rotatedTipY - dy*k*tipZ).
    // The horizontal upper-tip landmark is intentionally released so that
    // the top limb can straighten rather than being forced to lean right.
    private void SolveModernScreenshotBowPose(Camera camera,
        out Vector3 position, out Quaternion rotation, out float scale)
    {
        float depth = Mathf.Max(.40f, modernReferenceGripDepth);
        float kY = 2f * Mathf.Tan(camera.fieldOfView *
            Mathf.Deg2Rad * .5f);
        float deltaViewportY = modernReferenceTipViewportY -
            modernReferenceGripViewportY;
        Vector3 localTip = JemparinganBowVisual.UpperTip;
        float roll = Mathf.Clamp(modernBowCantDegrees, -12f, 12f);
        float radians = roll * Mathf.Deg2Rad;
        float rotatedTipY = Mathf.Sin(radians) * localTip.x +
            Mathf.Cos(radians) * localTip.y;
        float numerator = deltaViewportY * kY * depth;
        float denominator = rotatedTipY -
            deltaViewportY * kY * localTip.z;
        scale = Mathf.Clamp(numerator / Mathf.Max(.05f, denominator),
            .70f, 2f);

        position = camera.ViewportToWorldPoint(new Vector3(
            modernReferenceGripViewportX,
            modernReferenceGripViewportY, depth));
        rotation = camera.transform.rotation *
            Quaternion.Euler(0f, 0f, roll);
    }

    private static Vector3 eyeForwardForModernRecoil(Camera eye)
    {
        return eye != null ? eye.transform.forward : Vector3.forward;
    }

    private void Shoot(Vector3 origin, Vector3 rawAim, float draw, bool stable)
    {
        if (_shots >= arrowsPerRound) return;
        float power = Mathf.InverseLerp(minimumDraw, maximumDraw, draw);
        float speed = Mathf.Lerp(minimumArrowSpeed, maximumArrowSpeed, power);
        Vector3 aim = GetAssistedAim(origin, rawAim, speed);
        JemparinganArrow arrow = _arrows[_poolCursor];
        _poolCursor = (_poolCursor + 1) % _arrows.Length;
        // Stability is captured on the arrow so it survives subsequent shots.
        arrow.StableRelease = stable;
        if (IsModernMode())
        {
            arrow.Launch(origin, aim * speed, simulatedGravity, null, null, OnArrowMiss,
                outdoorArrowLifetime, _modernTargets, OnModernArrowImpact);
        }
        else
        {
            arrow.Launch(origin, aim * speed, simulatedGravity, _target,
                OnArrowImpact, OnArrowMiss,
                useOutdoorEnvironment ? outdoorArrowLifetime : 1.6f);
        }
        // Both desktop modes share the same cinematic projectile follow.
        bool useArrowCam = UsesSharedDesktopBow() && modernEveryShotArrowCam;
        if (useArrowCam && UsesDesktopInput() &&
            desktopInput != null && Camera.main != null)
        {
            // Arrow Cam inherits MainCamera settings via CopyFrom. Restore
            // the original FOV before its snapshot so cinematic framing
            // stays identical to the pre-zoom gameplay baseline.
            if (UsesSharedDesktopBow()) RestoreModernCameraFov();
            if (_arrowCam == null)
                _arrowCam = gameObject.AddComponent<JemparinganArrowFollowCamera>();
            // Do not steal an existing follow during an automated rapid-fire test.
            if (!_arrowCam.IsActive &&
                _arrowCam.Begin(arrow, Camera.main, desktopInput))
            {
                _restoreAfterArrowCam = true;
                HideModernHands();
            }
        }
        _shots++;
        if (UsesSharedDesktopBow() && modernLightweightGameFeel)
        {
            _modernReleaseFeedbackAt = Time.time;
            if (_audio != null && _modernReleaseSound != null)
                _audio.PlayOneShot(_modernReleaseSound, .43f);
        }
        else if (_audio != null && _shotSound != null)
            _audio.PlayOneShot(_shotSound, .48f);
        ShowFeedback("ARROW RELEASED");
        if (_shots >= arrowsPerRound) _roundOver = true;
        UpdateHUD();
    }

    private bool IsDesktopTrainingAimActive()
    {
        // Desktop Editor training only. Quest input and Traditional shooting
        // retain the original assistance behavior and physics.
        return useOutdoorEnvironment && UsesDesktopInput() &&
            _desktopTrainingEnabled;
    }

    // Only close-to-target desktop practice shots are corrected. Aim at the
    // visible bandulan (not above it); the training guide compensates drop.
    private Vector3 GetAssistedAim(Vector3 origin, Vector3 aim, float speed)
    {
        // Ring scoring is based on where the actual flight intersects each
        // circle; the hidden bandulan must never magnetize Modern shots.
        if (IsModernMode() || UsesSharedDesktopBow()) return aim;
        if (_target == null || aimAssist <= 0f) return aim;

        if (IsDesktopTrainingAimActive())
        {
            Vector3 ballistic, sightTarget;
            if (TryGetPracticeBallisticAim(origin, speed,
                out ballistic, out sightTarget))
            {
                float error = Vector3.Angle(aim, sightTarget - origin);
                if (error <= 2.8f) return ballistic;
            }
            return aim;
        }

        // Original non-training assist (legacy and Quest) stays unchanged.
        Vector3 delta = _target.transform.position - origin;
        float flightTime = Mathf.Max(0.05f, delta.magnitude / speed);
        Vector3 direction = (delta + Vector3.up *
            (0.5f * simulatedGravity * flightTime * flightTime)).normalized;
        float angularError = Vector3.Angle(aim, direction);
        if (angularError > 8f) return aim;
        // Traditional uses a subtle assist, without magnetic
        // ballistic snapping unless training is explicitly enabled with T.
        float limitedAssist = UsesTraditionalPresentation() ?
            Mathf.Min(aimAssist, .20f) : aimAssist;
        float strength = limitedAssist * Mathf.InverseLerp(8f, 0f, angularError);
        return Vector3.Slerp(aim, direction, strength).normalized;
    }

    private bool TryGetPracticeBallisticAim(Vector3 origin, float speed,
        out Vector3 direction, out Vector3 sightTarget)
    {
        direction = Vector3.forward;
        sightTarget = Vector3.zero;
        if (_target == null || speed < 0.1f) return false;

        // Iteration matters on stages II-IV because the bandulan sways while
        // the arrow is flying. Time-to-target and lead are solved together.
        float travelTime = Vector3.Distance(origin, _target.transform.position) / speed;
        float gravity = Mathf.Max(0f, simulatedGravity);
        for (int iteration = 0; iteration < 4; iteration++)
        {
            sightTarget = _target.PredictCenter(travelTime);
            Vector3 delta = sightTarget - origin;
            Vector3 flat = Vector3.ProjectOnPlane(delta, Vector3.up);
            float horizontal = flat.magnitude;
            if (horizontal < 0.01f) return false;
            if (gravity < 0.001f)
            {
                direction = delta.normalized;
                return true;
            }

            float speed2 = speed * speed;
            float discriminant = speed2 * speed2 -
                gravity * (gravity * horizontal * horizontal +
                    2f * delta.y * speed2);
            if (discriminant <= 0f) return false; // Unreachable shot.
            float tangent = (speed2 - Mathf.Sqrt(discriminant)) /
                (gravity * horizontal);
            float angle = Mathf.Atan(tangent);
            float cosine = Mathf.Cos(angle);
            direction = (flat.normalized * cosine +
                Vector3.up * Mathf.Sin(angle)).normalized;
            travelTime = horizontal / Mathf.Max(0.01f, speed * cosine);
        }
        return true;
    }


    private void OnModernArrowImpact(JemparinganArrow arrow,
        JemparinganModernTarget target, int points)
    {
        if (!IsModernMode()) return;
        _hits++;
        _score += points;
        if (points == 10) _bullseyes++;
        _best = Mathf.Max(_best, _score);
        if (_audio != null)
            _audio.PlayOneShot(points == 10 ? _bullseyeSound : _hitSound,
                points == 10 ? .54f : .45f);
        ShowFeedback("TARGET " + target.TargetNumber + "  |  +" + points +
            (points == 10 ? "  BULLSEYE!" : "  POINTS"),
            points == 10 ? new Color(.98f, .81f, .40f) :
            new Color(.40f, .94f, .75f));
        UpdateHUD();
    }

    private void OnArrowImpact(JemparinganArrow arrow, bool bullseye)
    {
        if (!IsTraditionalMode()) return;
        // Score the zone classified by the existing target collision.
        JemparinganTarget.TraditionalZone zone = _target.LastTraditionalZone;
        int points = JemparinganScoreRules.TraditionalPointsForZone(zone);
        _hits++;
        if (zone == JemparinganTarget.TraditionalZone.Molo) _moloHits++;
        else if (zone == JemparinganTarget.TraditionalZone.Jangga) _janggaHits++;
        else if (zone == JemparinganTarget.TraditionalZone.Awak) _awakHits++;
        _score += points;
        if (_score > _best) _best = _score;
        _traditionalImpactAt = Time.time;
        _traditionalImpactZone = zone;
        // Emphasize a Molo hit using the existing procedural clips,
        // without adding voices or overlapping new sound sources.
        if (_audio != null)
        {
            float volume = zone == JemparinganTarget.TraditionalZone.Molo
                ? .56f : zone == JemparinganTarget.TraditionalZone.Jangga
                    ? .46f : .38f;
            AudioClip impactSound = zone == JemparinganTarget.TraditionalZone.Molo
                ? _bullseyeSound : _hitSound;
            if (impactSound != null)
                _audio.PlayOneShot(impactSound, volume);
        }
        string zoneName = zone == JemparinganTarget.TraditionalZone.Molo ? "MOLO" :
            zone == JemparinganTarget.TraditionalZone.Jangga ? "JANGGA" : "AWAK";
        ShowImpactLabel("+" + points + "  " + zoneName,
            zone == JemparinganTarget.TraditionalZone.Molo ?
            new Color(.98f, .81f, .40f) : new Color(.40f, .94f, .75f));
        ShowFeedback(zoneName + "  +" + points);
        UpdateHUD();
    }

    private void OnArrowMiss(JemparinganArrow arrow)
    {
        if (_missSound != null) _audio.PlayOneShot(_missSound, 0.16f);
        ShowFeedback("MISS  •  TRY AGAIN", new Color(.98f, .55f, .46f));
        UpdateHUD();
    }



    private void FinishRound()
    {
        // Wait until the final projectile resolves before showing results.
        _roundOver = false;
        _finished = true;
        bool newRecord = _score > _previousBest;
        if (newRecord)
        {
            PlayerPrefs.SetInt(BestKey(), _score);
            PlayerPrefs.Save();
        }
        _best = Mathf.Max(_previousBest, _score);
        _scoreLabel.text = "COMPLETE  |  SCORE " + _score + "  |  " + _hits + " HITS";
        if (_phaseLabel != null) _phaseLabel.text = "ROUND COMPLETE";
        if (_feedbackLabel != null) _feedbackLabel.text = "";
        _feedbackUntil = 0f;
        SetInfo(UsesDesktopInput() ? "PRESS R OR HOLD LEFT MOUSE TO RESTART"
            : "RELEASE STRING, THEN HOLD TO RESTART");
        _bow.gameObject.SetActive(false);
        HideTraditionalArms();
        HideModernHands();
        HidePreview();
        if (_impactLabel != null) _impactLabel.text = "";
        _impactLabelUntil = 0f;
        if (_introPanel != null) _introPanel.SetActive(false);
        if (_stageBanner != null) _stageBanner.text = "";
        _stageBannerUntil = 0f;
        _stageIntroStartedAt = -1f;
        _stageIntroUntil = -1f;
        _replayArmed = false;
        _replayHoldStartedAt = -1f;

        if (_resultsPanel == null) return;
        if (IsModernMode())
        {
            // Modern desktop uses its IMGUI results screen.
            _resultsTitle.text = "COMPLETE";
            _resultsDetails.text = "SCORE " + _score +
                "  |  BEST " + _best +
                "\nACCURACY " +
                JemparinganScoreRules.AccuracyPercent(_hits, _shots) + "%";
            _resultsHint.text = UsesDesktopInput() ? "" : "RELEASE + HOLD TO RETRY";
            _resultsPanel.SetActive(!UsesDesktopInput());
            _scoreLabel.text = UsesDesktopInput() ? "" : "SCORE " + _score;
            if (_phaseLabel != null)
                _phaseLabel.text = UsesDesktopInput() ? "" : "COMPLETE";
            return;
        }

        int accuracy = JemparinganScoreRules.AccuracyPercent(_hits, _shots);
        _resultsTitle.text = "TRADITIONAL  •  COMPLETE";
        _resultsDetails.text = "SCORE " + _score + "  |  ACCURACY " + accuracy + "%" +
            "\nMOLO " + _moloHits + "  |  JANGGA " + _janggaHits +
            "\nAWAK " + _awakHits + "  |  BEST " + _best;
        _resultsHint.text = UsesDesktopInput()
            ? (newRecord ? "NEW BEST  |  R: RETRY  |  M: MODES"
                : "R: RETRY  |  M: MODES")
            : "RELEASE + HOLD TO RETRY";
        _resultsPanel.SetActive(!UsesDesktopInput());
    }

    private void ShowImpactLabel(string message, Color color)
    {
        // Both desktop modes rely on the compact score HUD and impact VFX,
        // not floating text in front of the target.
        if (_impactLabel == null || UsesSharedDesktopBow()) return;
        _impactLabel.text = message;
        _impactLabel.color = color;
        _impactLabelStart = Time.time;
        _impactLabelUntil = Time.time + .85f;
        _impactLabel.transform.localPosition = _impactLabelBasePosition;
    }

    private void UpdateImpactLabel()
    {
        if (_impactLabel == null || _impactLabelUntil <= 0f) return;
        if (Time.time >= _impactLabelUntil)
        {
            _impactLabel.text = "";
            _impactLabelUntil = 0f;
            return;
        }
        float progress = (Time.time - _impactLabelStart) / .85f;
        _impactLabel.transform.localPosition = _impactLabelBasePosition +
            Vector3.up * (.085f * Mathf.Clamp01(progress));
    }

    private void ResetRound()
    {
        // A retry or mode switch must never leave a cinematic
        // camera on top of the main view or mouse-look locked.
        if (_arrowCam != null) _arrowCam.Stop();
        _restoreAfterArrowCam = false;
        _modernRaisedPose = 0f;
        _modernPrecisionPose = 0f;
        _modernDrawPower = 0f;
        _modernBreathingWeight = 1f;
        _modernWobbleBlend = 0f;
        _modernLastCalibratedAimRayValid = false;
        _modernSightZeroed = false;
        _modernReleaseFeedbackAt = -1f;
        _modernDrawCuePlayed = false;
        if (_modernPhysicalSight != null)
            _modernPhysicalSight.ResetCalibration();
        HideModernHands();
        RestoreModernCameraFov();
        // Retry restores current mode's mouse scheme. Mode changes
        // already clear Modern input before switching gameplay.
        SetModernDesktopMouseControl(UsesSharedDesktopBow());
        for (int i = 0; i < _arrows.Length; i++) _arrows[i].Cancel();
        _shots = _hits = _score = _poolCursor = 0;
        _stage = 0;
        _stageIntroStartedAt = -1f;
        _stageIntroUntil = -1f;
        _bullseyes = 0;
        _moloHits = _janggaHits = _awakHits = 0;
        _traditionalImpactAt = -1f;
        _traditionalImpactZone = JemparinganTarget.TraditionalZone.None;
        _previousBest = PlayerPrefs.GetInt(BestKey(), 0);
        _best = _previousBest;
        ClearDrawState();
        _replayArmed = _awaitStringRelease = false;
        _replayHoldStartedAt = -1f;
        _stageBannerUntil = 0f;
        if (_stageBanner != null) _stageBanner.text = "";
        if (_introPanel != null) _introPanel.SetActive(true);
        if (_resultsPanel != null) _resultsPanel.SetActive(false);
        _roundOver = _finished = false;
        _ready = true;

        _target.SetSway(0f, 0f, true);
        if (IsModernMode() && _modernTargets != null)
            foreach (JemparinganModernTarget target in _modernTargets)
                if (target != null)
                {
                    target.SetLateralMotion(false);
                    target.ResetReaction();
                }
        _target.ResetReaction();
        UpdateNockedArrow(0f, true);
        HidePreview();
        if (_impactLabel != null) _impactLabel.text = "";
        _impactLabelUntil = 0f;
        ShowFeedback(IsModernMode() ? "LEVEL 1" : "TRADITIONAL  •  FOCUS");
        UpdateIntroMode();
        UpdateHUD();
        // Apply after ResetRound's intro/result/pip state changes.
        // When Modern is active no old 3D board may obstruct the sight.
        SyncWorldSpaceHudVisibility();
    }

    private void UpdateIntroMode()
    {
        if (_titleLabel != null && useOutdoorEnvironment)
            _titleLabel.text = IsModernMode()
                ? (UsesDesktopInput() ? "" : "MODERN ARCHERY")
                : "JEMPARINGAN  |  TRADITIONAL";
        if (_introGoalLabel == null || _introTitleLabel == null) return;
        if (IsModernMode())
        {
            _introTitleLabel.text = "MODERN ARCHERY";
            _introGoalLabel.text = "";
        }
        else
        {
            _introTitleLabel.text = "JEMPARINGAN  /  TRADITIONAL";
            _introGoalLabel.text = "12 ARROWS  |  3 LEVELS  |  MOLO 3 / JANGGA 2 / AWAK 1";
        }
    }

    private void UpdateHUD()
    {
        if (IsModernMode())
        {
            if (_scoreLabel != null)
                _scoreLabel.text = UsesDesktopInput() ? "" :
                    "SCORE " + _score + "  |  ARROWS " +
                    Mathf.Max(0, arrowsPerRound - _shots);
            if (_phaseLabel != null && !_finished)
                _phaseLabel.text = UsesDesktopInput() ? "" :
                    (modernProgressiveStages
                        ? "LEVEL " + (_stage + 1) + "/3" : "MODERN");
        }
        else
        {
            if (_scoreLabel != null)
                _scoreLabel.text = "SCORE " + _score + "  |  HITS " + _hits +
                    "  |  ARROWS " + _shots + "/" + arrowsPerRound;
            if (_phaseLabel != null && !_finished)
                _phaseLabel.text = "TRADITIONAL  •  LEVEL " + (_stage + 1) + "/3";
        }
        UpdateShotPips();
    }

    private void UpdateShotPips()
    {
        if (_shotPipRenderers == null) return;
        for (int i = 0; i < _shotPipRenderers.Length; i++)
        {
            Renderer pip = _shotPipRenderers[i];
            bool withinMode = i < arrowsPerRound;
            if (pip.gameObject.activeSelf != withinMode)
                pip.gameObject.SetActive(withinMode);
            if (!withinMode) continue;
            float x = arrowsPerRound <= 1 ? 0f :
                Mathf.Lerp(-.46f, .46f, i / (float)(arrowsPerRound - 1));
            pip.transform.localPosition = new Vector3(x, 0f, 0f);
            JemparinganArt.Tone tone = i < _shots ?
                JemparinganArt.Tone.Gold : JemparinganArt.Tone.Paper;
            if (!_finished && i >= _shots) tone = JemparinganArt.Tone.Jade;
            pip.sharedMaterial = JemparinganArt.Material(tone);
        }
    }
    private void SetInfo(string value)
    {
        if (UsesSharedDesktopBow())
        {
            // No world-space instructions obstruct the bow or target.
            // Do not keep ring explanations across the aiming view.
            value = "";
        }
        else if (useOutdoorEnvironment)
        {
            // Keep first-person VR hints short enough to fit the readable lower panel.
            if (value.StartsWith("READY") || value.StartsWith("Hold left mouse"))
                value = IsModernMode()
                    ? "RINGS: GOLD 10  RED 8  BLUE 6  BLACK 4  WHITE 2"
                    : "RED 3  |  YELLOW 2  |  WHITE 1";
            else if (value.StartsWith("Hand tracking"))
                value = "HAND TRACKING LOST";
            else if (value.StartsWith("Hold the bow"))
                value = "HOLD BOW WITH LEFT HAND";
        }
        if (_infoLabel != null && _lastInfo != value) { _lastInfo = value; _infoLabel.text = value; }
    }
    private void ShowFeedback(string msg, Color? feedbackColor = null)
    {
        if (_feedbackLabel == null) return;
        if (UsesSharedDesktopBow())
        {
            // The compact HUD shows results without duplicate world text.
            // Avoid duplicate ARROW RELEASED, HIT, MISS and control notices.
            _feedbackLabel.text = "";
            _feedbackUntil = 0f;
            return;
        }
        _feedbackLabel.color = feedbackColor ?? new Color(.40f, .94f, .75f);
        _feedbackLabel.text = msg;
        _feedbackUntil = Time.time + 1.5f;
    }

    private void BuildNockedArrow()
    {
        if (!useOutdoorEnvironment) return;

        var root = new GameObject("Nocked arrow | cosmetic preview only").transform;
        root.SetParent(_bow, false);
        _nockedArrow = root;

        // Nock at local Z=0, together with the bowstring bead. The shaft
        // extends forward along +Z; the whole arrow follows the drawn string.
        // Render only, with no projectile script or collision detection.
        JemparinganArt.Part("Bamboo arrow shaft", PrimitiveType.Cylinder, root,
            new Vector3(0f, 0f, .42f), new Vector3(.011f, .42f, .011f),
            JemparinganArt.Tone.BowEdge, Quaternion.Euler(90f, 0f, 0f));
        JemparinganArt.Part("Visible shaft highlight", PrimitiveType.Cube, root,
            new Vector3(0f, .009f, .43f), new Vector3(.004f, .003f, .69f),
            JemparinganArt.Tone.BowIvory);
        JemparinganArt.Part("Metal arrow tip", PrimitiveType.Cylinder, root,
            new Vector3(0f, 0f, .88f), new Vector3(.020f, .058f, .020f),
            JemparinganArt.Tone.BowBrass, Quaternion.Euler(90f, 0f, 0f));
        JemparinganArt.Part("Arrow nock", PrimitiveType.Sphere, root,
            Vector3.zero, new Vector3(.016f, .016f, .025f),
            JemparinganArt.Tone.BowIvory);
        foreach (float x in new[] {-.026f, .026f})
        {
            JemparinganArt.Part("Red fletching", PrimitiveType.Cube, root,
                new Vector3(x, 0f, .10f), new Vector3(.037f, .0035f, .135f),
                JemparinganArt.Tone.Red, Quaternion.Euler(0f, x < 0f ? -16f : 16f, 0f));
        }
        UpdateNockedArrow(0f, true);
    }

    private void UpdateNockedArrow(float pull, bool visible)
    {
        if (_nockedArrow == null) return;
        _nockedArrow.localPosition = TraditionalNockLocalPosition(pull);
        if (_nockedArrow.gameObject.activeSelf != visible)
            _nockedArrow.gameObject.SetActive(visible);
    }

    // Cosmetic brace height only. Input draw, launch origin, power and arrow
    // flight continue to use the original pose/distance, in both modes.
    private float TraditionalVisualPull(float pull)
    {
        return UsesTraditionalPresentation()
            ? Mathf.Lerp(.12f, maximumDraw, Mathf.InverseLerp(.04f, maximumDraw, pull))
            : pull;
    }

    // One bow-local cosmetic nock drives the string bead, both string halves
    // and the attached arrow. It deliberately stays independent from launch
    // origin, aim direction, draw power and projectile physics.
    private Vector3 TraditionalNockLocalPosition(float pull)
    {
        return Vector3.back * TraditionalVisualPull(pull);
    }

    private void UpdateBowStrings(float pull)
    {
        // Outdoor Bow Polish v1 uses the exact mesh endpoints. The original
        // v0.9 bow and string endpoints remain unchanged in legacy scenes.
        Vector3 upperTip = useOutdoorEnvironment
            ? JemparinganBowVisual.UpperTip : Vector3.up * 0.53f;
        Vector3 lowerTip = useOutdoorEnvironment
            ? JemparinganBowVisual.LowerTip : Vector3.down * 0.53f;
        Vector3 nock = TraditionalNockLocalPosition(pull);
        PositionString(_upperString, upperTip, nock);
        PositionString(_lowerString, lowerTip, nock);
    }
    private void PositionString(Transform piece, Vector3 a, Vector3 b)
    {
        Vector3 delta = b - a;
        piece.localPosition = (a + b) * 0.5f;
        piece.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        float stringRadius = useOutdoorEnvironment ? .0040f : .0025f;
        piece.localScale = new Vector3(stringRadius, delta.magnitude * 0.5f, stringRadius);
    }

    private void ShowPreview(Vector3 origin, Vector3 rawAim, float power)
    {
        if (!trajectoryPreview) return;
        float speed = Mathf.Lerp(minimumArrowSpeed, maximumArrowSpeed, power);
        Vector3 velocity = GetAssistedAim(origin, rawAim, speed) * speed;
        for (int i = 0; i < _previewDots.Length; i++)
        {
            Transform previewTarget = IsModernMode() && _modernTargets != null
                ? _modernTargets[1].transform : (_target != null ? _target.transform : null);
            float previewDuration = useOutdoorEnvironment && previewTarget != null
                ? Mathf.Clamp(Vector3.Distance(origin, previewTarget.position) /
                    Mathf.Max(1f, speed), 0.52f, 2.5f)
                : 0.52f;
            float t = (i + 1) * (previewDuration / _previewDots.Length);
            _previewDots[i].position = JemparinganTrajectory.Position(origin, velocity, simulatedGravity, t);
            _previewDots[i].gameObject.SetActive(true);
        }
    }
    private void HidePreview()
    {
        if (_previewDots == null) return;
        for (int i = 0; i < _previewDots.Length; i++) _previewDots[i].gameObject.SetActive(false);
    }

    private bool BuildScene()
    {
        Camera camera = Camera.main;
        if (camera == null) { Debug.LogError("Jemparingan: MainCamera missing."); return false; }
        _playerCamera = camera;
        _playerBaseFov = camera.fieldOfView;
        // Allocate enough pooled projectiles for either Traditional or Modern.
        if (useOutdoorEnvironment)
            arrowsPerRound = Mathf.Max(12, modernArrowCount);
        Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
        Quaternion facing = Quaternion.LookRotation(forward);
        if (useOutdoorEnvironment && outdoorTargetAnchor == null)
        {
            Debug.LogError("Jemparingan outdoor preview: target anchor is missing.");
            return false;
        }
        // In Playable v05, the authored gawang and gameplay anchor are siblings
        // under "Preview Bandulan | movable target". The generated targets
        // live elsewhere, so hiding this group is safe for Modern gameplay.
        if (useOutdoorEnvironment && outdoorTargetAnchor.parent != null &&
            outdoorTargetAnchor.parent.name == "Preview Bandulan | movable target")
        {
            _sceneBandulanFrame = outdoorTargetAnchor.parent.gameObject;
            _sceneBandulanFrameInitiallyActive = _sceneBandulanFrame.activeSelf;
        }

        Vector3 stagePosition = camera.transform.position + forward * 2.65f - Vector3.up * 0.05f;
        Vector3 targetPosition = useOutdoorEnvironment ? outdoorTargetAnchor.position : stagePosition;

        var root = new GameObject(useOutdoorEnvironment
            ? "JEMPARINGAN | Outdoor target gameplay"
            : "JEMPARINGAN | Panggung Pendopo Stylized").transform;
        root.SetPositionAndRotation(targetPosition, facing);
        if (!useOutdoorEnvironment)
        {
            // Legacy desktop gameplay remains exactly as before.
            BuildPendopo(root);
            BuildBackdrop(root);
        }
        BuildBandulan(root);
        if (useOutdoorEnvironment) BuildModernTargets(root);
        if (!useOutdoorEnvironment)
            BuildTargetPresentation(root);
        _impactLabel = JemparinganArt.Label("Floating impact points", root,
            _impactLabelBasePosition, "", .0041f, new Color(.98f, .81f, .40f));
        BuildBow();
        BuildTraditionalPosture(camera);
        BuildPool();
        BuildPreview();
        BuildDesktopPracticeSight(camera);

        var hud = new GameObject("Jemparingan HUD").transform;
        _worldSpaceHudRoot = hud.gameObject;
        if (useOutdoorEnvironment)
        {
            // Place non-Modern HUD text in the upper-right of the viewpoint.
            // No opaque boards; Modern desktop uses its small IMGUI instead.
            hud.SetParent(camera.transform, false);
            hud.localPosition = new Vector3(1.25f, 0.10f, 2.35f);
            hud.localRotation = Quaternion.Euler(0f, 25f, 0f);
        }
        else
            hud.SetPositionAndRotation(stagePosition, facing);
        _titleLabel = JemparinganArt.Label("Title", hud, new Vector3(0f, 1.16f, 0f), "J E M P A R I N G A N   •   R A S A", .0052f, new Color(0.96f, .79f, .48f));
        _scoreLabel = JemparinganArt.Label("Score", hud, new Vector3(0f, .96f, 0f), "", .0037f, Color.white);
        _phaseLabel = JemparinganArt.Label("Phase", hud, new Vector3(0f, .87f, 0f), "", .0033f, new Color(.93f, .90f, .70f));
        _feedbackLabel = JemparinganArt.Label("Feedback", hud, new Vector3(0f, .70f, 0f), "", .0042f, new Color(.40f, .94f, .75f));
        BuildShotPips(hud);
        BuildSessionPanels(hud);
        var help = new GameObject("Jemparingan Instructions").transform;
        _worldSpaceHelpRoot = help.gameObject;
        if (useOutdoorEnvironment)
        {
            // Instructions sit below the right-hand HUD and session cards,
            // not across the bow or in front of the target.
            help.SetParent(camera.transform, false);
            help.localPosition = new Vector3(1.25f, -0.60f, 2.35f);
            help.localRotation = Quaternion.Euler(0f, 25f, 0f);
        }
        else
            help.SetPositionAndRotation(camera.transform.position + forward * 1.05f - Vector3.up * .52f, facing);
        _infoLabel = JemparinganArt.Label("Help", help, Vector3.zero, "", .0026f, new Color(.95f, .89f, .76f));
        if (useOutdoorEnvironment)
            StyleOutdoorHud(hud, help);

        _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 0f;
        _audio.playOnAwake = false;
        _shotSound = GenerateTone("Archery String", 0.17f, 185f, true);
        if (useOutdoorEnvironment)
        {
            _modernDrawTensionSound = GenerateModernStringSound(
                "Modern | draw tension", false);
            _modernReleaseSound = GenerateModernStringSound(
                "Modern | short string snap", true);
        }
        _hitSound = GenerateWoodImpact("Bandulan Wood Hit", false);
        _bullseyeSound = GenerateWoodImpact("Bandulan Center Hit", true);
        _missSound = GenerateTone("Soft Miss", 0.14f, 115f, false);
        _focusSound = GenerateTone("RASA Ready", 0.13f, 710f, false);
        _stageSound = GenerateTone("Stage Change", 0.25f, 395f, false);

        _ambientAudio = gameObject.AddComponent<AudioSource>();
        _ambientAudio.spatialBlend = 0f;
        _ambientAudio.playOnAwake = false;
        _ambientAudio.loop = true;
        _ambientAudio.volume = ambientWindVolume;
        if (ambientWindVolume > 0f)
        {
            _ambientAudio.clip = GenerateWindAmbient("Soft Pendopo Wind", 4f);
            _ambientAudio.Play();
        }
        _best = PlayerPrefs.GetInt("JemparinganRasaBest", 0);
        ResetRound();
        SetInfo("Hold left mouse to draw, release to shoot. Hold right mouse to aim.");
        return true;
    }

    // Traditional and non-desktop HUD text; no opaque background meshes.
    private void StyleOutdoorHud(Transform hud, Transform help)
    {
        _titleLabel.text = "JEMPARINGAN  |  RASA";
        _titleLabel.characterSize = .0090f;
        _titleLabel.fontSize = 96;
        _titleLabel.fontStyle = FontStyle.Bold;
        _scoreLabel.characterSize = .0074f;
        _scoreLabel.fontSize = 96;
        _scoreLabel.fontStyle = FontStyle.Bold;
        _phaseLabel.characterSize = .0071f;
        _phaseLabel.fontSize = 96;
        _feedbackLabel.characterSize = .0072f;
        _feedbackLabel.fontSize = 96;
        // Keep feedback aligned with the other copy in the right-hand HUD.
        _feedbackLabel.transform.localPosition = new Vector3(0f, .67f, -.035f);

        _infoLabel.characterSize = .0066f;
        _infoLabel.fontSize = 96;
        _infoLabel.fontStyle = FontStyle.Bold;

        foreach (TextMesh label in hud.GetComponentsInChildren<TextMesh>(true))
        {
            if (label == _titleLabel || label == _scoreLabel ||
                label == _phaseLabel || label == _feedbackLabel) continue;
            label.characterSize *= 1.35f;
            label.fontSize = 96;
            if (label.name == "Intro title")
                label.text = "JEMPARINGAN  /  RASA";
            else if (label.name == "Intro goal")
                label.text = "12 ARROWS  |  MOLO 3  JANGGA 2  AWAK 1";
            else if (label.name == "Intro start" && UsesDesktopInput())
                label.text = "HOLD LEFT TO START";
        }
    }

    // Non-Modern and non-desktop UI stays as world-space TextMesh.
    // These groups carry labels only, never opaque background boards.
    private void BuildSessionPanels(Transform hud)
    {
        var intro = new GameObject("Session Intro | Auto Hides On Draw");
        intro.transform.SetParent(hud, false);
        intro.transform.localPosition = new Vector3(0f, .09f, -.27f);
        _introPanel = intro;
        _introTitleLabel = JemparinganArt.Label("Intro title", intro.transform, new Vector3(0f, .16f, -.025f),
            "J E M P A R I N G A N   /   R A S A", .0045f, new Color(.98f, .82f, .51f));
        _introGoalLabel = JemparinganArt.Label("Intro goal", intro.transform, new Vector3(0f, .015f, -.025f),
            "12 ARROWS  |  MOLO 3  JANGGA 2  AWAK 1", .0035f, Color.white);
        JemparinganArt.Label("Intro start", intro.transform, new Vector3(0f, -.14f, -.025f),
            UsesDesktopInput() ? "HOLD LEFT MOUSE TO START" : "PULL STRING TO START",
            .0037f, new Color(.56f, .94f, .76f));

        var results = new GameObject("Round Results | Hidden Until Finish");
        results.transform.SetParent(hud, false);
        results.transform.localPosition = new Vector3(0f, .09f, -.30f);
        _resultsPanel = results;
        _resultsTitle = JemparinganArt.Label("Results heading", results.transform,
            new Vector3(0f, .28f, -.026f), "", .0050f, new Color(.98f, .82f, .51f));
        _resultsDetails = JemparinganArt.Label("Results score and accuracy", results.transform,
            new Vector3(0f, .025f, -.026f), "", .0040f, Color.white);
        _resultsHint = JemparinganArt.Label("Results replay action", results.transform,
            new Vector3(0f, -.26f, -.026f), "", .0034f, new Color(.56f, .94f, .76f));
        results.SetActive(false);

        _stageBanner = JemparinganArt.Label("Stage transition", hud,
            new Vector3(0f, .38f, -.14f), "", .0042f, new Color(.98f, .82f, .51f));
    }

    private void BuildShotPips(Transform hud)
    {
        _shotPipRenderers = new Renderer[arrowsPerRound];
        var parent = new GameObject(arrowsPerRound + " Shot Progress").transform;
        parent.SetParent(hud, false);
        parent.localPosition = new Vector3(0f, .56f, 0f);
        const float width = .92f;
        for (int i = 0; i < arrowsPerRound; i++)
        {
            float x = arrowsPerRound <= 1 ? 0f : Mathf.Lerp(-width * .5f, width * .5f, i / (float)(arrowsPerRound - 1));
            var obj = JemparinganArt.Part("Shot " + (i + 1), PrimitiveType.Cube, parent,
                new Vector3(x, 0f, 0f), new Vector3(.055f, .018f, .018f), JemparinganArt.Tone.Paper);
            _shotPipRenderers[i] = obj.GetComponent<Renderer>();
        }
    }

    private void BuildPendopo(Transform root)
    {
        usingTone(root, new Vector3(0, -.89f, 0), new Vector3(1.8f, .10f, .85f), JemparinganArt.Tone.DarkWood, "Base");
        usingTone(root, new Vector3(0, -.83f, 0), new Vector3(1.65f, .025f, .72f), JemparinganArt.Tone.Wood, "Inset flooring");
        for (int i = -2; i <= 2; i++)
        {
            usingTone(root, new Vector3(i * .29f, -.811f, 0), new Vector3(.015f, .007f, .72f), JemparinganArt.Tone.LightWood, "Board seam");
        }
        for (int s = -1; s <= 1; s += 2)
        {
            float x = s * .62f;
            usingTone(root, new Vector3(x, -.05f, .10f), new Vector3(.07f, 1.55f, .075f), JemparinganArt.Tone.DarkWood, "Saka guru");
            usingTone(root, new Vector3(x, -.78f, .10f), new Vector3(.15f, .16f, .15f), JemparinganArt.Tone.Brass, "Post footing");
            usingTone(root, new Vector3(x, .60f, .10f), new Vector3(.13f, .12f, .12f), JemparinganArt.Tone.Gold, "Post collar");
            for (int side = -1; side <= 1; side += 2)
            {
                JemparinganArt.Beam("Roof brace", root,
                    new Vector3(x, .70f, .10f), new Vector3(x - s * .11f, .86f, .10f + side * .10f), .018f, JemparinganArt.Tone.Gold);
            }
        }
        usingTone(root, new Vector3(0, .77f, .10f), new Vector3(1.58f, .10f, .22f), JemparinganArt.Tone.Wood, "Pendopo lintel");
        usingTone(root, new Vector3(0, .85f, .10f), new Vector3(1.62f, .06f, .26f), JemparinganArt.Tone.DarkWood, "Roof tier");
        JemparinganArt.Beam("Left canopy ridge", root, new Vector3(-.82f, .88f, .10f), new Vector3(0, 1.12f, .10f), .025f, JemparinganArt.Tone.Brass);
        JemparinganArt.Beam("Right canopy ridge", root, new Vector3(.82f, .88f, .10f), new Vector3(0, 1.12f, .10f), .025f, JemparinganArt.Tone.Brass);
        for (int s = -1; s <= 1; s += 2)
        {
            JemparinganArt.Part("Jasmine tassel", PrimitiveType.Sphere, root,
                new Vector3(s * .73f, .68f, -.08f), new Vector3(.05f, .05f, .05f), JemparinganArt.Tone.Gold);
        }
        JemparinganArt.Part("Golden crest", PrimitiveType.Sphere, root,
            new Vector3(0, 1.12f, -.06f), new Vector3(.12f, .05f, .035f), JemparinganArt.Tone.Gold);
        usingTone(root, new Vector3(0, -.73f, -.13f), new Vector3(.76f, .035f, .30f), JemparinganArt.Tone.Indigo, "Batik runner");
        for (int i = -3; i <= 3; i++)
        {
            float x = i * .095f;
            JemparinganArt.Part("Runner motif", PrimitiveType.Cube, root,
                new Vector3(x, -.70f, -.13f), new Vector3(.037f, .005f, .037f), JemparinganArt.Tone.Gold,
                Quaternion.Euler(0, 45, 0));
        }
    }
    private static void usingTone(Transform parent, Vector3 pos, Vector3 scale, JemparinganArt.Tone tone, string name)
        => JemparinganArt.Part(name, PrimitiveType.Cube, parent, pos, scale, tone);

    // Minimal backdrop and lanterns: low-poly silhouettes, shared materials.
    private void BuildBackdrop(Transform root)
    {
        usingTone(root, new Vector3(0f, -.04f, .42f), new Vector3(1.55f, 1.05f, .03f), JemparinganArt.Tone.Indigo, "Indigo backdrop");
        usingTone(root, new Vector3(0f, .48f, .41f), new Vector3(1.61f, .07f, .04f), JemparinganArt.Tone.Gold, "Backdrop lintel");
        usingTone(root, new Vector3(-.79f, -.08f, .40f), new Vector3(.17f, .90f, .025f), JemparinganArt.Tone.Red, "Left curtain");
        usingTone(root, new Vector3(.79f, -.08f, .40f), new Vector3(.17f, .90f, .025f), JemparinganArt.Tone.Red, "Right curtain");
        for (int side = -1; side <= 1; side += 2)
        {
            JemparinganArt.Part("Curtain ornament", PrimitiveType.Sphere, root,
                new Vector3(side * .79f, .26f, .38f), Vector3.one * .055f, JemparinganArt.Tone.Gold);
            JemparinganArt.Beam("Lamp chain", root,
                new Vector3(side * .55f, .80f, -.03f), new Vector3(side * .55f, .55f, -.03f),
                .004f, JemparinganArt.Tone.Paper);
            JemparinganArt.Part("Lantern", PrimitiveType.Sphere, root,
                new Vector3(side * .55f, .48f, -.03f), new Vector3(.09f, .11f, .09f), JemparinganArt.Tone.Gold);
        }
    }

    // Target remains in front of a simple plaque for contrast.
    private void BuildModernTargets(Transform root)
    {
        _modernTargetsRoot = new GameObject("Modern Range | Three round targets").transform;
        _modernTargetsRoot.SetParent(root, false);
        _modernTargets = new JemparinganModernTarget[3];
        // A shallow arc gives the three targets distinct positions and distances
        // without introducing a new scene, arena, dependencies, or prefabs.
        Vector3[] positions =
        {
            new Vector3(-1.70f, .10f, -.80f),
            new Vector3(0f, .22f, .70f),
            new Vector3(1.70f, .10f, -.80f)
        };
        for (int i = 0; i < _modernTargets.Length; i++)
        {
            var go = new GameObject("Modern circular target " + (i + 1));
            go.transform.SetParent(_modernTargetsRoot, false);
            go.transform.localPosition = positions[i];
            _modernTargets[i] = go.AddComponent<JemparinganModernTarget>();
            _modernTargets[i].Build(i + 1);
        }
        _modernTargetsRoot.gameObject.SetActive(false);
    }

    private void BuildTargetPresentation(Transform root)
    {
        JemparinganArt.Beam("Left support", root,
            new Vector3(-.42f, -.55f, .01f), new Vector3(-.18f, .66f, .01f), .012f, JemparinganArt.Tone.Brass);
        JemparinganArt.Beam("Right support", root,
            new Vector3(.42f, -.55f, .01f), new Vector3(.18f, .66f, .01f), .012f, JemparinganArt.Tone.Brass);
        JemparinganArt.Beam("Support top", root,
            new Vector3(-.20f, .66f, .01f), new Vector3(.20f, .66f, .01f), .012f, JemparinganArt.Tone.Gold);
        JemparinganArt.Part("Plaque outer", PrimitiveType.Cylinder, root,
            new Vector3(0f, -.12f, .085f), new Vector3(.28f, .012f, .28f),
            JemparinganArt.Tone.Gold, Quaternion.Euler(90f, 0f, 0f));
        JemparinganArt.Part("Plaque inner", PrimitiveType.Cylinder, root,
            new Vector3(0f, -.12f, .070f), new Vector3(.22f, .012f, .22f),
            JemparinganArt.Tone.Indigo, Quaternion.Euler(90f, 0f, 0f));
    }



    private void BuildBandulan(Transform root)
    {
        JemparinganArt.Beam("Suspension cord", root, new Vector3(0, .69f, 0), new Vector3(0, .12f, 0), .005f, JemparinganArt.Tone.Paper);
        _bandulanSuspension = root.Find("Suspension cord");
        var targetGO = new GameObject("Bandulan | Target");
        targetGO.transform.SetParent(root, false);
        targetGO.transform.localPosition = new Vector3(0f, -.13f, 0f);
        _target = targetGO.AddComponent<JemparinganTarget>();
        var traditional = new GameObject("Traditional bandulan visuals");
        traditional.transform.SetParent(targetGO.transform, false);
        _traditionalBandulanVisual = traditional;
        float bodyBottom = JemparinganTarget.TraditionalBottom;
        float neckBottom = JemparinganTarget.TraditionalNeckBottom;
        float headBottom = JemparinganTarget.TraditionalHeadBottom;
        float headTop = JemparinganTarget.TraditionalTop;
        var awak = JemparinganArt.Part("Awak white body | 1 point",
            PrimitiveType.Cylinder, traditional.transform,
            new Vector3(0f, (bodyBottom + neckBottom) * .5f, 0f),
            new Vector3(.048f, (neckBottom - bodyBottom) * .5f, .048f),
            JemparinganArt.Tone.BandulanWhite);
        _traditionalBandulanBody = awak.GetComponent<Renderer>();
        var jangga = JemparinganArt.Part("Jangga yellow neck | 2 points",
            PrimitiveType.Cylinder, traditional.transform,
            new Vector3(0f, (neckBottom + headBottom) * .5f, 0f),
            new Vector3(.049f, (headBottom - neckBottom) * .5f, .049f),
            JemparinganArt.Tone.BandulanYellow);
        var molo = JemparinganArt.Part("Molo red head | 3 points",
            PrimitiveType.Cylinder, traditional.transform,
            new Vector3(0f, (headBottom + headTop) * .5f, 0f),
            new Vector3(.049f, (headTop - headBottom) * .5f, .049f),
            JemparinganArt.Tone.Red);
        _target.SetTraditionalZoneRenderers(molo.GetComponent<Renderer>(),
            jangga.GetComponent<Renderer>(), _traditionalBandulanBody);
        _target.SetBody(_traditionalBandulanBody, JemparinganArt.Tone.BandulanWhite);
        // The existing suspension mesh follows the target's upper tip.
        // No new suspension object or runtime component is created.
        _target.AttachSuspension(_bandulanSuspension, new Vector3(0f, .69f, 0f));
        traditional.SetActive(true);
    }

    // The legs stay in world space at the seated shooting spot. They should
    // not spin with camera aim as a camera-attached 3D sticker would.
    private void BuildTraditionalPosture(Camera camera)
    {
        if (!useOutdoorEnvironment) return;
        _traditionalPosture = new GameObject(
            "Traditional | cross-legged seated pose").transform;
        Vector3 groundForward = Vector3.ProjectOnPlane(camera.transform.forward,
            Vector3.up).normalized;
        if (groundForward.sqrMagnitude < .01f) groundForward = Vector3.forward;
        _traditionalPosture.SetPositionAndRotation(camera.transform.position,
            Quaternion.LookRotation(groundForward, Vector3.up));

        JemparinganArt.Part("Folded sarong at hips", PrimitiveType.Sphere,
            _traditionalPosture, new Vector3(0f, -.96f, .06f),
            new Vector3(.43f, .19f, .38f), JemparinganArt.Tone.TraditionalCloth);
        Vector3 leftHip = new Vector3(-.13f, -.96f, .06f);
        Vector3 rightHip = new Vector3(.13f, -.96f, .06f);
        Vector3 leftKnee = new Vector3(-.40f, -1.04f, .42f);
        Vector3 rightKnee = new Vector3(.40f, -1.04f, .42f);
        Vector3 leftFoot = new Vector3(.15f, -1.09f, .28f);
        Vector3 rightFoot = new Vector3(-.15f, -1.14f, .36f);
        MakeTraditionalLimb("Left folded thigh", _traditionalPosture,
            leftHip, leftKnee, .24f, JemparinganArt.Tone.TraditionalCloth);
        MakeTraditionalLimb("Right folded thigh", _traditionalPosture,
            rightHip, rightKnee, .24f, JemparinganArt.Tone.TraditionalCloth);
        MakeTraditionalLimb("Left shin crosses right", _traditionalPosture,
            leftKnee, leftFoot, .15f, JemparinganArt.Tone.TraditionalCloth);
        MakeTraditionalLimb("Right shin crosses left", _traditionalPosture,
            rightKnee, rightFoot, .15f, JemparinganArt.Tone.TraditionalCloth);
        JemparinganArt.Part("Left knee cloth fold", PrimitiveType.Sphere,
            _traditionalPosture, leftKnee, new Vector3(.20f, .15f, .23f),
            JemparinganArt.Tone.TraditionalCloth);
        JemparinganArt.Part("Right knee cloth fold", PrimitiveType.Sphere,
            _traditionalPosture, rightKnee, new Vector3(.20f, .15f, .23f),
            JemparinganArt.Tone.TraditionalCloth);
        // A little textile trim makes the crossed folds readable without
        // introducing imported clothing meshes or texture dependencies.
        MakeTraditionalLimb("Left sarong edge", _traditionalPosture,
            leftKnee + new Vector3(.08f, .10f, -.09f),
            leftFoot + new Vector3(.08f, .10f, -.09f),
            .016f, JemparinganArt.Tone.TraditionalTrim);
        MakeTraditionalLimb("Right sarong edge", _traditionalPosture,
            rightKnee + new Vector3(-.08f, .10f, -.09f),
            rightFoot + new Vector3(-.08f, .10f, -.09f),
            .016f, JemparinganArt.Tone.TraditionalTrim);
        JemparinganArt.Part("Left bare foot", PrimitiveType.Sphere,
            _traditionalPosture, leftFoot,
            new Vector3(.13f, .055f, .20f), JemparinganArt.Tone.TraditionalSkin);
        JemparinganArt.Part("Right bare foot", PrimitiveType.Sphere,
            _traditionalPosture, rightFoot,
            new Vector3(.13f, .055f, .20f), JemparinganArt.Tone.TraditionalSkin);
        _traditionalPosture.gameObject.SetActive(false);
        // Cache only the seated body renderers. The first-person grip hand
        // added below stays visible in Modern Range with the body hidden.
        _seatedBodyRenderers = _traditionalPosture.GetComponentsInChildren<Renderer>(true);

        _traditionalArcher = _traditionalPosture.gameObject.AddComponent<JemparinganTraditionalArcher>();
        _traditionalArcher.Build(_bow.Find("Leather-wrapped main grip"), _bow);
    }

    private static Transform NewTraditionalLimb(string name, Transform parent,
        JemparinganArt.Tone tone)
    {
        return JemparinganArt.Part(name, PrimitiveType.Capsule, parent,
            Vector3.zero, Vector3.one * .1f, tone).transform;
    }

    private static void PlaceTraditionalLimb(Transform limb, Vector3 start,
        Vector3 end, float width)
    {
        Vector3 delta = end - start;
        if (delta.sqrMagnitude < .0001f) return;
        limb.localPosition = (start + end) * .5f;
        limb.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        // Unity Capsule has a local height of 2 units, radius 0.5.
        limb.localScale = new Vector3(width, delta.magnitude * .5f, width);
    }

    private static void MakeTraditionalLimb(string name, Transform parent,
        Vector3 start, Vector3 end, float width, JemparinganArt.Tone tone)
    {
        PlaceTraditionalLimb(NewTraditionalLimb(name, parent, tone),
            start, end, width);
    }

    private void UpdateTraditionalPresentation(bool stringDrawing)
    {
        if (!UsesTraditionalPresentation())
        {
            HideModernHands();
            return;
        }
        if (UsesSharedDesktopBow())
        {
            // Both desktop modes share the first-person hands without
            // rendering the legacy Traditional hand rig.
            // The right draw hand follows the existing nock, never the input.
            HideTraditionalArms();
            bool show = modernUseNewHands && UsesDesktopInput() &&
                modernViewmodelRigV2 && modernUseReferenceFraming &&
                _modeChosen && !_roundOver && !_finished &&
                (_arrowCam == null || !_arrowCam.IsActive);
            if (show && _modernHands != null)
                _modernHands.Present(_playerCamera, stringDrawing,
                    _modernDrawPower, _modernRaisedPose,
                    _stringMarker, modernShowRightDrawHand);
            else
                HideModernHands();
            return;
        }
        // Traditional retains its original left-grip rig.
        HideModernHands();
        if (_traditionalArcher != null)
            _traditionalArcher.Present();
    }

    private void HideTraditionalArms()
    {
        if (_traditionalArcher != null) _traditionalArcher.HideArms();
    }

    private void HideModernHands()
    {
        if (_modernHands != null) _modernHands.Hide();
    }

    private void HideModernPhysicalSight()
    {
        _modernLastCalibratedAimRayValid = false;
        _modernWobbleBlend = 0f;
        if (_modernPhysicalSight != null)
            _modernPhysicalSight.Present(null, false, false);
    }

    private void PresentModernPhysicalSight()
    {
        if (_modernPhysicalSight == null) return;
        bool visible = UsesSharedDesktopBow() &&
            modernViewmodelRigV2 &&
            modernUseReferenceFraming && modernUsePhysicalSight &&
            !_finished && !_roundOver &&
            (_arrowCam == null || !_arrowCam.IsActive);
        // Sight-in ONCE at fully settled pose/FOV. The ring then has a
        // constant bow-local transform; it never chases the camera frame.
        bool lensSettled = _playerCamera != null &&
            Mathf.Abs(_playerCamera.fieldOfView -
                Mathf.Min(modernReferenceAimFov, _playerBaseFov)) < .15f;
        bool calibrate = visible && lensSettled &&
            ModernPrecisionAimRequested() && _modernPrecisionPose >= .98f;
        _modernPhysicalSight.Present(_playerCamera, visible, calibrate);
    }

    private void BuildBow()
    {
        _bow = new GameObject("Busur Jemparingan Stylized").transform;

        if (useOutdoorEnvironment)
        {
            // New 1.30m tapered curved mesh plus wrapped grip and subdued
            // metallic details. All visuals only; no changes to shoot origin.
            JemparinganBowVisual.Build(_bow);
            _modernPhysicalSight =
                _bow.gameObject.AddComponent<JemparinganModernBowSight>();
            _modernPhysicalSight.Build();
        }
        else
        {
            // Preserve the known-working v0.9 primitive bow without changes.
            JemparinganArt.Beam("Upper bow arm", _bow, new Vector3(0, 0, 0), new Vector3(.06f, .28f, .03f), .019f, JemparinganArt.Tone.Wood);
            JemparinganArt.Beam("Upper bow tip", _bow, new Vector3(.06f, .28f, .03f), new Vector3(.02f, .53f, .10f), .014f, JemparinganArt.Tone.LightWood);
            JemparinganArt.Beam("Lower bow arm", _bow, new Vector3(0, 0, 0), new Vector3(.06f, -.28f, .03f), .019f, JemparinganArt.Tone.Wood);
            JemparinganArt.Beam("Lower bow tip", _bow, new Vector3(.06f, -.28f, .03f), new Vector3(.02f, -.53f, .10f), .014f, JemparinganArt.Tone.LightWood);
            JemparinganArt.Part("Grip", PrimitiveType.Cylinder, _bow, Vector3.zero,
                new Vector3(.036f, .09f, .036f), JemparinganArt.Tone.DarkWood);
            JemparinganArt.Part("Upper bow ornament", PrimitiveType.Sphere, _bow,
                new Vector3(.045f, .34f, .055f), Vector3.one * .025f, JemparinganArt.Tone.Gold);
            JemparinganArt.Part("Lower bow ornament", PrimitiveType.Sphere, _bow,
                new Vector3(.045f, -.34f, .055f), Vector3.one * .025f, JemparinganArt.Tone.Gold);
        }

        _stringMarker = JemparinganArt.Part("String pull bead", PrimitiveType.Sphere, _bow,
            Vector3.zero, Vector3.one * .026f,
            useOutdoorEnvironment ? JemparinganArt.Tone.BowBrass : JemparinganArt.Tone.Jade).transform;
        JemparinganArt.Tone stringTone = useOutdoorEnvironment
            ? JemparinganArt.Tone.BowIvory : JemparinganArt.Tone.Paper;
        _upperString = JemparinganArt.Part("Upper string", PrimitiveType.Cylinder, _bow,
            Vector3.zero, Vector3.one * .01f, stringTone).transform;
        _lowerString = JemparinganArt.Part("Lower string", PrimitiveType.Cylinder, _bow,
            Vector3.zero, Vector3.one * .01f, stringTone).transform;
        UpdateBowStrings(0f);
        BuildNockedArrow();
        BuildFocusIndicator();
        if (useOutdoorEnvironment)
        {
            _modernHands = _bow.gameObject.AddComponent<JemparinganModernHands>();
            _modernHands.Build();
        }
        _bow.gameObject.SetActive(false);
    }

    private void BuildFocusIndicator()
    {
        const int count = 6;
        var holder = new GameObject("RASA Focus | Beside Bow");
        _focusRoot = holder.transform;
        _focusRoot.SetParent(_bow, false);
        _focusRoot.localPosition = new Vector3(.15f, -.06f, -.07f);
        _focusRenderers = new Renderer[count];
        for (int i = 0; i < count; i++)
        {
            var pip = JemparinganArt.Part("Focus " + (i + 1), PrimitiveType.Cube,
                _focusRoot, new Vector3(0f, i * .038f, 0f),
                new Vector3(.023f, .024f, .012f), JemparinganArt.Tone.Paper);
            _focusRenderers[i] = pip.GetComponent<Renderer>();
        }
        _focusRoot.gameObject.SetActive(false);
    }

    private void BuildPool()
    {
        // The Inspector allows up to 30 shots; don't recycle live projectiles.
        int poolSize = JemparinganScoreRules.PoolCapacity(arrowsPerRound, ArrowPoolSize);
        _arrows = new JemparinganArrow[poolSize];
        var poolRoot = new GameObject("Arrow Pool (reused)").transform;
        for (int i = 0; i < poolSize; i++)
        {
            var obj = new GameObject("Arrow_" + (i + 1));
            obj.transform.SetParent(poolRoot, false);
            JemparinganArt.Part("Bamboo shaft", PrimitiveType.Cylinder, obj.transform,
                new Vector3(0, 0, .02f),
                new Vector3(useOutdoorEnvironment ? .012f : .008f, .28f,
                    useOutdoorEnvironment ? .012f : .008f),
                JemparinganArt.Tone.LightWood,
                Quaternion.Euler(90f, 0f, 0f));
            JemparinganArt.Part("Metal tip", PrimitiveType.Cylinder, obj.transform,
                new Vector3(0, 0, .35f),
                new Vector3(useOutdoorEnvironment ? .021f : .017f, .035f,
                    useOutdoorEnvironment ? .021f : .017f),
                JemparinganArt.Tone.Gold,
                Quaternion.Euler(90f, 0f, 0f));
            if (useOutdoorEnvironment)
            {
                // Short ivory highlight and fletching read clearly on a moving
                // arrow without adding a light, particle system or physics.
                JemparinganArt.Part("Flying shaft highlight", PrimitiveType.Cube,
                    obj.transform, new Vector3(0f, .012f, -.02f),
                    new Vector3(.004f, .003f, .36f),
                    JemparinganArt.Tone.BowIvory);
            }
            for (int f = -1; f <= 1; f += 2)
                JemparinganArt.Part("Feather", PrimitiveType.Cube, obj.transform,
                    new Vector3(f * .022f, 0, -.20f), new Vector3(.035f, .004f, .11f), JemparinganArt.Tone.Red,
                    Quaternion.Euler(0, f * 22f, 0));
            // One subtle trail per pooled arrow; never instantiated per shot.
            var trail = obj.AddComponent<TrailRenderer>();
            // Slightly longer, wider flight streak only for the distant
            // outdoor lane; the original short-range v0.9 remains unchanged.
            trail.time = useOutdoorEnvironment ? .23f : .14f;
            trail.minVertexDistance = .025f;
            trail.startWidth = useOutdoorEnvironment ? .017f : .009f;
            trail.endWidth = useOutdoorEnvironment ? .0025f : .001f;
            trail.sharedMaterial = JemparinganArt.Material(useOutdoorEnvironment
                ? JemparinganArt.Tone.BowIvory : JemparinganArt.Tone.Paper);
            trail.emitting = false;
            _arrows[i] = obj.AddComponent<JemparinganArrow>();
            obj.SetActive(false);
        }
    }





    private void BuildDesktopPracticeSight(Camera camera)
    {
        if (!useOutdoorEnvironment || !UsesDesktopInput()) return;

        // Four small strokes leave an open center; this is a sighting aid,
        // not a full opaque crosshair or a fake physical projectile.
        var root = new GameObject("Desktop practice reticle | toggle T").transform;
        root.SetParent(camera.transform, false);
        root.localPosition = new Vector3(0f, 0f, 1.85f);
        root.localRotation = Quaternion.identity;
        _desktopPracticeSight = root;
        _practiceSightRenderers = new Renderer[4];
        Vector3[] positions = {
            new Vector3(-.051f, 0f, 0f),
            new Vector3(.051f, 0f, 0f),
            new Vector3(0f, .051f, 0f),
            new Vector3(0f, -.051f, 0f)
        };
        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 size = i < 2 ? new Vector3(.027f, .003f, .003f) :
                new Vector3(.003f, .027f, .003f);
            var part = JemparinganArt.Part("Practice sight stroke " + i,
                PrimitiveType.Cube, root, positions[i], size,
                JemparinganArt.Tone.Paper);
            _practiceSightRenderers[i] = part.GetComponent<Renderer>();
        }
        _practiceSightLabel = JemparinganArt.Label("Practice aim signal",
            root, new Vector3(0f, -.118f, 0f), "",
            .0047f, new Color(.95f, .88f, .72f));
        _practiceSightLabel.fontStyle = FontStyle.Bold;
    }

    private void UpdateDesktopPracticeSight(Vector3 origin, Vector3 aim,
        float speed, bool drawing)
    {
        if (_desktopPracticeSight == null) return;
        bool show = IsDesktopTrainingAimActive() && !_roundOver && !_finished;
        if (!show)
        {
            HideDesktopPracticeSight();
            return;
        }
        if (!_desktopPracticeSight.gameObject.activeSelf)
            _desktopPracticeSight.gameObject.SetActive(true);

        Vector3 direction, predictedTarget;
        bool canReach = TryGetPracticeBallisticAim(origin, speed,
            out direction, out predictedTarget);
        bool aligned = canReach &&
            Vector3.Angle(aim, predictedTarget - origin) <= 2.8f;
        // Leave the target unobscured; only show "RELEASE" when drawing
        // and aligned. Avoid rewriting text/renderer materials every frame.
        if (!_practiceSightStateKnown || aligned != _practiceSightWasAligned)
        {
            Material material = JemparinganArt.Material(aligned
                ? JemparinganArt.Tone.Jade : JemparinganArt.Tone.Paper);
            for (int i = 0; i < _practiceSightRenderers.Length; i++)
                _practiceSightRenderers[i].sharedMaterial = material;
        }
        if (!_practiceSightStateKnown || aligned != _practiceSightWasAligned ||
            drawing != _practiceSightWasDrawing)
        {
            _practiceSightLabel.text = aligned && drawing ? "RELEASE" : "";
            _practiceSightLabel.color = aligned
                ? new Color(.40f, .94f, .75f)
                : new Color(.95f, .88f, .72f);
        }
        _practiceSightStateKnown = true;
        _practiceSightWasAligned = aligned;
        _practiceSightWasDrawing = drawing;
    }

    private void HideDesktopPracticeSight()
    {
        if (_desktopPracticeSight != null)
            _desktopPracticeSight.gameObject.SetActive(false);
        _practiceSightStateKnown = false;
    }

    private void BuildPreview()
    {
        _previewDots = new Transform[PreviewDotCount];
        var parent = new GameObject("Aim Path Preview").transform;
        for (int i = 0; i < PreviewDotCount; i++)
        {
            _previewDots[i] = JemparinganArt.Part("Guide " + i, PrimitiveType.Sphere, parent,
                Vector3.zero, Vector3.one * (.017f - i * .0009f), JemparinganArt.Tone.Jade).transform;
            _previewDots[i].gameObject.SetActive(false);
        }
    }

    // Short original one-shot bowstring sounds. Created once on scene
    // startup (22.05 kHz mono); playback does not allocate new clips.
    // Draw = quiet fibrous tension, release = snappy double-frequency twang.
    private static AudioClip GenerateModernStringSound(
        string name, bool release)
    {
        const int sampleRate = 22050;
        float duration = release ? .19f : .15f;
        int count = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[count];
        uint noiseState = release ? 81263u : 92507u;
        float baseHz = release ? 175f : 96f;
        for (int index = 0; index < count; index++)
        {
            float t = (float)index / sampleRate;
            noiseState = noiseState * 1664525u + 1013904223u;
            float noise = (noiseState >> 16) / 32768f - 1f;
            float decay = Mathf.Exp(-t * (release ? 26f : 15f));
            float pitch = baseHz * (1f + .23f * Mathf.Exp(-t * 40f));
            float stringBody = Mathf.Sin(2f * Mathf.PI * pitch * t) +
                .23f * Mathf.Sin(2f * Mathf.PI * pitch * 2.08f * t);
            float snap = noise * Mathf.Exp(-t * (release ? 95f : 35f));
            samples[index] = decay * (release ? .25f : .08f) *
                stringBody + snap * (release ? .19f : .035f);
        }
        AudioClip clip = AudioClip.Create(
            name, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Short original synthesized wooden hit: harmonic resonance + soft transient.
    // This is sound design, not a claim to be recorded traditional instrumentation.
    private static AudioClip GenerateWoodImpact(string name, bool center)
    {
        const int sampleRate = 22050;
        const float duration = .38f;
        int count = Mathf.CeilToInt(duration * sampleRate);
        var samples = new float[count];
        float baseHz = center ? 590f : 365f;
        uint random = center ? 12345u : 67891u;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float decay = Mathf.Exp(-18f * t);
            random = random * 1664525u + 1013904223u;
            float noise = ((random >> 16) / 32768f - 1f);
            float body = Mathf.Sin(2f * Mathf.PI * baseHz * t) +
                .24f * Mathf.Sin(2f * Mathf.PI * baseHz * 2.12f * t);
            samples[i] = .19f * decay * body +
                .10f * Mathf.Exp(-95f * t) * noise;
        }
        var clip = AudioClip.Create(name, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Seamless low-level wind texture made once, then looped without streaming or files.
    // No external music/assets, and no allocations in gameplay Update.
    private static AudioClip GenerateWindAmbient(string name, float seconds)
    {
        const int sampleRate = 22050;
        int count = Mathf.CeilToInt(seconds * sampleRate);
        var samples = new float[count];
        float smoothedNoise = 0f;
        uint random = 40621u;
        for (int i = 0; i < count; i++)
        {
            float phase = i / (float)count * Mathf.PI * 2f;
            random = random * 1664525u + 1013904223u;
            float noise = ((random >> 16) / 32768f - 1f);
            smoothedNoise = Mathf.Lerp(smoothedNoise, noise, .0025f);
            float loopEnvelope = Mathf.Sin(phase) * Mathf.Sin(phase);
            float breeze = .45f * Mathf.Sin(phase * 2f) +
                .3f * Mathf.Sin(phase * 5f + .8f);
            samples[i] = .12f * (breeze + smoothedNoise * loopEnvelope);
        }
        var clip = AudioClip.Create(name, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip GenerateTone(string name, float duration, float frequency, bool twang)
    {
        const int sampleRate = 22050;
        int length = Mathf.CeilToInt(duration * sampleRate);
        var samples = new float[length];
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Pow(1f - t / duration, twang ? 4f : 2.1f);
            float frequencyNow = twang ? frequency * (1f + .35f * Mathf.Exp(-35f * t)) : frequency;
            samples[i] = .25f * envelope * (Mathf.Sin(2f * Mathf.PI * frequencyNow * t)
                + .22f * Mathf.Sin(2f * Mathf.PI * frequencyNow * 2.1f * t));
        }
        AudioClip clip = AudioClip.Create(name, length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}

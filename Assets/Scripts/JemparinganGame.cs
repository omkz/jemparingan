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

    public enum GameMode { Classic, Traditional }
    [Header("Outdoor game mode (Classic v1 remains unchanged)")]
    [SerializeField] private GameMode initialOutdoorMode = GameMode.Classic;
    private GameMode _mode = GameMode.Classic;
    private bool _modeChosen;
    private GameObject _classicBandulanVisual, _traditionalBandulanVisual;
    private Renderer _classicBandulanBody, _traditionalBandulanBody;
    private Transform _traditionalPosture;
    private TextMesh _introGoalLabel, _introTitleLabel;

    private bool IsTraditionalMode() { return useOutdoorEnvironment && _mode == GameMode.Traditional; }
    private string BestKey() { return IsTraditionalMode() ? "JemparinganTraditionalBest" : "JemparinganRasaBest"; }

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
    private Transform _bow, _stringMarker, _focusRoot, _leadMarker;
    private Transform _desktopPracticeSight;
    private Renderer[] _practiceSightRenderers;
    private TextMesh _practiceSightLabel;
    private bool _desktopTrainingEnabled = true;
    // Cached sight state keeps TextMesh updates event-driven, not every frame.
    private bool _practiceSightStateKnown, _practiceSightWasAligned, _practiceSightWasDrawing;
    private GameObject _outdoorFeedbackPanel;
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
    private int _shots, _hits, _score, _combo, _best, _poolCursor;
    private int _previousBest, _bullseyes, _bestCombo, _rasaHits, _rasaBonus;
    private int _moloHits, _janggaHits, _awakHits;
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
            if (initialOutdoorMode == GameMode.Traditional)
                ChooseMode(GameMode.Traditional);
            if (UsesDesktopInput()) _modeChosen = false;
        }
    }

    private void OnGUI()
    {
        // Only the desktop/Web demo uses immediate-mode mouse buttons. The
        // Quest build keeps native gameplay and may start Traditional via
        // initialOutdoorMode in the Inspector.
        if (!_ready || !useOutdoorEnvironment || !UsesDesktopInput() || _modeChosen)
            return;

        float width = Mathf.Min(520f, Screen.width - 24f);
        float height = 260f;
        Rect panel = new Rect((Screen.width - width) * .5f,
            (Screen.height - height) * .5f, width, height);
        GUI.Box(panel, "");
        GUIStyle header = new GUIStyle(GUI.skin.label);
        header.fontSize = 21;
        header.fontStyle = FontStyle.Bold;
        header.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(panel.x + 15f, panel.y + 15f, width - 30f, 36f),
            "JEMPARINGAN: RASA", header);
        GUI.Label(new Rect(panel.x + 15f, panel.y + 58f, width - 30f, 24f),
            "CHOOSE YOUR MODE", GUI.skin.box);
        float buttonWidth = (width - 46f) * .5f;
        if (GUI.Button(new Rect(panel.x + 15f, panel.y + 102f, buttonWidth, 100f),
            "CLASSIC\n16 ARROWS  |  4 ROUNDS\nARCADE SCORING"))
            ChooseMode(GameMode.Classic);
        if (GUI.Button(new Rect(panel.x + 31f + buttonWidth, panel.y + 102f,
            buttonWidth, 100f),
            "TRADITIONAL\n12 ARROWS  |  3-2-1 SCORING\nMATARAM-INSPIRED"))
            ChooseMode(GameMode.Traditional);
        GUI.Label(new Rect(panel.x + 15f, panel.y + 216f, width - 30f, 24f),
            "Keys: 1 Classic   |   2 Traditional", GUI.skin.box);
    }

    private void ChooseMode(GameMode next)
    {
        if (!useOutdoorEnvironment || !_ready) return;
        _mode = next;
        _modeChosen = true;
        arrowsPerRound = IsTraditionalMode()
            ? 12 : JemparinganScoreRules.OutdoorArrowCount;
        _desktopTrainingEnabled = !IsTraditionalMode();
        _target.UseTraditionalScoring(IsTraditionalMode());
        if (_classicBandulanVisual != null)
            _classicBandulanVisual.SetActive(!IsTraditionalMode());
        if (_traditionalBandulanVisual != null)
            _traditionalBandulanVisual.SetActive(IsTraditionalMode());
        _target.SetBody(IsTraditionalMode() ? _traditionalBandulanBody :
            _classicBandulanBody, IsTraditionalMode() ?
            JemparinganArt.Tone.BandulanWhite : JemparinganArt.Tone.Red);
        if (_traditionalPosture != null)
            _traditionalPosture.gameObject.SetActive(IsTraditionalMode());
        _lastInfo = "";
        HideDesktopPracticeSight();
        ResetRound();
        // Mouse selection is a UI click, never a shot.
        _awaitStringRelease = UsesDesktopInput() && Input.GetMouseButton(0);
    }

    private void Update()
    {
        if (!_ready) return;
        if (useOutdoorEnvironment && UsesDesktopInput())
        {
            if (Input.GetKeyDown(KeyCode.M) && (_shots == 0 || _finished))
            {
                ResetRound();
                _modeChosen = false;
                return;
            }
            if (!_modeChosen)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) ChooseMode(GameMode.Classic);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) ChooseMode(GameMode.Traditional);
                return;
            }
        }
        if (UsesDesktopInput() && Input.GetKeyDown(KeyCode.R) && _arrows != null)
        {
            // Restart during a held string must not turn that release into a shot.
            bool stringWasHeld = _wasDrawing || Input.GetMouseButton(0);
            ResetRound();
            _awaitStringRelease = stringWasHeld;
        }
        if (useOutdoorEnvironment && UsesDesktopInput() &&
            Input.GetKeyDown(KeyCode.T))
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
            if (_outdoorFeedbackPanel != null)
                _outdoorFeedbackPanel.SetActive(false);
        }
        UpdateImpactLabel();
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
            if (_bow != null) _bow.gameObject.SetActive(false);
            HideDesktopPracticeSight();
            HidePreview();
            ClearDrawState();
            SetInfo("Hand tracking unavailable. Try again.");
            return;
        }
        if (_roundOver)
        {
            _bow.gameObject.SetActive(false);
            HideDesktopPracticeSight();
            HidePreview();
            return;
        }
        if (!pose.BowPinched)
        {
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
        _bow.SetPositionAndRotation(pose.BowPosition, Quaternion.LookRotation(aim, Vector3.up));
        bool drawing = pose.StringPinched;
        float distance = Mathf.Clamp(Vector3.Dot(pose.BowPosition - pose.StringPosition, aim), 0f, maximumDraw);
        float power = Mathf.InverseLerp(minimumDraw, maximumDraw, distance);
        UpdateDesktopPracticeSight(pose.BowPosition + aim * .25f,
            aim, Mathf.Lerp(minimumArrowSpeed, maximumArrowSpeed, power), drawing);

        if (drawing)
        {
            if (!_wasDrawing)
            {
                _drawStartedAt = Time.time;
                BeginFocus(aim, pose.BowPosition);
            }
            _lastDraw = distance;
            UpdateFocus(aim, pose.BowPosition, power);
            float pull = Mathf.Clamp(distance, 0f, maximumDraw);
            _stringMarker.localPosition = Vector3.back * pull;
            UpdateBowStrings(pull);
            UpdateNockedArrow(pull, true);
            ShowPreview(pose.BowPosition + aim * 0.25f, aim, power);
            UpdateLeadMarker(pose.BowPosition + aim * .25f, power);
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
            _stringMarker.localPosition = Vector3.zero;
            UpdateBowStrings(0f);
            UpdateNockedArrow(0f, true);
            HidePreview();
            SetInfo("READY  •  Hold left mouse to draw; release to shoot. Hold right mouse to aim.");
        }

        // One uninterrupted draw-to-release transition fires exactly once.
        // Tracking loss / bow-unpinch resets without firing.
        if (_wasBowHeld && _wasDrawing && !drawing)
        {
            if (_lastDraw >= minimumDraw && Time.time - _drawStartedAt >= minimumHoldSeconds)
            {
                // Hide the attached arrow for this release frame; the pooled
                // projectile is the only arrow visible in flight.
                UpdateNockedArrow(0f, false);
                Shoot(pose.BowPosition + aim * 0.25f, aim, _lastDraw, Time.time <= _focusReadyUntil);
            }
            else
                ShowFeedback("DRAW FURTHER");
            _lastDraw = 0f;
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
        bool visible = power >= .35f;
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
        arrow.Launch(origin, aim * speed, simulatedGravity, _target, OnArrowImpact, OnArrowMiss,
            useOutdoorEnvironment ? outdoorArrowLifetime : 1.6f);
        _shots++;
        _audio.PlayOneShot(_shotSound, 0.48f);
        ShowFeedback("ARROW RELEASED");
        if (!IsTraditionalMode() &&
            (_shots == 4 || _shots == 8 ||
            (useOutdoorEnvironment && _shots == 12))) UpdateLevel();
        if (_shots >= arrowsPerRound) _roundOver = true;
        UpdateHUD();
    }

    private bool IsDesktopTrainingAimActive()
    {
        // Desktop Editor training only. Quest input and classic v0.9 shooting
        // retain the original assistance behavior and physics.
        return useOutdoorEnvironment && UsesDesktopInput() &&
            _desktopTrainingEnabled;
    }

    // Only close-to-target desktop practice shots are corrected. Aim at the
    // visible bandulan (not above it); the training guide compensates drop.
    private Vector3 GetAssistedAim(Vector3 origin, Vector3 aim, float speed)
    {
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
        // Traditional defaults to a subtle assist, without magnetic
        // ballistic snapping unless training is explicitly enabled with T.
        float limitedAssist = IsTraditionalMode() ?
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


    private void OnArrowImpact(JemparinganArrow arrow, bool bullseye)
    {
        if (IsTraditionalMode())
        {
            // Classification comes from the very hit-test that triggered this
            // callback; an active shot cannot change modes mid-flight.
            JemparinganTarget.TraditionalZone zone = _target.LastTraditionalZone;
            int points = JemparinganScoreRules.TraditionalPointsForZone(zone);
            _hits++;
            if (zone == JemparinganTarget.TraditionalZone.Molo) _moloHits++;
            else if (zone == JemparinganTarget.TraditionalZone.Jangga) _janggaHits++;
            else if (zone == JemparinganTarget.TraditionalZone.Awak) _awakHits++;
            _score += points;
            if (_score > _best) _best = _score;
            _audio.PlayOneShot(zone == JemparinganTarget.TraditionalZone.Molo ?
                _bullseyeSound : _hitSound, .50f);
            string zoneName = zone == JemparinganTarget.TraditionalZone.Molo ? "MOLO" :
                zone == JemparinganTarget.TraditionalZone.Jangga ? "JANGGA" : "AWAK";
            ShowImpactLabel("+" + points + "  " + zoneName,
                zone == JemparinganTarget.TraditionalZone.Molo ?
                new Color(.98f, .81f, .40f) : new Color(.40f, .94f, .75f));
            ShowFeedback(zoneName + "  +" + points);
            UpdateHUD();
            return;
        }
        _hits++;
        _combo++;
        if (bullseye) _bullseyes++;
        if (_combo > _bestCombo) _bestCombo = _combo;
        if (arrow.StableRelease)
        {
            _rasaHits++;
            _rasaBonus += 3;
        }
        int pts = JemparinganScoreRules.PointsForHit(bullseye, arrow.StableRelease, _combo);
        _score += pts;
        if (_score > _best) _best = _score;
        _audio.PlayOneShot(bullseye ? _bullseyeSound : _hitSound, bullseye ? 0.54f : 0.45f);
        ShowImpactLabel("+" + pts + (bullseye ? "  CENTER" : "  HIT"),
            bullseye ? new Color(.98f, .81f, .40f) : new Color(.40f, .94f, .75f));
        ShowFeedback((bullseye ? "BULLSEYE" : "HIT!") + "  +" + pts + (arrow.StableRelease ? "  STEADY" : "") + (_combo >= 3 ? "  COMBO" : ""),
            bullseye ? new Color(.98f, .81f, .40f) : new Color(.40f, .94f, .75f));
        UpdateHUD();
    }

    private void OnArrowMiss(JemparinganArrow arrow)
    {
        _combo = 0;
        if (_missSound != null) _audio.PlayOneShot(_missSound, 0.16f);
        ShowFeedback("MISS  •  TRY AGAIN", new Color(.98f, .55f, .46f));
        UpdateHUD();
    }

    private void UpdateLevel()
    {
        int level = JemparinganScoreRules.StageIndex(_shots);
        _target.SetSway(JemparinganScoreRules.SwayAmountForStage(level),
            JemparinganScoreRules.SwaySpeedForStage(level));
        ShowFeedback(JemparinganScoreRules.StageTitle(_shots));
        // The outdoor phase row and status already communicate the new
        // stage: avoid a second banner duplicating the same information.
        if (_stageBanner != null && !useOutdoorEnvironment)
        {
            _stageBanner.text = JemparinganScoreRules.StageTitle(_shots);
            _stageBannerUntil = Time.time + 1.25f;
        }
        if (_audio != null && _stageSound != null) _audio.PlayOneShot(_stageSound, .23f);
        UpdateHUD();
    }

    private void FinishRound()
    {
        // Called when the final projectile is no longer in flight.
        _roundOver = false;
        _finished = true;
        bool newRecord = _score > _previousBest;
        if (newRecord)
        {
            PlayerPrefs.SetInt(BestKey(), _score);
            PlayerPrefs.Save();
        }
        _best = Mathf.Max(_previousBest, _score);
        _scoreLabel.text = useOutdoorEnvironment
            ? "COMPLETE  |  SCORE " + _score + "  |  " + _hits + " HITS"
            : "COMPLETE  •  " + _score + " POINTS  •  " + _hits + "/" + _shots + " HITS";
        if (_phaseLabel != null)
            _phaseLabel.text = "ROUND COMPLETE";
        if (_outdoorFeedbackPanel != null)
        {
            _outdoorFeedbackPanel.SetActive(false);
            _feedbackLabel.text = "";
            _feedbackUntil = 0f;
        }
        SetInfo(UsesDesktopInput() ? "PRESS R OR HOLD LEFT MOUSE TO RESTART" : "RELEASE STRING, THEN HOLD TO RESTART");
        _bow.gameObject.SetActive(false);
        HidePreview();
        if (_impactLabel != null) _impactLabel.text = "";
        _impactLabelUntil = 0f;
        if (_introPanel != null) _introPanel.SetActive(false);
        if (_stageBanner != null) _stageBanner.text = "";
        _stageBannerUntil = 0f;
        _replayArmed = false;
        _replayHoldStartedAt = -1f;

        if (_resultsPanel != null)
        {
            _resultsTitle.text = GetRoundTitle();
            int accuracy = JemparinganScoreRules.AccuracyPercent(_hits, _shots);
            _resultsDetails.text = IsTraditionalMode()
                ? "SCORE " + _score + "  |  ACCURACY " + accuracy + "%" +
                    "\nMOLO " + _moloHits + "  |  JANGGA " + _janggaHits +
                    "\nAWAK " + _awakHits + "  |  BEST " + _best
                : useOutdoorEnvironment
                ? "SCORE " + _score + "  |  ACCURACY " + accuracy + "%" +
                    "\nBULLSEYES " + _bullseyes + "  |  COMBO " + _bestCombo +
                    "\nRASA BONUS +" + _rasaBonus
                : "SCORE  " + _score + "    ACCURACY  " + accuracy + "%" +
                    "\nBULLSEYES  " + _bullseyes + "    BEST COMBO  " + _bestCombo +
                    "\nRASA BONUS  +" + _rasaBonus + "  (" + _rasaHits + " HITS)";
            _resultsHint.text = IsTraditionalMode() && UsesDesktopInput()
                ? (newRecord ? "NEW BEST  |  R: RETRY  |  M: MODES"
                    : "R: RETRY  |  M: MODES")
                : useOutdoorEnvironment && UsesDesktopInput()
                ? (newRecord ? "NEW RECORD!  |  R: RESTART" : "R: RESTART  |  HOLD LEFT 0.7s")
                : (newRecord
                    ? "NEW RECORD!  •  " + (UsesDesktopInput() ? "R TO RESTART" : "RELEASE + HOLD STRING")
                    : (UsesDesktopInput()
                        ? "R TO RESTART / HOLD LEFT 0.7s"
                        : "RELEASE, THEN HOLD STRING 0.7s"));
            _resultsPanel.SetActive(true);
        }
    }

    // These titles are original game feedback, not historical Jemparingan ranks.
    // Evaluate both precision and composure; every player receives a positive result.
    private string GetRoundTitle()
    {
        return IsTraditionalMode()
            ? "TRADITIONAL  •  COMPLETE"
            : JemparinganScoreRules.ResultTitle(_hits, _rasaHits);
    }

    private void ShowImpactLabel(string message, Color color)
    {
        if (_impactLabel == null) return;
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
        for (int i = 0; i < _arrows.Length; i++) _arrows[i].Cancel();
        _shots = _hits = _score = _combo = _poolCursor = 0;
        _bullseyes = _bestCombo = _rasaHits = _rasaBonus = 0;
        _moloHits = _janggaHits = _awakHits = 0;
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
        _target.SetSway(IsTraditionalMode() ? 0f :
                JemparinganScoreRules.SwayAmountForStage(0),
            IsTraditionalMode() ? 0f :
                JemparinganScoreRules.SwaySpeedForStage(0), true);
        _target.ResetReaction();
        UpdateNockedArrow(0f, true);
        HidePreview();
        if (_impactLabel != null) _impactLabel.text = "";
        _impactLabelUntil = 0f;
        ShowFeedback(IsTraditionalMode() ?
            "TRADITIONAL  •  FOCUS" : "ROUND I  •  CALM");
        UpdateIntroMode();
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (_scoreLabel != null)
            _scoreLabel.text = IsTraditionalMode()
                ? "SCORE " + _score + "  |  HITS " + _hits +
                    "  |  ARROWS " + _shots + "/" + arrowsPerRound
                : useOutdoorEnvironment
                    ? "SCORE " + _score + "  |  HITS " + _hits +
                        "  |  ARROWS " + _shots + "/" + arrowsPerRound
                    : "SCORE " + _score + "  •  HITS " + _hits + "/" + _shots +
                        "  •  COMBO " + _combo + "  •  BEST " + _best;
        if (_phaseLabel != null && !_finished)
            _phaseLabel.text = IsTraditionalMode() ?
                "TRADITIONAL  •  MATARAM-INSPIRED" :
                JemparinganScoreRules.StageTitle(
                    useOutdoorEnvironment ? _shots : Mathf.Min(_shots, 11));
        UpdateShotPips();
    }

    private void UpdateShotPips()
    {
        if (_shotPipRenderers == null) return;
        for (int i = 0; i < _shotPipRenderers.Length; i++)
        {
            JemparinganArt.Tone tone = i < _shots ? JemparinganArt.Tone.Gold : JemparinganArt.Tone.Paper;
            int activeStage = Mathf.Min(_shots / JemparinganScoreRules.ArrowsPerStage,
                (arrowsPerRound - 1) / JemparinganScoreRules.ArrowsPerStage);
            bool inStage = i / JemparinganScoreRules.ArrowsPerStage == activeStage;
            if (!_finished && i >= _shots && inStage) tone = JemparinganArt.Tone.Jade;
            _shotPipRenderers[i].sharedMaterial = JemparinganArt.Material(tone);
        }
    }
    private void SetInfo(string value)
    {
        if (useOutdoorEnvironment)
        {
            // Keep first-person VR hints short enough to fit the readable lower panel.
            if (value.StartsWith("READY") || value.StartsWith("Hold left mouse"))
                value = "LMB DRAW  |  RMB AIM  |  SHIFT FINE";
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
        _feedbackLabel.color = feedbackColor ?? new Color(.40f, .94f, .75f);
        _feedbackLabel.text = msg;
        _feedbackUntil = Time.time + 1.5f;
        if (_outdoorFeedbackPanel != null)
            _outdoorFeedbackPanel.SetActive(true);
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
        _nockedArrow.localPosition = Vector3.back * pull;
        if (_nockedArrow.gameObject.activeSelf != visible)
            _nockedArrow.gameObject.SetActive(visible);
    }

    private void UpdateBowStrings(float pull)
    {
        // Outdoor Bow Polish v1 uses the exact mesh endpoints. The original
        // v0.9 bow and string endpoints remain unchanged in legacy scenes.
        Vector3 upperTip = useOutdoorEnvironment
            ? JemparinganBowVisual.UpperTip : Vector3.up * 0.53f;
        Vector3 lowerTip = useOutdoorEnvironment
            ? JemparinganBowVisual.LowerTip : Vector3.down * 0.53f;
        PositionString(_upperString, upperTip, Vector3.back * pull);
        PositionString(_lowerString, lowerTip, Vector3.back * pull);
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
            float previewDuration = useOutdoorEnvironment && _target != null
                ? Mathf.Clamp(Vector3.Distance(origin, _target.transform.position) /
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
        if (_leadMarker != null) _leadMarker.gameObject.SetActive(false);
    }

    private bool BuildScene()
    {
        Camera camera = Camera.main;
        if (camera == null) { Debug.LogError("Jemparingan: MainCamera missing."); return false; }
        // Existing Playable v5 scenes still serialize 12 shots. Upgrade only
        // outdoor gameplay at runtime so no scene regeneration or overwriting
        // user-customized environment assets is required. Legacy stays at 12.
        if (useOutdoorEnvironment)
            arrowsPerRound = JemparinganScoreRules.OutdoorArrowCount;
        Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
        Quaternion facing = Quaternion.LookRotation(forward);
        if (useOutdoorEnvironment && outdoorTargetAnchor == null)
        {
            Debug.LogError("Jemparingan outdoor preview: target anchor is missing.");
            return false;
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
        if (!useOutdoorEnvironment)
            BuildTargetPresentation(root);
        _impactLabel = JemparinganArt.Label("Floating impact points", root,
            _impactLabelBasePosition, "", .0041f, new Color(.98f, .81f, .40f));
        BuildBow();
        BuildTraditionalPosture(camera);
        BuildPool();
        BuildPreview();
        BuildDesktopPracticeSight(camera);
        BuildLeadMarker(root);

        var hud = new GameObject("Jemparingan HUD").transform;
        if (useOutdoorEnvironment)
        {
            // Place all outdoor HUD copy in the upper RIGHT of the viewpoint.
            // Bow remains left; the bandulan/aiming lane stays clear in the center.
            // Keep this world-space board ~2.35m out, without ScreenSpaceOverlay.
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

    // Outdoor-only readability pass. TextMesh is intentionally reused because the
    // project has no TMP/UGUI package dependency yet. These are 3D objects in front
    // of the seated camera, not a screen-space overlay.
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

        // Shared material, very low draw/geometry cost for readable contrast
        // against the bright rice fields and light sky.
        JemparinganArt.Part("HUD title + score contrast panel", PrimitiveType.Cube, hud,
            new Vector3(0f, 1.04f, .085f),
            new Vector3(1.88f, .49f, .025f), JemparinganArt.Tone.DarkWood);
        JemparinganArt.Part("HUD title gold top edge", PrimitiveType.Cube, hud,
            new Vector3(0f, 1.293f, .075f),
            new Vector3(1.88f, .015f, .022f), JemparinganArt.Tone.Gold);
        _outdoorFeedbackPanel = JemparinganArt.Part("HUD feedback contrast panel",
            PrimitiveType.Cube, hud, new Vector3(0f, .67f, .09f),
            new Vector3(1.75f, .18f, .023f), JemparinganArt.Tone.DarkWood);
        _outdoorFeedbackPanel.SetActive(false);
        JemparinganArt.Part("Instruction contrast panel", PrimitiveType.Cube, help,
            new Vector3(0f, 0f, .08f),
            new Vector3(1.77f, .18f, .022f), JemparinganArt.Tone.DarkWood);

        foreach (TextMesh label in hud.GetComponentsInChildren<TextMesh>(true))
        {
            if (label == _titleLabel || label == _scoreLabel ||
                label == _phaseLabel || label == _feedbackLabel) continue;
            label.characterSize *= 1.35f;
            label.fontSize = 96;
            if (label.name == "Intro title")
                label.text = "JEMPARINGAN  /  RASA";
            else if (label.name == "Intro goal")
                label.text = arrowsPerRound + " ARROWS  |  " +
                    JemparinganScoreRules.OutdoorRoundCount + " ROUNDS  |  RASA +3";
            else if (label.name == "Intro start" && UsesDesktopInput())
                label.text = "HOLD LEFT TO START";
        }
    }

    // UI stays in world space; no external canvas or Quest-specific SDK required.
    // It deliberately uses the existing shared primitive materials.
    private void BuildSessionPanels(Transform hud)
    {
        var intro = new GameObject("Session Intro | Auto Hides On Draw");
        intro.transform.SetParent(hud, false);
        intro.transform.localPosition = new Vector3(0f, .09f, -.27f);
        _introPanel = intro;
        JemparinganArt.Part("Intro card", PrimitiveType.Cube, intro.transform,
            Vector3.zero, new Vector3(1.46f, .55f, .016f), JemparinganArt.Tone.DarkWood);
        JemparinganArt.Part("Intro gold edge", PrimitiveType.Cube, intro.transform,
            new Vector3(0f, .268f, -.013f), new Vector3(1.46f, .012f, .01f), JemparinganArt.Tone.Gold);
        JemparinganArt.Label("Intro title", intro.transform, new Vector3(0f, .16f, -.025f),
            "J E M P A R I N G A N   /   R A S A", .0045f, new Color(.98f, .82f, .51f));
        JemparinganArt.Label("Intro goal", intro.transform, new Vector3(0f, .015f, -.025f),
            arrowsPerRound + " ARROWS   •   " +
                Mathf.CeilToInt(arrowsPerRound / (float)JemparinganScoreRules.ArrowsPerStage) +
                " ROUNDS   •   RASA +3 WHEN STEADY", .0035f, Color.white);
        JemparinganArt.Label("Intro start", intro.transform, new Vector3(0f, -.14f, -.025f),
            UsesDesktopInput() ? "HOLD LEFT MOUSE TO START" : "PULL STRING TO START",
            .0037f, new Color(.56f, .94f, .76f));

        var results = new GameObject("Round Results | Hidden Until Finish");
        results.transform.SetParent(hud, false);
        results.transform.localPosition = new Vector3(0f, .09f, -.30f);
        _resultsPanel = results;
        JemparinganArt.Part("Results card", PrimitiveType.Cube, results.transform,
            Vector3.zero, new Vector3(1.62f, .80f, .018f), JemparinganArt.Tone.DarkWood);
        JemparinganArt.Part("Results gold edge", PrimitiveType.Cube, results.transform,
            new Vector3(0f, .39f, -.014f), new Vector3(1.62f, .014f, .01f), JemparinganArt.Tone.Gold);
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
        var targetGO = new GameObject("Bandulan | Target");
        targetGO.transform.SetParent(root, false);
        targetGO.transform.localPosition = new Vector3(0f, -.13f, 0f);
        _target = targetGO.AddComponent<JemparinganTarget>();
        var classic = new GameObject("Classic bandulan visuals");
        classic.transform.SetParent(targetGO.transform, false);
        _classicBandulanVisual = classic;
        var main = JemparinganArt.Part("Bandulan lacquer red", PrimitiveType.Cylinder, classic.transform,
            Vector3.zero, new Vector3(.048f, .24f, .048f), JemparinganArt.Tone.Red);
        _classicBandulanBody = main.GetComponent<Renderer>();
        _target.SetBody(_classicBandulanBody);
        JemparinganArt.Part("Bandulan golden cap", PrimitiveType.Cylinder, classic.transform,
            Vector3.up * .23f, new Vector3(.054f, .018f, .054f), JemparinganArt.Tone.Gold);
        JemparinganArt.Part("Bandulan golden tip", PrimitiveType.Sphere, classic.transform,
            Vector3.down * .24f, new Vector3(.055f, .04f, .055f), JemparinganArt.Tone.Gold);
        for (int i = -1; i <= 1; i++)
        {
            JemparinganArt.Part("Thin gold binding", PrimitiveType.Cylinder, classic.transform,
                new Vector3(0, i * .13f, 0), new Vector3(.052f, .007f, .052f), JemparinganArt.Tone.Brass);
        }

        if (!useOutdoorEnvironment) return;
        var traditional = new GameObject("Traditional bandulan visuals");
        traditional.transform.SetParent(targetGO.transform, false);
        _traditionalBandulanVisual = traditional;
        var awak = JemparinganArt.Part("Awak white body | 1 point",
            PrimitiveType.Cylinder, traditional.transform,
            new Vector3(0f, -.085f, 0f), new Vector3(.048f, .135f, .048f),
            JemparinganArt.Tone.BandulanWhite);
        _traditionalBandulanBody = awak.GetComponent<Renderer>();
        JemparinganArt.Part("Jangga yellow neck | 2 points",
            PrimitiveType.Cylinder, traditional.transform,
            new Vector3(0f, .075f, 0f), new Vector3(.049f, .025f, .049f),
            JemparinganArt.Tone.BandulanYellow);
        JemparinganArt.Part("Molo red head | 3 points",
            PrimitiveType.Cylinder, traditional.transform,
            new Vector3(0f, .148f, 0f), new Vector3(.049f, .048f, .049f),
            JemparinganArt.Tone.Red);
        traditional.SetActive(false);
    }

    private void BuildTraditionalPosture(Camera camera)
    {
        if (!useOutdoorEnvironment) return;
        _traditionalPosture = new GameObject(
            "Traditional seated silhouette (simplified)").transform;
        _traditionalPosture.SetParent(camera.transform, false);
        _traditionalPosture.localPosition = new Vector3(0f, -.43f, .90f);
        JemparinganArt.Part("Seated cloth", PrimitiveType.Sphere,
            _traditionalPosture, new Vector3(0f, -.075f, -.12f),
            new Vector3(.47f, .14f, .40f), JemparinganArt.Tone.Indigo);
        JemparinganArt.Part("Left folded leg", PrimitiveType.Sphere,
            _traditionalPosture, new Vector3(-.22f, 0f, .09f),
            new Vector3(.26f, .13f, .43f), JemparinganArt.Tone.Indigo,
            Quaternion.Euler(0f, -35f, 0f));
        JemparinganArt.Part("Right folded leg", PrimitiveType.Sphere,
            _traditionalPosture, new Vector3(.22f, 0f, .09f),
            new Vector3(.26f, .13f, .43f), JemparinganArt.Tone.Indigo,
            Quaternion.Euler(0f, 35f, 0f));
        _traditionalPosture.gameObject.SetActive(false);
    }

    private void BuildBow()
    {
        _bow = new GameObject("Busur Jemparingan Stylized").transform;

        if (useOutdoorEnvironment)
        {
            // New 1.30m tapered curved mesh plus wrapped grip and subdued
            // metallic details. All visuals only; no changes to shoot origin.
            JemparinganBowVisual.Build(_bow);
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

    private void BuildLeadMarker(Transform root)
    {
        _leadMarker = JemparinganArt.Part("Lead cue | Stages II-IV", PrimitiveType.Sphere,
            root, Vector3.zero, Vector3.one * .023f, JemparinganArt.Tone.Jade).transform;
        _leadMarker.gameObject.SetActive(false);
    }

    private void UpdateLeadMarker(Vector3 arrowOrigin, float power)
    {
        if (_leadMarker == null || _target == null) return;
        bool visible = _shots >= 4 && !_roundOver && power >= .35f;
        if (!visible)
        {
            _leadMarker.gameObject.SetActive(false);
            return;
        }
        float speed = Mathf.Lerp(minimumArrowSpeed, maximumArrowSpeed, power);
        float travelTime = Vector3.Distance(_target.transform.position, arrowOrigin) /
            Mathf.Max(.01f, speed);
        // A small lead guide, not auto-aim: players can always ignore this hint.
        // Keep it slightly toward the viewer so it stays legible over the bandulan.
        _leadMarker.position = _target.PredictCenter(travelTime) -
            _target.transform.forward * .10f;
        _leadMarker.gameObject.SetActive(true);
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

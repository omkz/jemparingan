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
    private Renderer[] _focusRenderers;
    private Transform _upperString, _lowerString;
    private JemparinganTarget _target;
    private JemparinganArrow[] _arrows;
    private Transform[] _previewDots;
    private TextMesh _scoreLabel, _infoLabel, _feedbackLabel, _titleLabel, _phaseLabel;
    private TextMesh _resultsTitle, _resultsDetails, _resultsHint, _stageBanner;
    private GameObject _resultsPanel;
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
    private string _lastInfo = "";
    private int _lastPowerStep = -1;
    private bool _lastStableSignal;

    private IEnumerator Start()
    {
        if (desktopInput == null) desktopInput = GetComponent<DesktopBowInput>();
        if (questInput == null) questInput = GetComponent<MetaQuestBowInput>();
        yield return null;
        _ready = BuildScene();
    }

    private void Update()
    {
        if (Application.isEditor && Input.GetKeyDown(KeyCode.R) && _arrows != null)
        {
            // Restart during a held string must not turn that release into a shot.
            bool stringWasHeld = _wasDrawing || Input.GetMouseButton(0);
            ResetRound();
            _awaitStringRelease = stringWasHeld;
        }
        if (!_ready) return;
        if (_finished) { HandleResultInput(); return; }
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
        if (_roundOver)
        {
            bool anyFlying = false;
            for (int i = 0; i < _arrows.Length; i++) if (_arrows[i].IsFlying) { anyFlying = true; break; }
            // End the current frame after showing results; input must not overwrite the restart hint.
            if (!anyFlying) { FinishRound(); return; }
        }

        IJemparinganInput input = Application.isEditor && !preferQuestHandsInEditor
            ? (IJemparinganInput)desktopInput : questInput;
        if (input == null || !input.TryReadPose(out JemparinganHandPose pose))
        {
            if (_bow != null) _bow.gameObject.SetActive(false);
            HidePreview();
            ClearDrawState();
            SetInfo("Tangan belum terlacak — coba lagi.");
            return;
        }
        if (_roundOver)
        {
            _bow.gameObject.SetActive(false);
            HidePreview();
            return;
        }
        if (!pose.BowPinched)
        {
            _bow.gameObject.SetActive(false);
            HidePreview();
            ClearDrawState();
            SetInfo("Pegang busur dengan tangan kiri.");
            return;
        }

        _bow.gameObject.SetActive(true);
        if (_awaitStringRelease)
        {
            if (pose.StringPinched)
            {
                SetInfo("LEPASKAN TALI UNTUK MEMULAI");
                return;
            }
            _awaitStringRelease = false;
        }
        Vector3 aim = pose.AimDirection.sqrMagnitude > 0.01f ? pose.AimDirection.normalized : _lastAim;
        _lastAim = aim;
        _bow.SetPositionAndRotation(pose.BowPosition, Quaternion.LookRotation(aim, Vector3.up));
        bool drawing = pose.StringPinched;
        float distance = Mathf.Clamp(Vector3.Dot(pose.BowPosition - pose.StringPosition, aim), 0f, maximumDraw);
        float power = Mathf.InverseLerp(minimumDraw, maximumDraw, distance);

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
            ShowPreview(pose.BowPosition + aim * 0.25f, aim, power);
            UpdateLeadMarker(pose.BowPosition + aim * .25f, power);
            int step = Mathf.RoundToInt(power * 10f);
            bool calm = Time.time <= _focusReadyUntil;
            // RASA must update even at constant draw power.
            if (step != _lastPowerStep || calm != _lastStableSignal)
            {
                _lastPowerStep = step;
                _lastStableSignal = calm;
                SetInfo("TARIK " + (step * 10) + "%   |   " + (calm ? "RASA: TENANG" : "RASA: FOKUS"));
            }
        }
        else
        {
            _lastPowerStep = -1;
            _lastStableSignal = false;
            _stringMarker.localPosition = Vector3.zero;
            UpdateBowStrings(0f);
            HidePreview();
            SetInfo("SIAP  •  Tahan klik kiri untuk menarik; lepas untuk menembak. Klik kanan + mouse untuk membidik.");
        }

        // One uninterrupted draw-to-release transition fires exactly once.
        // Tracking loss / bow-unpinch resets without firing.
        if (_wasBowHeld && _wasDrawing && !drawing)
        {
            if (_lastDraw >= minimumDraw && Time.time - _drawStartedAt >= minimumHoldSeconds)
                Shoot(pose.BowPosition + aim * 0.25f, aim, _lastDraw, Time.time <= _focusReadyUntil);
            else
                ShowFeedback("TARIK LEBIH DALAM");
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
        IJemparinganInput input = Application.isEditor && !preferQuestHandsInEditor
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
                SetInfo("LEPASKAN TALI UNTUK MEMULAI");
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
        ShowFeedback("PANAH MELUNCUR");
        if (_shots == 4 || _shots == 8) UpdateLevel();
        if (_shots >= arrowsPerRound) _roundOver = true;
        UpdateHUD();
    }

    // Only adjust shots which are already close to the target; no automatic lock-on from far away.
    private Vector3 GetAssistedAim(Vector3 origin, Vector3 aim, float speed)
    {
        if (_target == null || aimAssist <= 0f) return aim;
        Vector3 delta = _target.transform.position - origin;
        float flightTime = Mathf.Max(0.05f, delta.magnitude / speed);
        Vector3 ballistic = (delta + Vector3.up * (0.5f * simulatedGravity * flightTime * flightTime)).normalized;
        float error = Vector3.Angle(aim, ballistic);
        if (error > 8f) return aim;
        float strength = aimAssist * Mathf.InverseLerp(8f, 0f, error);
        return Vector3.Slerp(aim, ballistic, strength).normalized;
    }

    private void OnArrowImpact(JemparinganArrow arrow, bool bullseye)
    {
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
        ShowImpactLabel("+" + pts + (bullseye ? "  TENGAH" : "  KENA"),
            bullseye ? new Color(.98f, .81f, .40f) : new Color(.40f, .94f, .75f));
        ShowFeedback((bullseye ? "TITIK TENGAH" : "KENA!") + "  +" + pts + (arrow.StableRelease ? "  TENANG" : "") + (_combo >= 3 ? "  KOMBO" : ""),
            bullseye ? new Color(.98f, .81f, .40f) : new Color(.40f, .94f, .75f));
        UpdateHUD();
    }

    private void OnArrowMiss(JemparinganArrow arrow)
    {
        _combo = 0;
        if (_missSound != null) _audio.PlayOneShot(_missSound, 0.16f);
        ShowFeedback("MELESET  •  COBA LAGI", new Color(.98f, .55f, .46f));
        UpdateHUD();
    }

    private void UpdateLevel()
    {
        int level = JemparinganScoreRules.StageIndex(_shots);
        _target.swayAmount = 0.06f + level * 0.045f;
        _target.swaySpeed = 0.7f + level * 0.52f;
        string stage = level == 1 ? "BABAK II  •  ANGIN" : "BABAK III  •  UJIAN RASA";
        ShowFeedback(stage);
        if (_stageBanner != null)
        {
            _stageBanner.text = stage;
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
            PlayerPrefs.SetInt("JemparinganRasaBest", _score);
            PlayerPrefs.Save();
        }
        _best = Mathf.Max(_previousBest, _score);
        _scoreLabel.text = "SELESAI  •  " + _score + " POIN  •  " + _hits + "/" + _shots + " KENA";
        if (_phaseLabel != null) _phaseLabel.text = "R O N D E  S E L E S A I";
        SetInfo(Application.isEditor ? "R UNTUK ULANG  •  ATAU TAHAN KLIK KIRI" : "LEPASKAN, LALU TAHAN TARIK UNTUK ULANG");
        _bow.gameObject.SetActive(false);
        HidePreview();
        if (_impactLabel != null) _impactLabel.text = "";
        _impactLabelUntil = 0f;
        if (_stageBanner != null) _stageBanner.text = "";
        _stageBannerUntil = 0f;
        _replayArmed = false;
        _replayHoldStartedAt = -1f;

        if (_resultsPanel != null)
        {
            _resultsTitle.text = GetRoundTitle();
            int accuracy = JemparinganScoreRules.AccuracyPercent(_hits, _shots);
            _resultsDetails.text = "SKOR  " + _score + "    AKURASI  " + accuracy + "%" +
                "\nTITIK TENGAH  " + _bullseyes + "    KOMBO TERBAIK  " + _bestCombo +
                "\nBONUS RASA  +" + _rasaBonus + "  (" + _rasaHits + " KENA)";
            _resultsHint.text = newRecord
                ? "REKOR BARU!  •  " + (Application.isEditor ? "R UNTUK ULANG" : "LEPAS + TAHAN TARIK")
                : (Application.isEditor
                    ? "R UNTUK ULANG  /  TAHAN KLIK KIRI 0.7 DETIK"
                    : "LEPASKAN, LALU TAHAN TARIK 0.7 DETIK");
            _resultsPanel.SetActive(true);
        }
    }

    // These titles are original game feedback, not historical Jemparingan ranks.
    // Evaluate both precision and composure; every player receives a positive result.
    private string GetRoundTitle()
    {
        return JemparinganScoreRules.ResultTitle(_hits, _rasaHits);
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
        _previousBest = PlayerPrefs.GetInt("JemparinganRasaBest", 0);
        _best = _previousBest;
        ClearDrawState();
        _replayArmed = _awaitStringRelease = false;
        _replayHoldStartedAt = -1f;
        _stageBannerUntil = 0f;
        if (_stageBanner != null) _stageBanner.text = "";
        if (_resultsPanel != null) _resultsPanel.SetActive(false);
        _roundOver = _finished = false;
        _ready = true;
        _target.swayAmount = 0.06f;
        _target.swaySpeed = 0.7f;
        _target.ResetReaction();
        HidePreview();
        if (_impactLabel != null) _impactLabel.text = "";
        _impactLabelUntil = 0f;
        ShowFeedback("BABAK I  •  TENANG");
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (_scoreLabel != null)
            _scoreLabel.text = useOutdoorEnvironment
                ? "SKOR " + _score + "  |  " + _shots + "/12 PANAH  |  KOMBO " + _combo
                : "SKOR " + _score + "  •  KENA " + _hits + "/" + _shots + "  •  KOMBO " + _combo + "  •  REKOR " + _best;
        if (_phaseLabel != null && !_finished)
            _phaseLabel.text = JemparinganScoreRules.StageTitle(_shots);
        UpdateShotPips();
    }

    private void UpdateShotPips()
    {
        if (_shotPipRenderers == null) return;
        for (int i = 0; i < _shotPipRenderers.Length; i++)
        {
            JemparinganArt.Tone tone = i < _shots ? JemparinganArt.Tone.Gold : JemparinganArt.Tone.Paper;
            bool inStage = (_shots < 4 && i < 4) ||
                (_shots >= 4 && _shots < 8 && i >= 4 && i < 8) ||
                (_shots >= 8 && i >= 8);
            if (!_finished && i >= _shots && inStage) tone = JemparinganArt.Tone.Jade;
            _shotPipRenderers[i].sharedMaterial = JemparinganArt.Material(tone);
        }
    }
    private void SetInfo(string value)
    {
        if (useOutdoorEnvironment)
        {
            // Keep first-person VR hints short enough to fit the readable lower panel.
            if (value.StartsWith("SIAP") || value.StartsWith("Tahan klik kiri"))
                value = "KIRI: TARIK / LEPAS   |   KANAN: BIDIK";
            else if (value.StartsWith("Tangan belum"))
                value = "TANGAN BELUM TERLACAK";
            else if (value.StartsWith("Pegang busur"))
                value = "PEGANG BUSUR DENGAN TANGAN KIRI";
        }
        if (_infoLabel != null && _lastInfo != value) { _lastInfo = value; _infoLabel.text = value; }
    }
    private void ShowFeedback(string msg, Color? feedbackColor = null)
    {
        if (_feedbackLabel == null) return;
        _feedbackLabel.color = feedbackColor ?? new Color(.40f, .94f, .75f);
        _feedbackLabel.text = msg;
        _feedbackUntil = Time.time + 1.5f;
    }

    private void UpdateBowStrings(float pull)
    {
        PositionString(_upperString, Vector3.up * 0.53f, Vector3.back * pull);
        PositionString(_lowerString, Vector3.down * 0.53f, Vector3.back * pull);
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
        BuildPool();
        BuildPreview();
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
        BuildResultsDisplay(hud);
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
        SetInfo("Tahan klik kiri untuk menarik, lepas untuk menembak. Klik kanan + mouse untuk membidik.");
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

        foreach (TextMesh label in hud.GetComponentsInChildren<TextMesh>(true))
        {
            if (label == _titleLabel || label == _scoreLabel ||
                label == _phaseLabel || label == _feedbackLabel) continue;
            label.characterSize *= 1.35f;
            label.fontSize = 96;
            if (label.name == "Intro title")
                label.text = "JEMPARINGAN  /  RASA";
            else if (label.name == "Intro goal")
                label.text = "12 PANAH  |  3 BABAK  |  RASA +3";
            else if (label.name == "Intro start" && Application.isEditor)
                label.text = "KLIK KIRI UNTUK MULAI";
        }
    }

    // Results appear only after the round, as text without a 3D background card.
    // Keep the gameplay view clear in all environments.
    private void BuildResultsDisplay(Transform hud)
    {
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
        var parent = new GameObject("12 Shot Progress").transform;
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
        var main = JemparinganArt.Part("Bandulan lacquer red", PrimitiveType.Cylinder, targetGO.transform,
            Vector3.zero, new Vector3(.048f, .24f, .048f), JemparinganArt.Tone.Red);
        _target.SetBody(main.GetComponent<Renderer>());
        JemparinganArt.Part("Bandulan golden cap", PrimitiveType.Cylinder, targetGO.transform,
            Vector3.up * .23f, new Vector3(.054f, .018f, .054f), JemparinganArt.Tone.Gold);
        JemparinganArt.Part("Bandulan golden tip", PrimitiveType.Sphere, targetGO.transform,
            Vector3.down * .24f, new Vector3(.055f, .04f, .055f), JemparinganArt.Tone.Gold);
        for (int i = -1; i <= 1; i++)
        {
            JemparinganArt.Part("Thin gold binding", PrimitiveType.Cylinder, targetGO.transform,
                new Vector3(0, i * .13f, 0), new Vector3(.052f, .007f, .052f), JemparinganArt.Tone.Brass);
        }
    }

    private void BuildBow()
    {
        _bow = new GameObject("Busur Jemparingan Stylized").transform;
        float armRadius = useOutdoorEnvironment ? .027f : .019f;
        float tipRadius = useOutdoorEnvironment ? .019f : .014f;
        float limbCurve = useOutdoorEnvironment ? .08f : .06f;
        float tipCurve = useOutdoorEnvironment ? .025f : .02f;
        JemparinganArt.Tone mainWood = useOutdoorEnvironment ? JemparinganArt.Tone.DarkWood : JemparinganArt.Tone.Wood;
        JemparinganArt.Beam("Upper bow arm", _bow, new Vector3(0, 0, 0), new Vector3(limbCurve, .28f, .03f), armRadius, mainWood);
        JemparinganArt.Beam("Upper bow tip", _bow, new Vector3(limbCurve, .28f, .03f), new Vector3(tipCurve, .53f, .10f), tipRadius, JemparinganArt.Tone.LightWood);
        JemparinganArt.Beam("Lower bow arm", _bow, new Vector3(0, 0, 0), new Vector3(limbCurve, -.28f, .03f), armRadius, mainWood);
        JemparinganArt.Beam("Lower bow tip", _bow, new Vector3(limbCurve, -.28f, .03f), new Vector3(tipCurve, -.53f, .10f), tipRadius, JemparinganArt.Tone.LightWood);
        if (useOutdoorEnvironment)
        {
            // Warm, contrasting details make the silhouette legible against trees.
            JemparinganArt.Beam("Upper polished limb inlay", _bow,
                new Vector3(-.010f, .08f, -.024f), new Vector3(.070f, .29f, .003f),
                .006f, JemparinganArt.Tone.Gold);
            JemparinganArt.Beam("Lower polished limb inlay", _bow,
                new Vector3(-.010f, -.08f, -.024f), new Vector3(.070f, -.29f, .003f),
                .006f, JemparinganArt.Tone.Gold);
        }
        JemparinganArt.Part("Grip", PrimitiveType.Cylinder, _bow, Vector3.zero,
            new Vector3(.036f, .09f, .036f), JemparinganArt.Tone.DarkWood);
        JemparinganArt.Part("Upper bow ornament", PrimitiveType.Sphere, _bow,
            new Vector3(.045f, .34f, .055f), Vector3.one * .025f, JemparinganArt.Tone.Gold);
        JemparinganArt.Part("Lower bow ornament", PrimitiveType.Sphere, _bow,
            new Vector3(.045f, -.34f, .055f), Vector3.one * .025f, JemparinganArt.Tone.Gold);
        _stringMarker = JemparinganArt.Part("String pull bead", PrimitiveType.Sphere, _bow,
            Vector3.zero, Vector3.one * .026f, JemparinganArt.Tone.Jade).transform;
        _upperString = JemparinganArt.Part("Upper string", PrimitiveType.Cylinder, _bow,
            Vector3.zero, Vector3.one * .01f, JemparinganArt.Tone.Paper).transform;
        _lowerString = JemparinganArt.Part("Lower string", PrimitiveType.Cylinder, _bow,
            Vector3.zero, Vector3.one * .01f, JemparinganArt.Tone.Paper).transform;
        UpdateBowStrings(0f);
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
                new Vector3(0, 0, .02f), new Vector3(.008f, .28f, .008f), JemparinganArt.Tone.LightWood,
                Quaternion.Euler(90f, 0f, 0f));
            JemparinganArt.Part("Metal tip", PrimitiveType.Cylinder, obj.transform,
                new Vector3(0, 0, .35f), new Vector3(.017f, .035f, .017f), JemparinganArt.Tone.Gold,
                Quaternion.Euler(90f, 0f, 0f));
            for (int f = -1; f <= 1; f += 2)
                JemparinganArt.Part("Feather", PrimitiveType.Cube, obj.transform,
                    new Vector3(f * .022f, 0, -.20f), new Vector3(.035f, .004f, .11f), JemparinganArt.Tone.Red,
                    Quaternion.Euler(0, f * 22f, 0));
            // One subtle trail per pooled arrow; never instantiated per shot.
            var trail = obj.AddComponent<TrailRenderer>();
            trail.time = .14f;
            trail.minVertexDistance = .025f;
            trail.startWidth = .009f;
            trail.endWidth = .001f;
            trail.sharedMaterial = JemparinganArt.Material(JemparinganArt.Tone.Paper);
            trail.emitting = false;
            _arrows[i] = obj.AddComponent<JemparinganArrow>();
            obj.SetActive(false);
        }
    }

    private void BuildLeadMarker(Transform root)
    {
        _leadMarker = JemparinganArt.Part("Lead cue | Stages II and III", PrimitiveType.Sphere,
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

using System.Collections;
using UnityEngine;

// V0.2 desktop-focused gameplay and procedural art pass.
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
    [SerializeField, Range(4f, 15f)] private float minimumArrowSpeed = 7.5f;
    [SerializeField, Range(6f, 20f)] private float maximumArrowSpeed = 11f;
    [SerializeField, Range(0f, 10f)] private float simulatedGravity = 2.25f;
    [SerializeField, Range(0f, 1f)] private float aimAssist = 0.40f;
    [SerializeField] private bool trajectoryPreview = true;

    private const int ArrowPoolSize = 14;
    private const int PreviewDotCount = 8;
    private Transform _bow, _stringMarker;
    private Transform _upperString, _lowerString;
    private JemparinganTarget _target;
    private JemparinganArrow[] _arrows;
    private Transform[] _previewDots;
    private TextMesh _scoreLabel, _infoLabel, _feedbackLabel, _titleLabel;
    private AudioSource _audio;
    private AudioClip _shotSound, _hitSound;
    private Vector3 _lastAim = Vector3.forward;
    private bool _ready, _wasDrawing, _wasBowHeld, _roundOver, _finished;
    private float _lastDraw, _stableTime, _feedbackUntil;
    private int _shots, _hits, _score, _combo, _best, _poolCursor;
    private string _lastInfo = "";
    private int _lastPowerStep = -1;

    private IEnumerator Start()
    {
        if (desktopInput == null) desktopInput = GetComponent<DesktopBowInput>();
        if (questInput == null) questInput = GetComponent<MetaQuestBowInput>();
        yield return null;
        _ready = BuildScene();
    }

    private void Update()
    {
        if (Application.isEditor && Input.GetKeyDown(KeyCode.R) && _arrows != null) ResetRound();
        if (!_ready || _finished) return;
        if (_feedbackLabel != null && _feedbackUntil > 0f && Time.time >= _feedbackUntil)
        {
            _feedbackLabel.text = "";
            _feedbackUntil = 0f;
        }
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
            _wasDrawing = _wasBowHeld = false;
            _stableTime = 0f;
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
            _wasBowHeld = _wasDrawing = false;
            _stableTime = 0f;
            SetInfo("Pegang busur dengan tangan kiri.");
            return;
        }

        _bow.gameObject.SetActive(true);
        Vector3 aim = pose.AimDirection.sqrMagnitude > 0.01f ? pose.AimDirection.normalized : _lastAim;
        float angleRate = Vector3.Angle(_lastAim, aim) / Mathf.Max(Time.deltaTime, 0.001f);
        _lastAim = aim;
        _bow.SetPositionAndRotation(pose.BowPosition, Quaternion.LookRotation(aim, Vector3.up));
        bool drawing = pose.StringPinched;
        float distance = Mathf.Clamp(Vector3.Dot(pose.BowPosition - pose.StringPosition, aim), 0f, maximumDraw);
        float power = Mathf.InverseLerp(minimumDraw, maximumDraw, distance);

        if (drawing)
        {
            _lastDraw = distance;
            _stableTime = angleRate < 18f && power > 0.35f
                ? Mathf.Min(2f, _stableTime + Time.deltaTime) : Mathf.Max(0f, _stableTime - Time.deltaTime * 2f);
            float pull = Mathf.Clamp(distance, 0f, maximumDraw);
            _stringMarker.localPosition = Vector3.back * pull;
            UpdateBowStrings(pull);
            ShowPreview(pose.BowPosition + aim * 0.25f, aim, power);
            int step = Mathf.RoundToInt(power * 10f);
            if (step != _lastPowerStep)
            {
                _lastPowerStep = step;
                SetInfo("TARIK " + (step * 10) + "%   |   " + (_stableTime > 0.45f ? "RASA: TENANG" : "RASA: FOKUS"));
            }
        }
        else
        {
            _lastPowerStep = -1;
            _stringMarker.localPosition = Vector3.zero;
            UpdateBowStrings(0f);
            HidePreview();
            SetInfo("Tahan klik kiri untuk menarik, lepas untuk menembak. Klik kanan + mouse untuk membidik.");
        }

        // No accidental release after loss of input/tracking.
        if (_wasBowHeld && _wasDrawing && !drawing && _lastDraw >= minimumDraw)
        {
            Shoot(pose.BowPosition + aim * 0.25f, aim, _lastDraw, _stableTime >= 0.45f);
        }
        if (!drawing) _stableTime = 0f;
        _wasDrawing = drawing;
        _wasBowHeld = true;
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
        arrow.Launch(origin, aim * speed, simulatedGravity, _target, OnArrowImpact, OnArrowMiss);
        _shots++;
        _audio.PlayOneShot(_shotSound, 0.48f);
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
        int pts = bullseye ? 10 : 5;
        if (arrow.StableRelease) pts += 3;
        if (_combo >= 3) pts += 2;
        _score += pts;
        if (_score > _best) _best = _score;
        _audio.PlayOneShot(_hitSound, bullseye ? 0.72f : 0.55f);
        ShowFeedback((bullseye ? "TITIK TENGAH" : "KENA!") + "  +" + pts + (arrow.StableRelease ? "  TENANG" : ""));
        UpdateHUD();
    }

    private void OnArrowMiss(JemparinganArrow arrow)
    {
        _combo = 0;
    }

    private void UpdateLevel()
    {
        int level = _shots / 4;
        _target.swayAmount = 0.06f + level * 0.045f;
        _target.swaySpeed = 0.7f + level * 0.52f;
        ShowFeedback(level == 1 ? "BABAK II  •  ANGIN" : "BABAK III  •  UJIAN RASA");
    }

    private void FinishRound()
    {
        // Only called after the final arrow has landed or despawned.
        _roundOver = false;
        _finished = true;
        if (_score > PlayerPrefs.GetInt("JemparinganRasaBest", 0))
        {
            PlayerPrefs.SetInt("JemparinganRasaBest", _score);
            PlayerPrefs.Save();
        }
        _scoreLabel.text = "SELESAI  •  " + _score + " POIN  •  " + _hits + "/" + _shots + " HIT";
        SetInfo("Tekan R untuk mulai ronde baru.");
        _bow.gameObject.SetActive(false);
        HidePreview();
    }

    private void ResetRound()
    {
        for (int i = 0; i < _arrows.Length; i++) _arrows[i].Cancel();
        _shots = _hits = _score = _combo = _poolCursor = 0;
        _stableTime = 0f;
        _wasBowHeld = _wasDrawing = _roundOver = _finished = false;
        _lastDraw = 0f;
        _ready = true;
        _target.swayAmount = 0.06f;
        _target.swaySpeed = 0.7f;
        ShowFeedback("BABAK I  •  TENANG");
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (_scoreLabel != null)
            _scoreLabel.text = "JEMPARINGAN  /  RASA     " + _score + " POIN     " + _shots + "/" + arrowsPerRound + " PANAH     REKOR " + _best;
    }
    private void SetInfo(string value)
    {
        if (_infoLabel != null && _lastInfo != value) { _lastInfo = value; _infoLabel.text = value; }
    }
    private void ShowFeedback(string msg)
    {
        if (_feedbackLabel == null) return;
        _feedbackLabel.text = msg;
        _feedbackUntil = Time.time + 1.5f;
    }

    private void UpdateBowStrings(float pull)
    {
        PositionString(_upperString, Vector3.up * 0.53f, Vector3.back * pull);
        PositionString(_lowerString, Vector3.down * 0.53f, Vector3.back * pull);
    }
    private static void PositionString(Transform piece, Vector3 a, Vector3 b)
    {
        Vector3 delta = b - a;
        piece.localPosition = (a + b) * 0.5f;
        piece.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        piece.localScale = new Vector3(0.0025f, delta.magnitude * 0.5f, 0.0025f);
    }

    private void ShowPreview(Vector3 origin, Vector3 rawAim, float power)
    {
        if (!trajectoryPreview) return;
        float speed = Mathf.Lerp(minimumArrowSpeed, maximumArrowSpeed, power);
        Vector3 velocity = GetAssistedAim(origin, rawAim, speed) * speed;
        for (int i = 0; i < _previewDots.Length; i++)
        {
            float t = (i + 1) * 0.065f;
            _previewDots[i].position = origin + velocity * t + Vector3.down * (0.5f * simulatedGravity * t * t);
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
        Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
        Quaternion facing = Quaternion.LookRotation(forward);
        Vector3 stagePosition = camera.transform.position + forward * 2.65f - Vector3.up * 0.05f;

        var root = new GameObject("JEMPARINGAN | Panggung Pendopo Stylized").transform;
        root.SetPositionAndRotation(stagePosition, facing);
        BuildPendopo(root);
        BuildBandulan(root);
        BuildBow();
        BuildPool();
        BuildPreview();

        var hud = new GameObject("Jemparingan HUD").transform;
        hud.SetPositionAndRotation(stagePosition, facing);
        _titleLabel = JemparinganArt.Label("Title", hud, new Vector3(0f, 1.16f, 0f), "J E M P A R I N G A N   •   R A S A", .0052f, new Color(0.96f, .79f, .48f));
        _scoreLabel = JemparinganArt.Label("Score", hud, new Vector3(0f, .96f, 0f), "", .0037f, Color.white);
        _feedbackLabel = JemparinganArt.Label("Feedback", hud, new Vector3(0f, .77f, 0f), "", .0042f, new Color(.40f, .94f, .75f));
        var help = new GameObject("Jemparingan Instructions").transform;
        help.SetPositionAndRotation(camera.transform.position + forward * 1.05f - Vector3.up * .52f, facing);
        _infoLabel = JemparinganArt.Label("Help", help, Vector3.zero, "", .0026f, new Color(.95f, .89f, .76f));

        _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 0f;
        _audio.playOnAwake = false;
        _shotSound = GenerateTone("Archery String", 0.17f, 185f, true);
        _hitSound = GenerateTone("Bandulan Bell", 0.48f, 520f, false);
        _best = PlayerPrefs.GetInt("JemparinganRasaBest", 0);
        ResetRound();
        SetInfo("Tahan klik kiri untuk menarik, lepas untuk menembak. Klik kanan + mouse untuk membidik.");
        return true;
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
        JemparinganArt.Beam("Upper bow arm", _bow, new Vector3(0, 0, 0), new Vector3(.06f, .28f, .03f), .019f, JemparinganArt.Tone.Wood);
        JemparinganArt.Beam("Upper bow tip", _bow, new Vector3(.06f, .28f, .03f), new Vector3(.02f, .53f, .10f), .014f, JemparinganArt.Tone.LightWood);
        JemparinganArt.Beam("Lower bow arm", _bow, new Vector3(0, 0, 0), new Vector3(.06f, -.28f, .03f), .019f, JemparinganArt.Tone.Wood);
        JemparinganArt.Beam("Lower bow tip", _bow, new Vector3(.06f, -.28f, .03f), new Vector3(.02f, -.53f, .10f), .014f, JemparinganArt.Tone.LightWood);
        JemparinganArt.Part("Grip", PrimitiveType.Cylinder, _bow, Vector3.zero,
            new Vector3(.036f, .09f, .036f), JemparinganArt.Tone.DarkWood);
        _stringMarker = JemparinganArt.Part("String pull bead", PrimitiveType.Sphere, _bow,
            Vector3.zero, Vector3.one * .026f, JemparinganArt.Tone.Jade).transform;
        _upperString = JemparinganArt.Part("Upper string", PrimitiveType.Cylinder, _bow,
            Vector3.zero, Vector3.one * .01f, JemparinganArt.Tone.Paper).transform;
        _lowerString = JemparinganArt.Part("Lower string", PrimitiveType.Cylinder, _bow,
            Vector3.zero, Vector3.one * .01f, JemparinganArt.Tone.Paper).transform;
        UpdateBowStrings(0f);
        _bow.gameObject.SetActive(false);
    }

    private void BuildPool()
    {
        _arrows = new JemparinganArrow[ArrowPoolSize];
        var poolRoot = new GameObject("Arrow Pool (reused)").transform;
        for (int i = 0; i < ArrowPoolSize; i++)
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
            _arrows[i] = obj.AddComponent<JemparinganArrow>();
            obj.SetActive(false);
        }
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

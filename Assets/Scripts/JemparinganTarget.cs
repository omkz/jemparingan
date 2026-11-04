using UnityEngine;

// Procedural bandulan: swept hit test, lightweight recoil and reusable impact sparks.
public class JemparinganTarget : MonoBehaviour
{
    public enum TraditionalZone { None, Awak, Jangga, Molo }
    public TraditionalZone LastTraditionalZone { get; private set; }

    // Shared visual and scoring boundaries. Keep these synchronized with
    // BuildBandulan so every colored region awards its advertised points.
    public const float TraditionalBottom = -.22f;
    public const float TraditionalNeckBottom = .05f;
    public const float TraditionalHeadBottom = .10f;
    public const float TraditionalTop = .196f;
    private bool _useTraditionalScoring;
    private JemparinganArt.Tone _normalBodyTone = JemparinganArt.Tone.Red;
    [Range(0f, 0.3f)] public float swayAmount = 0.10f;
    [Range(0f, 5f)] public float swaySpeed = 0.9f;

    private const float BurstDuration = 0.40f;
    // Fade between round amplitudes instead of teleporting the bandulan.
    private const float SwayAmplitudeTransitionPerSecond = 0.34f;
    private const int SparkCount = 8;
    private Vector3 _rest;
    private Quaternion _restRotation;
    private float _activeSwayAmount;
    private float _swayPhaseOffset;
    private Renderer _body;
    private Renderer _traditionalMolo, _traditionalJangga, _traditionalAwak;
    private Renderer _flashRenderer;
    private JemparinganArt.Tone _flashOriginalTone;
    private Transform _fxRoot;
    private Transform[] _sparks;
    private Renderer[] _sparkRenderers;
    private float _flashUntil;
    private float _hitAt = -1f;
    private bool _bullseye;

    private void Awake()
    {
        _rest = transform.localPosition;
        _restRotation = transform.localRotation;
        _activeSwayAmount = swayAmount;
    }

    public void SetBody(Renderer body, JemparinganArt.Tone normalTone = JemparinganArt.Tone.Red)
    {
        _body = body;
        _normalBodyTone = normalTone;
    }

    public void SetTraditionalZoneRenderers(Renderer molo, Renderer jangga, Renderer awak)
    {
        _traditionalMolo = molo;
        _traditionalJangga = jangga;
        _traditionalAwak = awak;
    }

    public void UseTraditionalScoring(bool traditional)
    {
        _useTraditionalScoring = traditional;
        LastTraditionalZone = TraditionalZone.None;
    }

    // Preserve the old waveform's phase when speed changes, and ease the
    // amplitude over a short window. No random jumps between rounds.
    public void SetSway(float amount, float speed, bool resetImmediately = false)
    {
        amount = Mathf.Clamp(amount, 0f, 0.3f);
        speed = Mathf.Clamp(speed, 0f, 5f);
        if (resetImmediately)
        {
            _activeSwayAmount = amount;
            _swayPhaseOffset = 0f;
        }
        else
        {
            _swayPhaseOffset += Time.time * (swaySpeed - speed);
        }
        swayAmount = amount;
        swaySpeed = speed;
    }

    private void Update()
    {
        _activeSwayAmount = Mathf.MoveTowards(_activeSwayAmount, swayAmount,
            SwayAmplitudeTransitionPerSecond * Time.deltaTime);
        float phase = Time.time * swaySpeed + _swayPhaseOffset;
        transform.localPosition = _rest +
            Vector3.right * (Mathf.Sin(phase) * _activeSwayAmount);

        if (_hitAt >= 0f)
        {
            float age = Time.time - _hitAt;
            float recoil = age < 0.5f
                ? Mathf.Sin(age * 37f) * Mathf.Exp(-age * 9f) * (_bullseye ? 10f : 6f)
                : 0f;
            transform.localRotation = _restRotation * Quaternion.Euler(0f, 0f, recoil);

            if (_fxRoot != null && _fxRoot.gameObject.activeSelf)
            {
                float progress = age / BurstDuration;
                if (progress >= 1f) _fxRoot.gameObject.SetActive(false);
                else AnimateBurst(progress);
            }
        }

        if (_flashUntil > 0f && Time.time >= _flashUntil)
            RestoreFlash();
    }

    // Predicted target center for a small, optional lead cue on later stages.
    // Uses the same deterministic sine function as Update; no random wind.
    public Vector3 PredictCenter(float secondsAhead)
    {
        float ahead = Mathf.Max(0f, secondsAhead);
        float predictedAmount = Mathf.MoveTowards(_activeSwayAmount, swayAmount,
            SwayAmplitudeTransitionPerSecond * ahead);
        float predictedPhase = (Time.time + ahead) * swaySpeed + _swayPhaseOffset;
        float predictedSway = Mathf.Sin(predictedPhase) * predictedAmount;
        Vector3 localCenter = _rest + Vector3.right * predictedSway;
        return transform.parent != null ? transform.parent.TransformPoint(localCenter) : localCenter;
    }

    // Closest distance between the swept arrow tip and the bandulan's central axis.
    // Does not allocate or require Rigidbody/Collider on the target.
    public bool TestShot(Vector3 arrowFrom, Vector3 arrowTo, out bool bullseye)
    {
        LastTraditionalZone = TraditionalZone.None;
        if (_useTraditionalScoring)
        {
            // The cylinder is stylized and forgiving in width, but its colored
            // Y sections use the exact same boundaries as the 3D mesh.
            Vector3 traditionalBottom = transform.TransformPoint(
                new Vector3(0f, TraditionalBottom, 0f));
            Vector3 traditionalTop = transform.TransformPoint(
                new Vector3(0f, TraditionalTop, 0f));
            bool hitTraditional = SegmentDistanceSquared(arrowFrom, arrowTo,
                traditionalBottom, traditionalTop) <= .07f * .07f;
            if (!hitTraditional) { bullseye = false; return false; }

            // Classify the swept arrow tip at the bandulan plane so the thin
            // yellow neck is not swallowed by the head's collision tolerance.
            Vector3 from = transform.InverseTransformPoint(arrowFrom);
            Vector3 to = transform.InverseTransformPoint(arrowTo);
            float dz = to.z - from.z;
            float alpha = Mathf.Abs(dz) > .00001f
                ? Mathf.Clamp01(-from.z / dz) : .5f;
            float hitY = Mathf.Lerp(from.y, to.y, alpha);
            float hitX = Mathf.Lerp(from.x, to.x, alpha);
            // The previous swept-axis test can graze above/below a visual
            // endpoint by 7 cm. Prevent empty-space hits at the bandulan tips.
            if (hitY < TraditionalBottom || hitY > TraditionalTop ||
                Mathf.Abs(hitX) > .07f)
            {
                bullseye = false;
                return false;
            }
            LastTraditionalZone = hitY >= TraditionalHeadBottom
                ? TraditionalZone.Molo
                : hitY >= TraditionalNeckBottom
                    ? TraditionalZone.Jangga : TraditionalZone.Awak;
            bullseye = LastTraditionalZone == TraditionalZone.Molo;
            return true;
        }

        Vector3 axisA = transform.TransformPoint(new Vector3(0f, -0.21f, 0f));
        Vector3 axisB = transform.TransformPoint(new Vector3(0f, 0.21f, 0f));
        float d2 = SegmentDistanceSquared(arrowFrom, arrowTo, axisA, axisB);
        // The visible shaft radius is ~0.048 m and its bindings ~0.052 m.
        // Keep modest forgiveness instead of scoring a visibly distant shot.
        bool hit = d2 <= 0.07f * 0.07f;
        if (!hit) { bullseye = false; return false; }

        // A central hit is near the MIDDLE of the bandulan, not anywhere along
        // the full-length vertical axis. Score a smaller central segment.
        Vector3 centerA = transform.TransformPoint(new Vector3(0f, -0.075f, 0f));
        Vector3 centerB = transform.TransformPoint(new Vector3(0f, 0.075f, 0f));
        float centerDistance = SegmentDistanceSquared(arrowFrom, arrowTo, centerA, centerB);
        bullseye = centerDistance <= 0.035f * 0.035f;
        return true;
    }

    public void Flash(bool bullseye = false)
    {
        _bullseye = bullseye;
        _hitAt = Time.time;
        // Light up the hit colored section rather than flashing only the
        // white body even when the arrow struck the red head.
        RestoreFlash();
        _flashRenderer = _body;
        _flashOriginalTone = _normalBodyTone;
        if (_useTraditionalScoring)
        {
            switch (LastTraditionalZone)
            {
                case TraditionalZone.Molo:
                    _flashRenderer = _traditionalMolo;
                    _flashOriginalTone = JemparinganArt.Tone.Red;
                    break;
                case TraditionalZone.Jangga:
                    _flashRenderer = _traditionalJangga;
                    _flashOriginalTone = JemparinganArt.Tone.BandulanYellow;
                    break;
                case TraditionalZone.Awak:
                    _flashRenderer = _traditionalAwak;
                    _flashOriginalTone = JemparinganArt.Tone.BandulanWhite;
                    break;
            }
        }
        if (_flashRenderer != null)
        {
            _flashRenderer.sharedMaterial = JemparinganArt.Material(JemparinganArt.Tone.Gold);
            _flashUntil = Time.time + 0.14f;
        }
        if (_sparks == null) CreateBurst();
        Material sparkMaterial = JemparinganArt.Material(bullseye ? JemparinganArt.Tone.Gold : JemparinganArt.Tone.Jade);
        for (int i = 0; i < _sparkRenderers.Length; i++)
            _sparkRenderers[i].sharedMaterial = sparkMaterial;
        _fxRoot.gameObject.SetActive(true);
        AnimateBurst(0f);
    }

    private void RestoreFlash()
    {
        if (_flashRenderer != null)
            _flashRenderer.sharedMaterial = JemparinganArt.Material(_flashOriginalTone);
        _flashRenderer = null;
        _flashUntil = 0f;
    }

    public void ResetReaction()
    {
        _hitAt = -1f;
        RestoreFlash();
        transform.localRotation = _restRotation;
        if (_body != null) _body.sharedMaterial = JemparinganArt.Material(_normalBodyTone);
        if (_fxRoot != null) _fxRoot.gameObject.SetActive(false);
    }

    private void CreateBurst()
    {
        _fxRoot = new GameObject("Impact Burst | Reused").transform;
        _fxRoot.SetParent(transform, false);
        // Place the burst on the camera-facing side of the bandulan.
        _fxRoot.localPosition = new Vector3(0f, 0f, -0.075f);
        _sparks = new Transform[SparkCount];
        _sparkRenderers = new Renderer[SparkCount];
        for (int i = 0; i < SparkCount; i++)
        {
            var spark = JemparinganArt.Part("Spark " + i, PrimitiveType.Sphere, _fxRoot,
                Vector3.zero, Vector3.one * 0.016f, JemparinganArt.Tone.Gold);
            _sparks[i] = spark.transform;
            _sparkRenderers[i] = spark.GetComponent<Renderer>();
        }
        _fxRoot.gameObject.SetActive(false);
    }

    private void AnimateBurst(float progress)
    {
        float radius = Mathf.Lerp(0.045f, 0.21f, progress);
        float size = Mathf.Lerp(0.022f, 0.002f, progress);
        for (int i = 0; i < _sparks.Length; i++)
        {
            float angle = (i / (float)SparkCount) * Mathf.PI * 2f;
            _sparks[i].localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            _sparks[i].localScale = Vector3.one * size;
        }
    }

    private static float SegmentDistanceSquared(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
    {
        Vector3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
        float a = Vector3.Dot(d1, d1), e = Vector3.Dot(d2, d2), f = Vector3.Dot(d2, r);
        float s, t;
        if (a <= 1e-8f && e <= 1e-8f) return (p1 - p2).sqrMagnitude;
        if (a <= 1e-8f) { s = 0f; t = Mathf.Clamp01(f / e); }
        else
        {
            float c = Vector3.Dot(d1, r);
            if (e <= 1e-8f) { t = 0f; s = Mathf.Clamp01(-c / a); }
            else
            {
                float b = Vector3.Dot(d1, d2);
                float denominator = a * e - b * b;
                s = denominator > 1e-8f ? Mathf.Clamp01((b * f - c * e) / denominator) : 0f;
                t = (b * s + f) / e;
                if (t < 0f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                else if (t > 1f) { t = 1f; s = Mathf.Clamp01((b - c) / a); }
            }
        }
        Vector3 delta = r + d1 * s - d2 * t;
        return delta.sqrMagnitude;
    }
}

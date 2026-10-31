using UnityEngine;

// Procedural bandulan: swept hit test, lightweight recoil and reusable impact sparks.
public class JemparinganTarget : MonoBehaviour
{
    [Range(0f, 0.3f)] public float swayAmount = 0.10f;
    [Range(0f, 5f)] public float swaySpeed = 0.9f;

    private const float BurstDuration = 0.40f;
    private const int SparkCount = 8;
    private Vector3 _rest;
    private Quaternion _restRotation;
    private Renderer _body;
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
    }

    public void SetBody(Renderer body) { _body = body; }

    private void Update()
    {
        transform.localPosition = _rest + Vector3.right * (Mathf.Sin(Time.time * swaySpeed) * swayAmount);

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

        if (_body != null && _flashUntil > 0f && Time.time >= _flashUntil)
        {
            _body.sharedMaterial = JemparinganArt.Material(JemparinganArt.Tone.Red);
            _flashUntil = 0f;
        }
    }

    // Closest distance between the swept arrow tip and the bandulan's central axis.
    // Does not allocate or require Rigidbody/Collider on the target.
    public bool TestShot(Vector3 arrowFrom, Vector3 arrowTo, out bool bullseye)
    {
        Vector3 axisA = transform.TransformPoint(new Vector3(0f, -0.21f, 0f));
        Vector3 axisB = transform.TransformPoint(new Vector3(0f, 0.21f, 0f));
        float d2 = SegmentDistanceSquared(arrowFrom, arrowTo, axisA, axisB);
        bullseye = d2 <= 0.037f * 0.037f;
        return d2 <= 0.12f * 0.12f;
    }

    public void Flash(bool bullseye = false)
    {
        _bullseye = bullseye;
        _hitAt = Time.time;
        if (_body != null)
        {
            _body.sharedMaterial = JemparinganArt.Material(JemparinganArt.Tone.Gold);
            _flashUntil = Time.time + 0.14f;
        }
        if (_sparks == null) CreateBurst();
        Material sparkMaterial = JemparinganArt.Material(bullseye ? JemparinganArt.Tone.Gold : JemparinganArt.Tone.Jade);
        for (int i = 0; i < _sparkRenderers.Length; i++)
            _sparkRenderers[i].sharedMaterial = sparkMaterial;
        _fxRoot.gameObject.SetActive(true);
        AnimateBurst(0f);
    }

    public void ResetReaction()
    {
        _hitAt = -1f;
        _flashUntil = 0f;
        transform.localRotation = _restRotation;
        if (_body != null) _body.sharedMaterial = JemparinganArt.Material(JemparinganArt.Tone.Red);
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

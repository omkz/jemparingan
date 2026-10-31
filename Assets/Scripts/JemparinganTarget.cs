using UnityEngine;

// Lightweight procedural bandulan sway, with a capsule-shaped scoring zone.
public class JemparinganTarget : MonoBehaviour
{
    [Range(0f, 0.3f)] public float swayAmount = 0.10f;
    [Range(0f, 5f)] public float swaySpeed = 0.9f;
    private Vector3 _rest;
    private Renderer _body;
    private float _flashUntil;

    private void Awake() { _rest = transform.localPosition; }
    public void SetBody(Renderer body) { _body = body; }

    private void Update()
    {
        transform.localPosition = _rest + Vector3.right * (Mathf.Sin(Time.time * swaySpeed) * swayAmount);
        if (_body != null && _flashUntil > 0f && Time.time >= _flashUntil)
        {
            _body.sharedMaterial = JemparinganArt.Material(JemparinganArt.Tone.Red);
            _flashUntil = 0f;
        }
    }

    // Segment-segment closest distance between moving arrow tip and the target's vertical axis.
    // No Rigidbody, Collider, allocations, or per-frame physics synchronization needed.
    public bool TestShot(Vector3 arrowFrom, Vector3 arrowTo, out bool bullseye)
    {
        Vector3 axisA = transform.TransformPoint(new Vector3(0f, -0.21f, 0f));
        Vector3 axisB = transform.TransformPoint(new Vector3(0f, 0.21f, 0f));
        float d2 = SegmentDistanceSquared(arrowFrom, arrowTo, axisA, axisB);
        bullseye = d2 <= 0.037f * 0.037f;
        return d2 <= 0.12f * 0.12f;
    }

    public void Flash()
    {
        if (_body == null) return;
        _body.sharedMaterial = JemparinganArt.Material(JemparinganArt.Tone.Gold);
        _flashUntil = Time.time + 0.14f;
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

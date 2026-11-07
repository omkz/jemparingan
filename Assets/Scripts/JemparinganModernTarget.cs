using UnityEngine;

// Procedural circular target for Modern Archery only. Hit testing uses the
// existing arrow's swept tip; no physics collider or Rigidbody is required.
public sealed class JemparinganModernTarget : MonoBehaviour
{
    public const float Radius = 0.50f;
    public const float FaceDepth = -0.045f;
    public int TargetNumber { get; private set; }
    private GameObject _impactMarker;
    private Transform _face, _burstRoot;
    private Transform[] _sparks;
    private Renderer[] _sparkRenderers;
    private TextMesh _scorePopup;
    private float _impactAt = -1f;
    private float _impactStrength = 1f;
    private bool _isBullseye;
    private const float ImpactDuration = .42f;
    private const int SparkCount = 8;
    // Moving stage translates the entire face AND its collision plane.
    // The stand follows as a lightweight moving assembly; there is no
    // separate physics collider or target-specific aim correction.
    private Vector3 _homeLocalPosition;
    private bool _lateralMotionEnabled;
    private float _lateralMotionStartedAt;
    private float _lateralMotionAmplitude;
    private float _lateralMotionRadiansPerSecond;

    public void Build(int number)
    {
        TargetNumber = number;
        _homeLocalPosition = transform.localPosition;
        // The face reacts subtly without moving the stand or changing hitboxes.
        _face = new GameObject("Circular face | reactive visual").transform;
        _face.SetParent(transform, false);
        // Face points back toward the player's camera (local -Z).
        JemparinganArt.Part("Wooden circular backboard", PrimitiveType.Cylinder,
            _face, new Vector3(0f, 0f, .013f),
            new Vector3(1.10f, .025f, 1.10f),
            JemparinganArt.Tone.DarkWood, Quaternion.Euler(90f, 0f, 0f));

        // Front-most rings are slightly in front of their larger neighbors.
        AddDisc("White | 2", Radius, -.022f, JemparinganArt.Tone.BandulanWhite);
        AddDisc("Black | 4", .39f, -.027f, JemparinganArt.Tone.Indigo);
        AddDisc("Blue | 6", .28f, -.032f, JemparinganArt.Tone.Jade);
        AddDisc("Red | 8", .17f, -.037f, JemparinganArt.Tone.Scarlet);
        AddDisc("Gold | 10", .085f, -.042f, JemparinganArt.Tone.Gold);
        // Raised teak lip and tiny brass pins outside the white scoring ring.
        // This detail follows the cosmetic face recoil but never alters
        // PointsForRadius, FaceDepth, or the scoring hit plane.
        JemparinganTargetTrim.BuildModernFace(_face);

        JemparinganArt.Beam("Target stand vertical", transform,
            new Vector3(0f, -1.14f, .18f), new Vector3(0f, -.46f, .18f),
            .055f, JemparinganArt.Tone.Wood);
        JemparinganArt.Beam("Target stand base", transform,
            new Vector3(-.34f, -1.13f, .20f), new Vector3(.34f, -1.13f, .20f),
            .042f, JemparinganArt.Tone.DarkWood);
        // Two joined braces give the stand a readable, grounded silhouette.
        // They are one mesh renderer, behind the target, without colliders.
        JemparinganTargetTrim.BuildModernStand(transform);
        JemparinganArt.Label("Target number", transform,
            new Vector3(0f, .69f, -.06f), "TARGET " + number,
            .022f, Color.white);

        _impactMarker = JemparinganArt.Part("Impact center | reusable",
            PrimitiveType.Sphere, transform, Vector3.zero,
            new Vector3(.04f, .04f, .012f), JemparinganArt.Tone.BowIvory);
        _impactMarker.SetActive(false);

        _burstRoot = new GameObject("Impact sparks | reused").transform;
        _burstRoot.SetParent(transform, false);
        _sparks = new Transform[SparkCount];
        _sparkRenderers = new Renderer[SparkCount];
        for (int i = 0; i < SparkCount; i++)
        {
            _sparks[i] = JemparinganArt.Part("Impact spark " + i,
                PrimitiveType.Sphere, _burstRoot, Vector3.zero,
                Vector3.one * .02f, JemparinganArt.Tone.Gold).transform;
            _sparkRenderers[i] = _sparks[i].GetComponent<Renderer>();
        }
        _burstRoot.gameObject.SetActive(false);
        _scorePopup = JemparinganArt.Label("Ring score popup", transform,
            Vector3.zero, "", .027f, Color.white);
    }

    private void AddDisc(string name, float radius, float z, JemparinganArt.Tone tone)
    {
        JemparinganArt.Part(name, PrimitiveType.Cylinder, _face,
            new Vector3(0f, 0f, z),
            new Vector3(radius * 2f, .004f, radius * 2f),
            tone, Quaternion.Euler(90f, 0f, 0f));
    }

    // A crossing of the visible front plane must fall inside the circular
    // target. Scoring matches the actual concentric colored ring boundaries.
    public bool TestShot(Vector3 fromWorld, Vector3 toWorld,
        out int points, out Vector3 impactWorld)
    {
        points = 0;
        impactWorld = Vector3.zero;
        Vector3 from = transform.InverseTransformPoint(fromWorld);
        Vector3 to = transform.InverseTransformPoint(toWorld);
        float dz = to.z - from.z;
        if (Mathf.Abs(dz) < .00001f) return false;
        float t = (FaceDepth - from.z) / dz;
        if (t < 0f || t > 1f) return false;

        Vector3 localImpact = Vector3.Lerp(from, to, t);
        float radius = new Vector2(localImpact.x, localImpact.y).magnitude;
        points = PointsForRadius(radius);
        if (points == 0) return false;
        impactWorld = transform.TransformPoint(localImpact);
        return true;
    }

    public static int PointsForRadius(float radius)
    {
        if (radius < 0f || radius > Radius) return 0;
        if (radius <= .085f) return 10;
        if (radius <= .17f) return 8;
        if (radius <= .28f) return 6;
        if (radius <= .39f) return 4;
        return 2;
    }

    public void ShowImpact(Vector3 worldPosition, int ringPoints)
    {
        if (_impactMarker == null) return;
        Vector3 point = transform.InverseTransformPoint(worldPosition);
        point.z = FaceDepth - .018f;
        _impactMarker.transform.localPosition = point;
        _impactMarker.SetActive(true);
        _isBullseye = ringPoints == 10;
        _impactStrength = _isBullseye ? 1.24f : .88f;
        _burstRoot.localPosition = point;
        _burstRoot.gameObject.SetActive(true);
        // Reuse the exact same eight sparks. No prefab instantiate,
        // particle-system emission, or runtime material construction.
        Material sparkMaterial = JemparinganArt.Material(_isBullseye
            ? JemparinganArt.Tone.Gold : JemparinganArt.Tone.Jade);
        for (int i = 0; i < _sparkRenderers.Length; i++)
            _sparkRenderers[i].sharedMaterial = sparkMaterial;
        _scorePopup.transform.localPosition = point + Vector3.up * .15f;
        _scorePopup.transform.localScale = Vector3.one;
        _scorePopup.text = _isBullseye ? "BULLSEYE +10" : "+" + ringPoints;
        _scorePopup.color = _isBullseye ?
            new Color(1f, .89f, .4f) : Color.white;
        _impactAt = Time.time;
        AnimateImpact(0f);
    }

    // Stage 3 only: the middle board travels gently side to side.
    // Reset/retry restores its original local position exactly.
    public void SetLateralMotion(bool enabled, float amplitude = .26f,
        float radiansPerSecond = .85f)
    {
        _lateralMotionEnabled = enabled;
        _lateralMotionAmplitude = Mathf.Clamp(amplitude, 0f, .45f);
        _lateralMotionRadiansPerSecond =
            Mathf.Clamp(radiansPerSecond, .3f, 1.8f);
        _lateralMotionStartedAt = Time.time;
        if (!enabled) transform.localPosition = _homeLocalPosition;
    }

    private void Update()
    {
        if (_lateralMotionEnabled)
        {
            float motionElapsed = Mathf.Max(0f, Time.time - _lateralMotionStartedAt);
            float onset = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(motionElapsed * 1.2f));
            float offset = _lateralMotionAmplitude * onset *
                Mathf.Sin(motionElapsed * _lateralMotionRadiansPerSecond);
            transform.localPosition = _homeLocalPosition +
                Vector3.right * offset;
        }
        if (_impactAt < 0f) return;
        float elapsed = Time.time - _impactAt;
        if (elapsed >= ImpactDuration)
        {
            ResetReaction();
            return;
        }
        float t = Mathf.Clamp01(elapsed / ImpactDuration);
        // A small damped face vibration, never a full target or stand swing.
        float recoil = Mathf.Sin(elapsed * 37f) * Mathf.Exp(-elapsed * 13f);
        if (_face != null)
        {
            // Bullseye punches slightly harder; only the decorative face
            // moves. TestShot still intersects the unchanged root plane.
            _face.localRotation = Quaternion.Euler(
                recoil * 3f * _impactStrength, 0f,
                recoil * 2f * _impactStrength);
            _face.localPosition = Vector3.forward *
                (recoil * .012f * _impactStrength);
            float pulse = 1f + .014f * _impactStrength *
                Mathf.Exp(-elapsed * 19f);
            _face.localScale = Vector3.one * pulse;
        }
        AnimateImpact(t);
        if (_scorePopup != null)
        {
            _scorePopup.transform.localPosition =
                _burstRoot.localPosition + Vector3.up * (.15f + t * .12f);
            float pop = 1f + (_isBullseye ? .32f : .15f) *
                Mathf.Exp(-elapsed * 13f);
            _scorePopup.transform.localScale = Vector3.one * pop;
            Color color = _scorePopup.color;
            color.a = Mathf.Clamp01((1f - t) * 2f);
            _scorePopup.color = color;
        }
    }

    private void AnimateImpact(float progress)
    {
        if (_burstRoot == null || _sparks == null) return;
        float radius = Mathf.Lerp(.025f, .17f, progress) *
            _impactStrength;
        float size = Mathf.Lerp(.024f, .002f, progress) *
            _impactStrength;
        for (int i = 0; i < _sparks.Length; i++)
        {
            float angle = i * (Mathf.PI * 2f / SparkCount);
            _sparks[i].localPosition = new Vector3(
                Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            _sparks[i].localScale = Vector3.one * size;
        }
        if (_impactMarker != null)
            _impactMarker.transform.localScale = new Vector3(
                Mathf.Lerp(.04f, .012f, progress),
                Mathf.Lerp(.04f, .012f, progress), .012f);
    }

    public void ResetReaction()
    {
        _impactAt = -1f;
        if (_impactMarker != null) _impactMarker.SetActive(false);
        if (_burstRoot != null) _burstRoot.gameObject.SetActive(false);
        if (_scorePopup != null)
        {
            _scorePopup.text = "";
            _scorePopup.transform.localScale = Vector3.one;
        }
        if (_face != null)
        {
            _face.localRotation = Quaternion.identity;
            _face.localPosition = Vector3.zero;
            _face.localScale = Vector3.one;
        }
        _impactStrength = 1f;
        _isBullseye = false;
    }
}

using UnityEngine;

// Modern desktop only: compact, fixed bow-mounted sight. Its pin is calibrated
// ONCE at settled full draw; it never chases the camera during gameplay.
// The visual sight never modifies projectile origin/direction or hit scoring.
public sealed class JemparinganModernBowSight : MonoBehaviour
{
    private const int RimSegments = 16;
    private const float RimRadius = .012f;
    private const float SightForwardOffset = .055f;
    private static readonly Vector3 RestingPin = new Vector3(
        -.085f, .185f, SightForwardOffset);

    private Transform _sightRoot;
    private Transform _aperture;
    private Transform _supportRod;
    private Transform _mount;
    private bool _hasCalibration;

    public void Build()
    {
        if (_sightRoot != null) return;

        _sightRoot = new GameObject(
            "Modern | compact recurve sight fixed to riser").transform;
        _sightRoot.SetParent(transform, false);

        _mount = JemparinganArt.Part("Sight bracket on bow riser",
            PrimitiveType.Cube, _sightRoot,
            new Vector3(0f, .115f, .007f),
            new Vector3(.013f, .026f, .014f),
            JemparinganArt.Tone.BowBrass).transform;

        _supportRod = JemparinganArt.Part("Single slim sight support",
            PrimitiveType.Cylinder, _sightRoot,
            Vector3.zero, Vector3.one * .01f,
            JemparinganArt.Tone.BowEdge).transform;

        _aperture = new GameObject(
            "Fixed aperture | slim ring and small centered pin").transform;
        _aperture.SetParent(_sightRoot, false);
        _aperture.localRotation = Quaternion.identity;
        _aperture.localPosition = RestingPin;

        // The aperture and pin are small physical 3D objects (not an overlay).
        for (int i = 0; i < RimSegments; i++)
        {
            float a = Mathf.PI * 2f * i / RimSegments;
            float b = Mathf.PI * 2f * (i + 1) / RimSegments;
            Vector3 from = new Vector3(
                Mathf.Cos(a) * RimRadius, Mathf.Sin(a) * RimRadius, 0f);
            Vector3 to = new Vector3(
                Mathf.Cos(b) * RimRadius, Mathf.Sin(b) * RimRadius, 0f);
            JemparinganArt.Beam("Sight ring edge " + i, _aperture,
                from, to, .0011f, JemparinganArt.Tone.BowBrass);
        }

        JemparinganArt.Beam("Short pin post", _aperture,
            new Vector3(0f, -RimRadius, -.001f),
            new Vector3(0f, -.0032f, -.001f),
            .0011f, JemparinganArt.Tone.BowEdge);
        JemparinganArt.Part("Single fiber sight tip", PrimitiveType.Sphere,
            _aperture, new Vector3(0f, 0f, -.002f),
            Vector3.one * .0027f, JemparinganArt.Tone.Jade);

        PlaceSupport();
        _sightRoot.gameObject.SetActive(false);
    }

    public void ResetCalibration()
    {
        _hasCalibration = false;
        if (_aperture == null) return;
        _aperture.localPosition = RestingPin;
        _aperture.localRotation = Quaternion.identity;
        PlaceSupport();
    }

    public void Present(Camera camera, bool visible, bool calibrate)
    {
        if (_sightRoot == null) return;
        if (_sightRoot.gameObject.activeSelf != visible)
            _sightRoot.gameObject.SetActive(visible);
        if (!visible || !calibrate || _hasCalibration || camera == null)
            return;

        // One-time "sight-in" at stable full draw and zoom. The camera ray
        // is sampled at the fixed physical pin depth; this is then stored
        // in BOW-LOCAL space. Subsequent movement is purely inherited from
        // the bow, never recomputed from the camera.
        float depth = Vector3.Dot(transform.position -
            camera.transform.position, camera.transform.forward);
        depth += transform.lossyScale.z * SightForwardOffset;
        if (depth <= camera.nearClipPlane + .02f) return;

        Vector3 worldPoint = camera.ViewportToWorldPoint(
            new Vector3(.5f, .5f, depth));
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        // Guard against extreme settings producing a comically long mount.
        // If out of physical range, keep the compact fixed resting sight.
        if (localPoint.x < -.20f || localPoint.x > .04f ||
            localPoint.y < .10f || localPoint.y > .34f)
            return;

        localPoint.z = SightForwardOffset;
        _aperture.localPosition = localPoint;
        _hasCalibration = true;
        PlaceSupport();
    }

    private void PlaceSupport()
    {
        if (_supportRod == null || _mount == null || _aperture == null) return;
        Vector3 from = _mount.localPosition + new Vector3(0f, .012f, .006f);
        Vector3 to = _aperture.localPosition +
            Vector3.right * RimRadius;
        Vector3 delta = to - from;
        _supportRod.localPosition = (from + to) * .5f;
        _supportRod.localRotation =
            Quaternion.FromToRotation(Vector3.up, delta.normalized);
        _supportRod.localScale = new Vector3(
            .0028f, delta.magnitude * .5f, .0028f);
    }
}

using UnityEngine;

// Modern desktop only: small bow-mounted aiming aperture and fiber-style pin.
// The geometry is parented to the REAL cosmetic bow transform, not drawn on HUD.
// Zeroing moves the adjustable bracket/pin on the riser, never the arrow,
// camera rotation, projectile origin, scoring or ballistic equation.
public sealed class JemparinganModernBowSight : MonoBehaviour
{
    private const int RimSegments = 20;
    private const float RimRadius = .015f;

    private Transform _sightRoot;
    private Transform _aperture;
    private Transform _mountRod;
    private Transform _outerRod;
    private Transform _mountPlate;
    private bool _hasCalibration;

    public void Build()
    {
        if (_sightRoot != null) return;
        _sightRoot = new GameObject(
            "Modern | bow-mounted adjustable physical sight").transform;
        _sightRoot.SetParent(transform, false);

        _mountPlate = JemparinganArt.Part(
            "Sight riser mount (attached near upper grip)",
            PrimitiveType.Cube, _sightRoot,
            new Vector3(0f, .11f, -.012f),
            new Vector3(.022f, .043f, .025f),
            JemparinganArt.Tone.BowBrass).transform;

        _mountRod = JemparinganArt.Part(
            "Sight extension arm | bow-fixed",
            PrimitiveType.Cylinder, _sightRoot,
            Vector3.zero, Vector3.one * .01f,
            JemparinganArt.Tone.BowEdge).transform;

        _outerRod = JemparinganArt.Part(
            "Sight pin adjustment stem",
            PrimitiveType.Cylinder, _sightRoot,
            Vector3.zero, Vector3.one * .01f,
            JemparinganArt.Tone.BowBrass).transform;

        _aperture = new GameObject(
            "Sight aperture | ring and centered fiber pin").transform;
        _aperture.SetParent(_sightRoot, false);
        _aperture.localPosition = new Vector3(-.055f, .20f, -.014f);
        _aperture.localRotation = Quaternion.identity;

        // Slender full ring, not a wide fake HUD crosshair. Mesh objects are
        // created once; no per-frame primitives, render textures or colliders.
        for (int i = 0; i < RimSegments; i++)
        {
            float a = 2f * Mathf.PI * i / RimSegments;
            float b = 2f * Mathf.PI * (i + 1) / RimSegments;
            Vector3 from = new Vector3(
                Mathf.Cos(a) * RimRadius, Mathf.Sin(a) * RimRadius, 0f);
            Vector3 to = new Vector3(
                Mathf.Cos(b) * RimRadius, Mathf.Sin(b) * RimRadius, 0f);
            JemparinganArt.Beam("Aperture rim " + i, _aperture,
                from, to, .00145f, JemparinganArt.Tone.BowBrass);
        }

        // The actual aiming point is a small physical colored pin fixed to
        // the aperture center. A short stem rises from the lower ring.
        JemparinganArt.Beam("Pin stem", _aperture,
            new Vector3(0f, -RimRadius, -.001f),
            new Vector3(0f, -.004f, -.001f),
            .0013f, JemparinganArt.Tone.BowEdge);
        JemparinganArt.Part("Fiber-style sight pin", PrimitiveType.Sphere,
            _aperture, new Vector3(0f, 0f, -.002f),
            Vector3.one * .0042f, JemparinganArt.Tone.Jade);

        // Start at a plausible resting mount before the player first draws.
        // Actual camera zeroing updates the adjustment bracket at full draw.
        PlaceMount();
        _sightRoot.gameObject.SetActive(false);
    }

    public void Present(Camera camera, bool visible, bool calibrate)
    {
        if (_sightRoot == null) return;
        if (_sightRoot.gameObject.activeSelf != visible)
            _sightRoot.gameObject.SetActive(visible);
        if (!visible || camera == null) return;

        if (calibrate)
        {
            // The screenshot-based bow viewmodel changes depth/scale as FOV
            // settles. Place the pin where the camera ray passes through the
            // bow's own depth, then convert into this bow's LOCAL coordinates.
            // Thus the pin stays a child of the bow and all riser hardware
            // follows its idle / draw / aim pose instead of floating on HUD.
            float depth = Vector3.Dot(transform.position -
                camera.transform.position, camera.transform.forward);
            depth = Mathf.Max(camera.nearClipPlane + .12f, depth + .015f);
            Vector3 cameraAxisAtBowDepth =
                camera.ViewportToWorldPoint(new Vector3(.5f, .5f, depth));
            _aperture.localPosition =
                transform.InverseTransformPoint(cameraAxisAtBowDepth);
            _hasCalibration = true;
        }

        // The aperture faces the eye, but stays under the bow hierarchy;
        // unlike a Canvas reticle it does not render when the bow is hidden.
        _aperture.localRotation =
            Quaternion.Inverse(transform.rotation) * camera.transform.rotation;
        // The mount has a continuously connected extension from the bow grip
        // to the rim. Only its adjustable length changes with calibration.
        PlaceMount();
    }

    private void PlaceMount()
    {
        Vector3 pivot = _mountPlate.localPosition;
        Vector3 ringRight = _aperture.localRotation * Vector3.right;
        Vector3 outerEdge = _aperture.localPosition + ringRight * RimRadius;
        Vector3 elbow = outerEdge + ringRight * .033f;
        PlaceRod(_mountRod, pivot, elbow, .0034f);
        PlaceRod(_outerRod, elbow, outerEdge, .0022f);
    }

    private static void PlaceRod(Transform rod, Vector3 from, Vector3 to,
        float diameter)
    {
        Vector3 diff = to - from;
        if (diff.sqrMagnitude < .000001f) return;
        rod.localPosition = (from + to) * .5f;
        rod.localRotation = Quaternion.FromToRotation(Vector3.up, diff.normalized);
        rod.localScale = new Vector3(diameter, diff.magnitude * .5f, diameter);
    }
}

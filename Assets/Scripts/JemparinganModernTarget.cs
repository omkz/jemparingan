using UnityEngine;

// Procedural circular target for Modern Archery only. Hit testing uses the
// existing arrow's swept tip; no physics collider or Rigidbody is required.
public sealed class JemparinganModernTarget : MonoBehaviour
{
    public const float Radius = 0.50f;
    public const float FaceDepth = -0.045f;
    private GameObject _impactMarker;
    private float _impactUntil;

    public void Build(int number)
    {
        // Face points back toward the player's camera (local -Z).
        JemparinganArt.Part("Wooden circular backboard", PrimitiveType.Cylinder,
            transform, new Vector3(0f, 0f, .013f),
            new Vector3(1.10f, .025f, 1.10f),
            JemparinganArt.Tone.DarkWood, Quaternion.Euler(90f, 0f, 0f));

        // Front-most rings are slightly in front of their larger neighbors.
        AddDisc("White | 2", Radius, -.022f, JemparinganArt.Tone.BandulanWhite);
        AddDisc("Black | 4", .39f, -.027f, JemparinganArt.Tone.Indigo);
        AddDisc("Blue | 6", .28f, -.032f, JemparinganArt.Tone.Jade);
        AddDisc("Red | 8", .17f, -.037f, JemparinganArt.Tone.Scarlet);
        AddDisc("Gold | 10", .085f, -.042f, JemparinganArt.Tone.Gold);

        JemparinganArt.Beam("Target stand vertical", transform,
            new Vector3(0f, -1.14f, .18f), new Vector3(0f, -.46f, .18f),
            .055f, JemparinganArt.Tone.Wood);
        JemparinganArt.Beam("Target stand base", transform,
            new Vector3(-.34f, -1.13f, .20f), new Vector3(.34f, -1.13f, .20f),
            .042f, JemparinganArt.Tone.DarkWood);
        JemparinganArt.Label("Target number", transform,
            new Vector3(0f, .69f, -.06f), "TARGET " + number,
            .022f, Color.white);

        _impactMarker = JemparinganArt.Part("Hit marker (reused)",
            PrimitiveType.Sphere, transform, Vector3.zero,
            new Vector3(.065f, .065f, .02f), JemparinganArt.Tone.Gold);
        _impactMarker.SetActive(false);
    }

    private void AddDisc(string name, float radius, float z, JemparinganArt.Tone tone)
    {
        JemparinganArt.Part(name, PrimitiveType.Cylinder, transform,
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

    public void ShowImpact(Vector3 worldPosition)
    {
        if (_impactMarker == null) return;
        _impactMarker.transform.position = worldPosition + transform.TransformDirection(
            Vector3.back * .007f);
        _impactMarker.SetActive(true);
        _impactUntil = Time.time + .22f;
    }

    private void Update()
    {
        if (_impactMarker != null && _impactMarker.activeSelf &&
            Time.time >= _impactUntil)
            _impactMarker.SetActive(false);
    }

    public void ResetReaction()
    {
        if (_impactMarker != null) _impactMarker.SetActive(false);
        _impactUntil = 0f;
    }
}

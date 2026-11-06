using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Shared first-person hand presentation for Traditional, Challenge and Modern.
// The hand is attached to the real leather grip. The visual-only forearm
// enters from below the camera frustum instead of ending visibly mid-screen.
// This changes no gameplay, input, aim, collider or projectile state.
public sealed class JemparinganTraditionalArcher : MonoBehaviour
{
    private const int Sides = 8;
    private const int ForearmRings = 8;
    private readonly List<Mesh> _meshes = new List<Mesh>();
    private readonly Vector3[] _forearmVertices =
        new Vector3[ForearmRings * Sides + 2];

    private Transform _handle, _bow, _visuals, _leftHand;
    private Mesh _leftSleeve;

    // The mesh wrist is behind the fingers, so the sleeve terminates inside
    // the palm instead of leaving a visible gap at the grip.
    private static readonly Vector3 LeftWrist =
        new Vector3(-.026f, -.012f, -.095f);

    public void Build(Transform handle, Transform bow)
    {
        _handle = handle;
        _bow = bow;

        _visuals = new GameObject(
            "First-person | left grip hand and continuous forearm").transform;
        _visuals.SetParent(transform, false);
        _leftHand = BuildLeftHand();
        _leftSleeve = MakeMesh("First-person left forearm (screen-edge)", true);
        // Register topology once: each frame updates only the deforming
        // vertices, never a recreated mesh or changing index buffer.
        _leftSleeve.vertices = _forearmVertices;
        _leftSleeve.triangles = TubeIndices(ForearmRings);
        AddRenderer("Left sleeve entering from below view", _visuals,
            _leftSleeve, JemparinganArt.Tone.TraditionalCloth);
        // Modern draws its own hands through JemparinganModernHands;
        // this component keeps only the shared left grip and forearm.
        _visuals.gameObject.SetActive(false);
    }

    public void HideArms()
    {
        if (_visuals != null) _visuals.gameObject.SetActive(false);
    }

    public void Present(bool modernReferenceForearm = false)
    {
        if (_handle == null || _bow == null || Camera.main == null) return;
        if (!_visuals.gameObject.activeSelf) _visuals.gameObject.SetActive(true);

        // Align the left grip to the physical bow and keep its forearm
        // continuous with the player's camera framing.
        Vector3 contact = _handle.TransformPoint(Vector3.down * .55f);
        _leftHand.SetPositionAndRotation(contact, _handle.rotation);
        PoseForearm(Camera.main, modernReferenceForearm);
    }

    private void PoseForearm(Camera camera, bool modernReferenceForearm)
    {
        Transform eye = camera.transform;
        Vector3 wristWorld = _leftHand.TransformPoint(LeftWrist);
        // The old endpoint sat INSIDE the camera view (0.23m down at 0.46m
        // depth with a 65-degree FOV), exposing a flat severed sleeve cap.
        // Start it below the lower frustum boundary, leaving a generous
        // margin for tube radius and different desktop aspect ratios.
        float startDepth = Mathf.Max(.55f, camera.nearClipPlane + .18f);
        float halfViewHeight = Mathf.Tan(camera.fieldOfView *
            Mathf.Deg2Rad * .5f) * startDepth;
        // Modern screenshot pose: the sleeve enters diagonally from the
        // bottom-left of the frame, visibly connecting to the grip on the
        // right. Traditional/Challenge retain their restrained original arm.
        Vector3 startWorld = eye.position + eye.forward * startDepth -
            eye.right * (modernReferenceForearm ? .42f : .27f) -
            eye.up * (halfViewHeight + (modernReferenceForearm ? .16f : .14f));
        Vector3 controlWorld = modernReferenceForearm
            ? Vector3.Lerp(startWorld, wristWorld, .53f) +
                eye.up * .075f + eye.right * .010f
            : Vector3.Lerp(startWorld, wristWorld, .56f) +
                eye.up * .018f - eye.right * .010f;

        Vector3 start = transform.InverseTransformPoint(startWorld);
        Vector3 control = transform.InverseTransformPoint(controlWorld);
        Vector3 wrist = transform.InverseTransformPoint(wristWorld);
        Vector3 viewForward = transform.InverseTransformDirection(eye.forward);

        for (int ring = 0; ring < ForearmRings; ring++)
        {
            float t = (float)ring / (ForearmRings - 1);
            Vector3 a = Vector3.Lerp(start, control, t);
            Vector3 b = Vector3.Lerp(control, wrist, t);
            Vector3 point = Vector3.Lerp(a, b, t);
            Vector3 tangent = (2f * (1f - t) * (control - start)
                + 2f * t * (wrist - control)).normalized;
            Vector3 across = Vector3.Cross(tangent, viewForward).normalized;
            if (across.sqrMagnitude < .01f)
                across = Vector3.Cross(tangent, Vector3.up).normalized;
            Vector3 normal = Vector3.Cross(tangent, across).normalized;
            // Wider, tapering low-poly sleeve creates a readable foreground
            // silhouette instead of a small detached wrist in Modern only.
            float radius = modernReferenceForearm
                ? Mathf.Lerp(.090f, .024f, t)
                : Mathf.Lerp(.046f, .021f, t);

            for (int side = 0; side < Sides; side++)
            {
                float angle = side * Mathf.PI * 2f / Sides;
                _forearmVertices[ring * Sides + side] = point + radius *
                    (across * Mathf.Cos(angle) + normal * Mathf.Sin(angle));
            }
        }

        _forearmVertices[ForearmRings * Sides] = start;
        _forearmVertices[ForearmRings * Sides + 1] = wrist;
        _leftSleeve.vertices = _forearmVertices;
        _leftSleeve.RecalculateNormals();
        _leftSleeve.RecalculateBounds();
    }

    private Transform BuildLeftHand()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        Vector3 palm = new Vector3(-.026f, 0f, -.030f);

        AddTube(vertices, triangles,
            new[] { LeftWrist, Vector3.Lerp(LeftWrist, palm, .45f), palm },
            new[] { .025f, .027f, .031f }, .65f);
        for (int finger = 0; finger < 4; finger++)
        {
            float y = .021f - finger * .017f;
            AddTube(vertices, triangles, new[] {
                    new Vector3(-.032f, y, -.022f),
                    new Vector3(-.006f, y, -.026f),
                    new Vector3(.023f, y, -.017f),
                    new Vector3(.026f, y, .008f),
                    new Vector3(.008f, y, .022f) },
                new[] { .009f, .009f, .0085f, .008f, .006f }, 1f);
        }
        AddTube(vertices, triangles, new[] {
                palm + Vector3.down * .028f,
                new Vector3(-.027f, -.035f, .003f),
                new Vector3(-.005f, -.023f, .021f) },
            new[] { .013f, .011f, .008f }, 1f);

        Mesh mesh = MakeMesh("Left hand around leather handle", false);
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Transform hand = AddRenderer("Left hand | fingers around leather handle",
            _visuals, mesh, JemparinganArt.Tone.TraditionalSkin);
        // The procedural finger tubes were floating over a narrow wrist tube,
        // making the hand silhouette look incomplete at some view angles.
        // Add a small, noninteractive palm volume behind the fingers.
        JemparinganArt.Part("Palm fullness | cosmetic", PrimitiveType.Sphere,
            hand, new Vector3(-.018f, -.003f, -.030f),
            new Vector3(.058f, .061f, .062f),
            JemparinganArt.Tone.TraditionalSkin);
        return hand;
    }

    private static void AddTube(List<Vector3> vertices, List<int> triangles,
        Vector3[] path, float[] radii, float flatten)
    {
        int start = vertices.Count;
        for (int ring = 0; ring < path.Length; ring++)
        {
            Vector3 tangent = (path[Mathf.Min(ring + 1, path.Length - 1)]
                - path[Mathf.Max(0, ring - 1)]).normalized;
            Vector3 across = Vector3.Cross(tangent, Vector3.up).normalized;
            if (across.sqrMagnitude < .01f) across = Vector3.right;
            Vector3 normal = Vector3.Cross(tangent, across).normalized;
            for (int side = 0; side < Sides; side++)
            {
                float angle = side * Mathf.PI * 2f / Sides;
                vertices.Add(path[ring] + radii[ring] *
                    (across * Mathf.Cos(angle) * Mathf.Lerp(1f, flatten,
                        (float)ring / (path.Length - 1))
                    + normal * Mathf.Sin(angle)));
            }
        }
        vertices.Add(path[0]);
        vertices.Add(path[path.Length - 1]);
        foreach (int index in TubeIndices(path.Length))
            triangles.Add(start + index);
    }

    private static int[] TubeIndices(int rings)
    {
        var indices = new List<int>((rings - 1) * Sides * 6 + Sides * 6);
        for (int ring = 0; ring < rings - 1; ring++)
        for (int side = 0; side < Sides; side++)
        {
            int a = ring * Sides + side;
            int b = ring * Sides + (side + 1) % Sides;
            int c = a + Sides;
            int d = b + Sides;
            indices.Add(a); indices.Add(b); indices.Add(c);
            indices.Add(b); indices.Add(d); indices.Add(c);
        }
        for (int side = 0; side < Sides; side++)
        {
            int next = (side + 1) % Sides;
            indices.Add(rings * Sides); indices.Add(next); indices.Add(side);
            indices.Add(rings * Sides + 1);
            indices.Add((rings - 1) * Sides + side);
            indices.Add((rings - 1) * Sides + next);
        }
        return indices.ToArray();
    }

    private Mesh MakeMesh(string name, bool dynamic)
    {
        var mesh = new Mesh { name = name };
        if (dynamic) mesh.MarkDynamic();
        _meshes.Add(mesh);
        return mesh;
    }

    private static Transform AddRenderer(string name, Transform parent,
        Mesh mesh, JemparinganArt.Tone tone)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = JemparinganArt.Material(tone);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go.transform;
    }

    private void OnDestroy()
    {
        foreach (Mesh mesh in _meshes)
            if (mesh != null) Destroy(mesh);
    }
}

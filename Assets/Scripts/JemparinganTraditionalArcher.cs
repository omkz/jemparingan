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

    private Transform _handle, _bow, _visuals, _leftHand, _modernRightHand;
    private Mesh _leftSleeve;

    // Cosmetic Modern-only view-model scaling. The draw hand moves closer
    // to the player's camera as the string is pulled. Compensate perspective
    // so it never grows disproportionately large compared with the bow.
    private const float ModernRightHandVisualScale = 0.64f;
    private const float ModernRightHandReferenceDepth = 0.65f;

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
        // The standalone Modern left-hand viewmodel supersedes this
        // historical Modern right-hand prototype. No code path requests
        // Present(modernRightHandDrawing: true) anymore, so don't allocate
        // its unused meshes or renderers in Traditional/Challenge either.
        _visuals.gameObject.SetActive(false);
    }

    public void HideArms()
    {
        if (_visuals != null) _visuals.gameObject.SetActive(false);
    }

    public void Present(bool modernRightHandDrawing = false, Transform nock = null,
        float rightHandTuning = 1f, bool modernReferenceForearm = false)
    {
        if (_handle == null || _bow == null || Camera.main == null) return;
        if (!_visuals.gameObject.activeSelf) _visuals.gameObject.SetActive(true);

        // Grip the lower half of the actual scaled leather wrap. No camera-
        // space hand offset is applied after this contact point is resolved.
        Vector3 contact = _handle.TransformPoint(Vector3.down * .55f);
        _leftHand.SetPositionAndRotation(contact, _handle.rotation);
        PoseForearm(Camera.main, modernReferenceForearm);

        // Modern-only cosmetic draw hand: its fingertips follow the real
        // visual nock and its rotation follows the bow itself. Framing checks
        // happen before toggling visibility, avoiding repeated activation at
        // the edge of the near clip plane.
        if (_modernRightHand != null)
        {
            bool show = modernRightHandDrawing && nock != null &&
                PoseModernRightHand(Camera.main, nock, rightHandTuning);
            if (_modernRightHand.gameObject.activeSelf != show)
                _modernRightHand.gameObject.SetActive(show);
        }
    }

    private void BuildModernRightHand()
    {
        _modernRightHand = new GameObject(
            "Modern | small pinching hand mesh (visual only)").transform;
        _modernRightHand.SetParent(_visuals, false);

        // Earlier prototype combined a floating sphere, cylindrical fingers,
        // and a 30cm pipe-like sleeve. At close first-person distance those
        // separate primitives looked disconnected and oversized.
        // Build one restrained stylized silhouette using tapered organic tubes.
        // No colliders, imported assets or changes to the actual string.
        var skinVertices = new List<Vector3>(176);
        var skinTriangles = new List<int>(768);
        AddTube(skinVertices, skinTriangles,
            new[] {
                new Vector3(.143f, -.063f, .028f),
                new Vector3(.122f, -.055f, .025f),
                new Vector3(.087f, -.036f, .018f),
                new Vector3(.060f, -.020f, .012f) },
            new[] { .016f, .025f, .032f, .024f }, .68f);
        // Index and middle fingers wrap toward the actual bowstring nock.
        AddTube(skinVertices, skinTriangles,
            new[] {
                new Vector3(.065f, .002f, .008f),
                new Vector3(.045f, .013f, .003f),
                new Vector3(.022f, .014f, -.002f),
                new Vector3(.008f, .005f, -.007f) },
            new[] { .012f, .011f, .0085f, .006f }, .84f);
        AddTube(skinVertices, skinTriangles,
            new[] {
                new Vector3(.063f, -.013f, .010f),
                new Vector3(.040f, -.009f, .004f),
                new Vector3(.017f, -.004f, -.006f) },
            new[] { .011f, .0095f, .006f }, .85f);
        // Thumb comes from below and pinches the underside of the nock.
        AddTube(skinVertices, skinTriangles,
            new[] {
                new Vector3(.099f, -.044f, -.001f),
                new Vector3(.064f, -.034f, -.004f),
                new Vector3(.036f, -.021f, -.010f),
                new Vector3(.015f, -.013f, -.008f) },
            new[] { .014f, .012f, .009f, .0055f }, .88f);

        Mesh handMesh = MakeMesh("Modern right hand | organic pinch", false);
        handMesh.SetVertices(skinVertices);
        handMesh.SetTriangles(skinTriangles, 0);
        handMesh.RecalculateNormals();
        handMesh.RecalculateBounds();
        AddRenderer("Right hand | sculpted grip", _modernRightHand,
            handMesh, JemparinganArt.Tone.TraditionalSkin);

        // A small cuff, not a long floating rod. The cuff meets the
        // back of the tapered palm and does not intrude into the sightline.
        var cuffVertices = new List<Vector3>(Sides * 3 + 2);
        var cuffTriangles = new List<int>(Sides * 18);
        AddTube(cuffVertices, cuffTriangles,
            new[] {
                new Vector3(.173f, -.086f, .040f),
                new Vector3(.157f, -.071f, .033f),
                new Vector3(.139f, -.061f, .028f) },
            new[] { .013f, .021f, .018f }, .72f);
        Mesh cuffMesh = MakeMesh("Modern wrist cuff | short", false);
        cuffMesh.SetVertices(cuffVertices);
        cuffMesh.SetTriangles(cuffTriangles, 0);
        cuffMesh.RecalculateNormals();
        cuffMesh.RecalculateBounds();
        AddRenderer("Short wrist cuff", _modernRightHand,
            cuffMesh, JemparinganArt.Tone.TraditionalCloth);
        _modernRightHand.gameObject.SetActive(false);
    }

    private bool PoseModernRightHand(Camera camera, Transform nock,
        float rightHandTuning)
    {
        // The established bow pose is the source of truth. The right hand
        // aligns to the *same* bow rotation as the leather grip, arrow and
        // string; using camera.rotation used to make it look disconnected
        // whenever the player aimed up/down or the bow was canted.
        Vector3 drawAnchor = nock.position - _bow.forward * .012f;
        Vector3 projected = camera.WorldToViewportPoint(drawAnchor);
        bool safeDepth = projected.z > Mathf.Max(.20f,
            camera.nearClipPlane + .08f);
        bool inFrame = projected.x > .03f && projected.x < .95f &&
            projected.y > .05f && projected.y < .93f;
        if (!safeDepth || !inFrame) return false;

        // Keep the fingertips near the nock, with the palm extending toward
        // bow-right. Follow the bow's cant instead of a screen-space offset.
        // This is purely cosmetic and never moves the bow or projectile.
        _modernRightHand.SetPositionAndRotation(drawAnchor, _bow.rotation);

        // Perspective compensation: the physical draw moves the nock toward
        // the eye, so proportionally shrink only the *view-model* hand. The
        // fixed local origin keeps its pinch anchored to the same string.
        float depthFactor = Mathf.Clamp(
            projected.z / ModernRightHandReferenceDepth, .53f, 1f);
        // The right hand is not parented under the bow, so account for the
        // Modern viewmodel bow scaling explicitly. Inspector hand tuning
        // controls relative size without affecting grip or bow geometry.
        float relativeScale = Mathf.Clamp(rightHandTuning, .50f, 1.30f);
        float bowVisualScale = Mathf.Max(.01f, _bow.lossyScale.x);
        _modernRightHand.localScale = Vector3.one *
            (ModernRightHandVisualScale * depthFactor *
             relativeScale * bowVisualScale);
        return true;
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

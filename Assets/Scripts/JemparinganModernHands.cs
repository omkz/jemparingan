using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Independent, Modern-only FPS hands. Built from scratch; the older
// JemparinganTraditionalArcher is never reused for this presentation.
// All meshes are cosmetic: no colliders, hand tracking, shot or camera changes.
public sealed class JemparinganModernHands : MonoBehaviour
{
    private const int Sides = 9;
    private const int SleeveRings = 9;
    private const float ReleaseDuration = .18f;

    private readonly List<Mesh> _meshes = new List<Mesh>();
    private Transform _root, _leftPalm, _rightPalm;
    private Transform _rightIndex, _rightMiddle, _rightThumb;
    private Mesh _leftSleeve, _rightSleeve;
    private Transform _leftSleeveRoot, _rightSleeveRoot;
    private readonly Vector3[] _leftSleeveVertices =
        new Vector3[SleeveRings * Sides];
    private readonly Vector3[] _rightSleeveVertices =
        new Vector3[SleeveRings * Sides];

    private bool _wasDrawing;
    private float _releaseAt = -1f;
    private Vector3 _releaseFrom;
    private Vector3 _lastRightHandPosition;

    public void Build()
    {
        if (_root != null) return;
        _root = new GameObject("Modern FPS | NEW hand rig").transform;
        _root.SetParent(transform, false);

        _leftPalm = new GameObject("Left hand | grip contact").transform;
        _leftPalm.SetParent(_root, false);
        _leftPalm.localPosition = Vector3.zero;
        BuildLeftGrip();

        _rightPalm = new GameObject("Right hand | nock pinch and release").transform;
        _rightPalm.SetParent(_root, false);
        BuildRightDrawHand();

        _leftSleeveRoot = NewSleeve("Left forearm | from bottom-left");
        _rightSleeveRoot = NewSleeve("Right forearm | from bottom-right");
        _leftSleeve = DynamicSleeve(_leftSleeveRoot,
            "Modern left sleeve | deforming", _leftSleeveVertices);
        _rightSleeve = DynamicSleeve(_rightSleeveRoot,
            "Modern right sleeve | deforming", _rightSleeveVertices);

        Hide();
    }

    private void BuildLeftGrip()
    {
        // Integrated tapered wrist + palm volume. Wrapped fingers visibly
        // curl around the actual grip, not the old detached sphere fingers.
        AddSkinnedTube("Left wrist and palm", _leftPalm,
            JemparinganArt.Tone.TraditionalSkin, new[] {
                new Vector3(-.059f, -.072f, -.041f),
                new Vector3(-.046f, -.047f, -.039f),
                new Vector3(-.032f, -.019f, -.033f),
                new Vector3(-.023f, .005f, -.026f) },
            new[] { .020f, .024f, .030f, .028f }, .86f);

        for (int finger = 0; finger < 4; finger++)
        {
            float y = .031f - finger * .019f;
            AddSkinnedTube("Left curled grip finger " + finger,
                _leftPalm, JemparinganArt.Tone.TraditionalSkin,
                new[] {
                    new Vector3(-.037f, y, -.024f),
                    new Vector3(-.013f, y, -.030f),
                    new Vector3(.016f, y - .002f, -.020f),
                    new Vector3(.025f, y - .007f, .003f),
                    new Vector3(.010f, y - .011f, .024f) },
                new[] { .011f, .010f, .009f, .008f, .006f }, .90f);
        }
        AddSkinnedTube("Left thumb presses front of grip", _leftPalm,
            JemparinganArt.Tone.TraditionalSkin, new[] {
                new Vector3(-.029f, -.018f, -.030f),
                new Vector3(-.025f, -.036f, -.008f),
                new Vector3(-.006f, -.022f, .021f) },
            new[] { .013f, .011f, .007f }, .90f);
        AddSkinnedTube("Left cuff | tucked into palm", _leftPalm,
            JemparinganArt.Tone.Indigo, new[] {
                new Vector3(-.070f, -.095f, -.046f),
                new Vector3(-.059f, -.073f, -.042f) },
            new[] { .025f, .024f }, .95f);
    }

    private void BuildRightDrawHand()
    {
        // Palm lives RIGHT of the nock. Separate animated pinch fingers have
        // tips close to (0,0,0) of the original string marker after offset.
        AddSkinnedTube("Right tapered wrist and palm", _rightPalm,
            JemparinganArt.Tone.TraditionalSkin, new[] {
                new Vector3(.110f, -.068f, .012f),
                new Vector3(.091f, -.048f, .009f),
                new Vector3(.067f, -.029f, .001f),
                new Vector3(.044f, -.014f, -.009f) },
            new[] { .020f, .026f, .031f, .024f }, .85f);

        _rightIndex = BuildPinchFinger("Index pulling bowstring",
            new[] { new Vector3(.054f, .002f, -.003f),
                new Vector3(.022f, .023f, -.014f),
                new Vector3(-.023f, .031f, -.027f),
                new Vector3(-.061f, .034f, -.031f) },
            new[] { .012f, .011f, .009f, .006f });
        _rightMiddle = BuildPinchFinger("Middle finger beneath nock",
            new[] { new Vector3(.055f, -.017f, -.006f),
                new Vector3(.019f, -.008f, -.016f),
                new Vector3(-.024f, .002f, -.025f),
                new Vector3(-.060f, .025f, -.030f) },
            new[] { .012f, .0105f, .008f, .0055f });
        _rightThumb = BuildPinchFinger("Thumb below bowstring",
            new[] { new Vector3(.078f, -.044f, .001f),
                new Vector3(.042f, -.046f, -.009f),
                new Vector3(.001f, -.024f, -.020f),
                new Vector3(-.058f, .020f, -.032f) },
            new[] { .014f, .012f, .009f, .006f });

        AddSkinnedTube("Right short cuff", _rightPalm,
            JemparinganArt.Tone.Indigo, new[] {
                new Vector3(.130f, -.084f, .020f),
                new Vector3(.109f, -.067f, .012f) },
            new[] { .023f, .022f }, .90f);
    }

    private Transform BuildPinchFinger(string name, Vector3[] points,
        float[] radii)
    {
        Transform finger = new GameObject(name).transform;
        finger.SetParent(_rightPalm, false);
        AddSkinnedTube("Sculpted " + name, finger,
            JemparinganArt.Tone.TraditionalSkin, points, radii, .90f);
        return finger;
    }

    private void AddSkinnedTube(string name, Transform parent,
        JemparinganArt.Tone material, Vector3[] path, float[] radii,
        float flatten)
    {
        var verts = new List<Vector3>(path.Length * Sides);
        var indices = new List<int>((path.Length - 1) * Sides * 6);
        for (int r = 0; r < path.Length; r++)
        {
            Vector3 tangent = (path[Mathf.Min(r + 1, path.Length - 1)] -
                path[Mathf.Max(0, r - 1)]).normalized;
            Vector3 across = Vector3.Cross(tangent, Vector3.up);
            if (across.sqrMagnitude < .0001f)
                across = Vector3.Cross(tangent, Vector3.forward);
            across.Normalize();
            Vector3 normal = Vector3.Cross(tangent, across).normalized;
            for (int j = 0; j < Sides; j++)
            {
                float a = j * Mathf.PI * 2f / Sides;
                verts.Add(path[r] + radii[r] * (
                    across * Mathf.Cos(a) * flatten +
                    normal * Mathf.Sin(a)));
            }
        }
        AddSleeveFaces(indices, path.Length);
        Mesh mesh = new Mesh { name = name };
        mesh.SetVertices(verts);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        _meshes.Add(mesh);
        AttachMesh(name, parent, mesh, material);
    }

    private Transform NewSleeve(string name)
    {
        Transform sleeve = new GameObject(name).transform;
        sleeve.SetParent(_root, false);
        return sleeve;
    }

    private Mesh DynamicSleeve(Transform parent, string name,
        Vector3[] vertices)
    {
        Mesh mesh = new Mesh { name = name };
        mesh.MarkDynamic();
        var triangles = new List<int>((SleeveRings - 1) * Sides * 6);
        AddSleeveFaces(triangles, SleeveRings);
        mesh.vertices = vertices;
        mesh.SetTriangles(triangles, 0);
        _meshes.Add(mesh);
        AttachMesh(name, parent, mesh, JemparinganArt.Tone.Indigo);
        return mesh;
    }

    private static void AddSleeveFaces(List<int> indices, int rings)
    {
        for (int r = 0; r < rings - 1; r++)
        for (int j = 0; j < Sides; j++)
        {
            int a = r * Sides + j;
            int b = r * Sides + (j + 1) % Sides;
            int c = a + Sides;
            int d = b + Sides;
            // Wind triangles outward, matching the tangent-frame circle.
            // Inverted winding hides hand/sleeve surfaces under backface cull.
            indices.Add(a); indices.Add(b); indices.Add(c);
            indices.Add(b); indices.Add(d); indices.Add(c);
        }
    }

    private static void AttachMesh(string name, Transform parent,
        Mesh mesh, JemparinganArt.Tone tone)
    {
        GameObject object3D = new GameObject(name);
        object3D.transform.SetParent(parent, false);
        object3D.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = object3D.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = JemparinganArt.Material(tone);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // Called AFTER the visual bow, string and nock have been posed in Game.
    // This is a viewmodel; real shot physics continues to use gameplayBow.
    public void Present(Camera camera, Transform nock, bool drawing,
        float drawPower)
    {
        if (_root == null || camera == null || nock == null) return;
        if (!_root.gameObject.activeSelf)
            _root.gameObject.SetActive(true);

        // Left glove remains rigidly wrapped on the scaled riser's grip;
        // animated sleeve end joins the lower wrist of that hand.
        _leftPalm.localPosition = Vector3.zero;
        Vector3 leftWrist = _leftPalm.TransformPoint(
            new Vector3(-.065f, -.085f, -.044f));
        PoseSleeve(camera, _leftSleeve, _leftSleeveVertices,
            camera.ViewportToWorldPoint(new Vector3(.19f, -.27f, .56f)),
            leftWrist, .064f, .021f, .022f, true);

        if (drawing)
        {
            _releaseAt = -1f;
            // Contact follows the ACTUAL visual nock, not a guessed timing
            // or a camera-fixed screen point. Shift toward the bow by the
            // minimum needed to keep fingers beyond the camera near plane.
            Vector3 contact = nock.position +
                transform.right * .065f -
                transform.up * .034f +
                transform.forward * .033f;
            float projected = Vector3.Dot(contact -
                camera.transform.position, camera.transform.forward);
            float safe = camera.nearClipPlane + .085f;
            contact += camera.transform.forward *
                Mathf.Clamp(safe - projected, 0f, .16f);
            _rightPalm.position = contact;
            _rightPalm.localRotation = Quaternion.Euler(
                -Mathf.Lerp(6f, 0f, drawPower), 0f,
                Mathf.Lerp(6f, -2f, drawPower));
            _lastRightHandPosition = _rightPalm.localPosition;
            _wasDrawing = true;
        }
        else if (_wasDrawing)
        {
            _wasDrawing = false;
            _releaseAt = Time.time;
            _releaseFrom = _lastRightHandPosition;
        }

        bool releasing = _releaseAt >= 0f &&
            (Time.time - _releaseAt) < ReleaseDuration;
        bool showRight = drawing || releasing;
        if (_rightPalm.gameObject.activeSelf != showRight)
            _rightPalm.gameObject.SetActive(showRight);
        if (_rightSleeveRoot.gameObject.activeSelf != showRight)
            _rightSleeveRoot.gameObject.SetActive(showRight);
        if (!showRight) return;

        float open = 0f;
        if (!drawing)
        {
            open = Mathf.Clamp01((Time.time - _releaseAt) /
                ReleaseDuration);
            float eased = Mathf.SmoothStep(0f, 1f, open);
            _rightPalm.localPosition = _releaseFrom +
                new Vector3(.13f * eased, -.095f * eased, -.055f * eased);
            _rightPalm.localRotation = Quaternion.Euler(
                -18f * eased, 0f, -15f * eased);
        }
        // Actual finger flex is animated at release instead of keeping
        // a permanently pinched static right hand.
        _rightIndex.localRotation = Quaternion.Euler(0f, 0f, 21f * open);
        _rightMiddle.localRotation = Quaternion.Euler(0f, 0f, -17f * open);
        _rightThumb.localRotation = Quaternion.Euler(0f, 0f, -24f * open);

        Vector3 rightWrist = _rightPalm.TransformPoint(
            new Vector3(.121f, -.077f, .016f));
        PoseSleeve(camera, _rightSleeve, _rightSleeveVertices,
            camera.ViewportToWorldPoint(new Vector3(.95f, -.28f, .55f)),
            rightWrist, .058f, .019f, .020f, false);
    }

    private void PoseSleeve(Camera camera, Mesh sleeve, Vector3[] vertices,
        Vector3 fromWorld, Vector3 wristWorld, float startRadius,
        float endRadius, float bendUp, bool left)
    {
        Vector3 midpoint = Vector3.Lerp(fromWorld, wristWorld, .55f) +
            camera.transform.up * bendUp +
            camera.transform.right * (left ? .02f : -.02f);
        Vector3 start = transform.InverseTransformPoint(fromWorld);
        Vector3 middle = transform.InverseTransformPoint(midpoint);
        Vector3 end = transform.InverseTransformPoint(wristWorld);
        Vector3 face = transform.InverseTransformDirection(
            camera.transform.forward).normalized;
        float scale = Mathf.Max(.1f, transform.lossyScale.x);

        for (int ring = 0; ring < SleeveRings; ring++)
        {
            float t = ring / (float)(SleeveRings - 1);
            Vector3 p0 = Vector3.Lerp(start, middle, t);
            Vector3 p1 = Vector3.Lerp(middle, end, t);
            Vector3 point = Vector3.Lerp(p0, p1, t);
            Vector3 tangent = (2f * (1f - t) * (middle - start) +
                2f * t * (end - middle)).normalized;
            Vector3 across = Vector3.Cross(tangent, face);
            if (across.sqrMagnitude < .001f)
                across = Vector3.Cross(tangent, Vector3.up);
            across.Normalize();
            Vector3 normal = Vector3.Cross(tangent, across).normalized;
            float radius = Mathf.Lerp(startRadius, endRadius, t) / scale;
            for (int j = 0; j < Sides; j++)
            {
                float a = j * Mathf.PI * 2f / Sides;
                vertices[ring * Sides + j] = point + radius *
                    (across * Mathf.Cos(a) +
                     normal * Mathf.Sin(a));
            }
        }
        sleeve.vertices = vertices;
        sleeve.RecalculateNormals();
        sleeve.RecalculateBounds();
    }

    public void Hide()
    {
        if (_root != null) _root.gameObject.SetActive(false);
        _wasDrawing = false;
        _releaseAt = -1f;
    }

    private void OnDestroy()
    {
        foreach (Mesh mesh in _meshes)
            if (mesh != null) Destroy(mesh);
        _meshes.Clear();
    }
}

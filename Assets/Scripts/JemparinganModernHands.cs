using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Modern-only, lightweight LEFT-HAND first-person bow grip.
// Built from scratch (never reusing JemparinganTraditionalArcher).
// Deliberately no right-hand meshes, draw animations, or per-frame work.
// Does not affect the existing bowstring, nock, shot, camera or scoring.
public sealed class JemparinganModernHands : MonoBehaviour
{
    private const int Sides = 9;
    private const int SleeveRings = 9;

    private readonly List<Mesh> _meshes = new List<Mesh>();
    private Transform _root, _leftPalm;
    private Mesh _leftSleeve;
    private readonly Vector3[] _leftSleeveVertices =
        new Vector3[SleeveRings * Sides];

    public void Build()
    {
        if (_root != null) return;
        _root = new GameObject("Modern FPS | left grip only").transform;
        _root.SetParent(transform, false);

        _leftPalm = new GameObject("Left hand | grip contact").transform;
        _leftPalm.SetParent(_root, false);
        _leftPalm.localPosition = Vector3.zero;
        BuildLeftGrip();

        Transform sleeveRoot = NewSleeve("Left forearm | from bottom-left");
        _leftSleeve = DynamicSleeve(sleeveRoot,
            "Modern left sleeve | deforming", _leftSleeveVertices);

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
        // Close both ends of the static finger/palm tubes. The sleeve tube
        // stays uncapped because its start is outside the screen and its
        // other end is buried inside the hand.
        int startCap = verts.Count;
        verts.Add(path[0]);
        int endCap = verts.Count;
        verts.Add(path[path.Length - 1]);
        int last = (path.Length - 1) * Sides;
        for (int side = 0; side < Sides; side++)
        {
            int next = (side + 1) % Sides;
            indices.Add(startCap); indices.Add(next); indices.Add(side);
            indices.Add(endCap);
            indices.Add(last + side); indices.Add(last + next);
        }
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

    // Called after the cosmetic bow has moved. Left palm remains fixed to
    // the grip, while only the sleeve mesh follows the camera framing.
    public void Present(Camera camera)
    {
        if (_root == null || camera == null) return;
        if (!_root.gameObject.activeSelf)
            _root.gameObject.SetActive(true);

        _leftPalm.localPosition = Vector3.zero;
        Vector3 leftWrist = _leftPalm.TransformPoint(
            new Vector3(-.065f, -.085f, -.044f));
        PoseSleeve(camera, _leftSleeve, _leftSleeveVertices,
            camera.ViewportToWorldPoint(new Vector3(.19f, -.27f, .56f)),
            leftWrist, .064f, .021f, .022f);
    }

    private void PoseSleeve(Camera camera, Mesh sleeve, Vector3[] vertices,
        Vector3 fromWorld, Vector3 wristWorld, float startRadius,
        float endRadius, float bendUp)
    {
        Vector3 midpoint = Vector3.Lerp(fromWorld, wristWorld, .55f) +
            camera.transform.up * bendUp + camera.transform.right * .02f;
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
    }

    private void OnDestroy()
    {
        foreach (Mesh mesh in _meshes)
            if (mesh != null) Destroy(mesh);
        _meshes.Clear();
    }
}

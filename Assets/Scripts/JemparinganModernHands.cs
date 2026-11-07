using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Shared desktop Traditional/Modern LEFT-HAND first-person grip.
// Quest tracking is a separate input/visual path: this is not a VR hand rig.
// Keep the grip contact, bowstring, nock, shot, camera and scoring unchanged.
public sealed class JemparinganModernHands : MonoBehaviour
{
    // Slightly rounder close-up silhouette, still tiny procedural geometry.
    // Static finger meshes are built once; only the existing sleeve deforms.
    private const int Sides = 12;
    private const int SleeveRings = 13;
    private const float ReleaseFollowSeconds = .17f;

    // Hand, fingers, and sleeve remain children of the cosmetic bow.
    // These are prebuilt meshes; animation only rotates tiny grip joints.
    private readonly List<Mesh> _meshes = new List<Mesh>();
    private readonly Transform[] _gripFingers = new Transform[4];
    private Transform _root, _leftPalm, _thumb;
    private Mesh _leftSleeve;
    private readonly Vector3[] _leftSleeveVertices =
        new Vector3[SleeveRings * Sides];
    private float _gripPressure;
    private bool _wasDrawing;
    private float _releaseAt = -1f;

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
        // The leather handle is a 0.060m-diameter cylinder centered on
        // bow-local origin. Palm is behind it (-Z), while four differently
        // sized fingers curl around its right edge toward the front (+Z).
        // Keep the grip contact itself at local (0,0,0) while animating.
        AddSkinnedTube("Left tapered wrist into palm", _leftPalm,
            JemparinganArt.Tone.TraditionalSkin, new[] {
                new Vector3(-.064f, -.080f, -.044f),
                new Vector3(-.056f, -.057f, -.042f),
                new Vector3(-.040f, -.032f, -.037f),
                new Vector3(-.027f, -.007f, -.030f),
                new Vector3(-.025f, .022f, -.028f) },
            new[] { .019f, .024f, .029f, .030f, .023f }, .87f);

        // Fingertips do not all end at the same place: the middle finger
        // reaches furthest, ring shorter, and little finger shortest.
        // Pivot each curved finger at its actual knuckle so pressure makes
        // a slight visible curl without detaching it from the palm.
        float[] reach = { .98f, 1.06f, .93f, .81f };
        for (int finger = 0; finger < _gripFingers.Length; finger++)
        {
            float y = .031f - finger * .019f;
            float extension = reach[finger];
            Transform knuckle = new GameObject(
                "Left finger " + finger + " | grip knuckle").transform;
            knuckle.SetParent(_leftPalm, false);
            knuckle.localPosition = new Vector3(-.037f, y, -.025f);
            _gripFingers[finger] = knuckle;

            AddSkinnedTube("Curved grip finger " + finger, knuckle,
                JemparinganArt.Tone.TraditionalSkin, new[] {
                    Vector3.zero,
                    new Vector3(.014f * extension, .002f, -.006f),
                    new Vector3(.039f * extension, .001f, -.008f),
                    new Vector3(.059f * extension, -.001f, .002f),
                    new Vector3(.067f * extension, -.003f, .014f),
                    new Vector3(.062f * extension, -.009f, .031f),
                    new Vector3(.041f * extension, -.013f, .049f) },
                // Fuller knuckles, gradual fingertip taper, same contact tip.
                new[] { .0105f, .0115f, .0107f, .0094f, .0082f,
                    .0068f, .0043f },
                .91f);
        }

        // Opposing thumb presses the grip from underneath, rather than
        // looking like a fifth finger parallel to the others.
        _thumb = new GameObject("Left thumb | opposing grip").transform;
        _thumb.SetParent(_leftPalm, false);
        _thumb.localPosition = new Vector3(-.030f, -.024f, -.030f);
        AddSkinnedTube("Left curved thumb", _thumb,
            JemparinganArt.Tone.TraditionalSkin, new[] {
                Vector3.zero,
                new Vector3(.004f, -.009f, .009f),
                new Vector3(.016f, -.014f, .019f),
                new Vector3(.031f, -.007f, .037f),
                new Vector3(.037f, .003f, .047f) },
            new[] { .014f, .014f, .0115f, .009f, .0055f }, .88f);

        // Keep the navy cuff inserted into the skin, matching the sleeve
        // end so there is no floating wrist or exposed sleeve cap.
        AddSkinnedTube("Left wrist cuff | joins sleeve", _leftPalm,
            JemparinganArt.Tone.Indigo, new[] {
                new Vector3(-.072f, -.103f, -.050f),
                new Vector3(-.064f, -.083f, -.044f),
                new Vector3(-.055f, -.068f, -.042f) },
            new[] { .026f, .025f, .021f }, .94f);
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

    // Called after the shared desktop cosmetic bow has been posed and
    // the gameplay string/nock updated. Only LEFT-HAND animation here.
    // Input, projectile launch, bow pose and optical sight are untouched.
    public void Present(Camera camera, bool drawing,
        float drawPower, float raisedPose)
    {
        if (_root == null || camera == null) return;
        if (!_root.gameObject.activeSelf)
            _root.gameObject.SetActive(true);

        if (_wasDrawing && !drawing) _releaseAt = Time.time;
        if (drawing) _releaseAt = -1f; // Rapid redraw cancels old recoil.
        _wasDrawing = drawing;

        // The large idle-to-full-draw movement belongs to the existing bow
        // controller. This joint adds only subtle grip pressure and recoil
        // (1-2 degrees), without sliding fingers off the 6cm grip.
        float pressureTarget = drawing ? Mathf.Clamp01(drawPower) : 0f;
        _gripPressure = Mathf.MoveTowards(_gripPressure, pressureTarget,
            6f * Time.deltaTime);
        float raised = Mathf.Clamp01(raisedPose);
        float recoil = 0f;
        if (_releaseAt >= 0f)
        {
            float t = (Time.time - _releaseAt) / ReleaseFollowSeconds;
            if (t >= 1f) _releaseAt = -1f;
            else recoil = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        }
        _leftPalm.localPosition = Vector3.zero;
        _leftPalm.localRotation = Quaternion.Euler(
            Mathf.Lerp(1.3f, -.3f, raised) - .6f * _gripPressure +
                1.2f * recoil,
            .65f * _gripPressure,
            Mathf.Lerp(-.9f, .4f, raised) - .8f * recoil);

        for (int finger = 0; finger < _gripFingers.Length; finger++)
        {
            // Tiny knuckle flex (no procedural mesh rebuilding).
            float curl = _gripPressure * (2.6f + finger * .35f);
            _gripFingers[finger].localRotation =
                Quaternion.Euler(0f, -curl, -curl * .32f);
        }
        _thumb.localRotation =
            Quaternion.Euler(0f, 1.3f * _gripPressure, 0f);

        Vector3 leftWrist = _leftPalm.TransformPoint(
            new Vector3(-.066f, -.090f, -.046f));
        float rise = Mathf.SmoothStep(0f, 1f, raised);
        // The sleeve naturally changes its elbow bend as the existing bow
        // rises. Its first ring remains below the screen at all draw stages.
        Vector3 sleeveStart = camera.ViewportToWorldPoint(
            new Vector3(Mathf.Lerp(.20f, .17f, rise),
                -.29f, .56f));
        PoseSleeve(camera, _leftSleeve, _leftSleeveVertices,
            sleeveStart, leftWrist, .061f, .023f,
            Mathf.Lerp(.014f, .040f, rise));
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
        if (_leftPalm != null)
        {
            _leftPalm.localPosition = Vector3.zero;
            _leftPalm.localRotation = Quaternion.identity;
        }
        _wasDrawing = false;
        _gripPressure = 0f;
        _releaseAt = -1f;
    }

    private void OnDestroy()
    {
        foreach (Mesh mesh in _meshes)
            if (mesh != null) Destroy(mesh);
        _meshes.Clear();
    }
}

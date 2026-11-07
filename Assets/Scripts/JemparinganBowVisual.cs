using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Outdoor-only procedural wooden bow art. The existing shooting mechanics,
// hand tracking and projectile origin remain owned by JemparinganGame.
// Generated meshes belong to the bow root and are destroyed with it.
public sealed class JemparinganBowVisual : MonoBehaviour
{
    // More cross-sections improve the VR close-up silhouette while keeping
    // the bow under ~1k triangles. Generated once; never rebuilt per frame.
    private const int LimbSteps = 20;
    private const int RingSides = 12;
    private const float InnerY = 0.075f;
    private const float TipY = 0.65f;
    private readonly List<Mesh> _generatedMeshes = new List<Mesh>();

    // The same sampled curve drives both limb tip geometry and string endpoints.
    public static Vector3 UpperTip { get { return LimbPoint(1f, 1); } }
    public static Vector3 LowerTip { get { return LimbPoint(1f, -1); } }

    public static void Build(Transform bow)
    {
        var visual = bow.gameObject.AddComponent<JemparinganBowVisual>();
        visual.BuildLimb(bow, 1);
        visual.BuildLimb(bow, -1);
        visual.BuildGrip(bow);
        visual.BuildTips(bow);
    }

    private static Vector3 LimbPoint(float t, int sign)
    {
        t = Mathf.Clamp01(t);
        // A gently recurved belly, with the original tip and grip anchors.
        // Extra terms are zero at both ends so existing string endpoints
        // and aiming geometry stay exactly where they were.
        float belly = Mathf.Sin(Mathf.PI * t);
        float x = 0.015f + 0.11f * Mathf.Sin(Mathf.PI * 0.90f * t)
            + 0.014f * belly * belly;
        float y = sign * Mathf.Lerp(InnerY, TipY, t);
        float z = 0.020f + 0.075f * t * t
            - 0.010f * Mathf.Sin(Mathf.PI * 2f * t) * belly;
        return new Vector3(x, y, z);
    }

    private static float HalfWidth(float t)
    {
        return Mathf.Lerp(.024f, .009f,
            Mathf.SmoothStep(0f, 1f, Mathf.Pow(t, .9f))) +
            .001f * Mathf.Sin(Mathf.PI * t);
    }

    private static float HalfDepth(float t)
    {
        return Mathf.Lerp(.016f, .008f,
            Mathf.SmoothStep(0f, 1f, t));
    }

    private void BuildLimb(Transform bow, int sign)
    {
        string side = sign > 0 ? "Upper" : "Lower";
        AddMesh(side + " curved tapered teak limb", bow, BuildLimbMesh(sign),
            JemparinganArt.Tone.BowTeak);
        AddMesh(side + " restrained wood edge inlay", bow, BuildInlayMesh(sign),
            JemparinganArt.Tone.BowEdge);
    }

    private Mesh BuildLimbMesh(int sign)
    {
        var vertices = new List<Vector3>((LimbSteps + 1) * RingSides + 2);
        var uv = new List<Vector2>((LimbSteps + 1) * RingSides + 2);
        var indices = new List<int>(LimbSteps * RingSides * 6 + RingSides * 6);

        for (int i = 0; i <= LimbSteps; i++)
        {
            float t = (float)i / LimbSteps;
            Vector3 spine = LimbPoint(t, sign);
            float halfWidth = HalfWidth(t), halfDepth = HalfDepth(t);
            for (int j = 0; j < RingSides; j++)
            {
                float angle = Mathf.PI * 2f * j / RingSides;
                vertices.Add(spine + new Vector3(
                    Mathf.Cos(angle) * halfWidth, 0f,
                    Mathf.Sin(angle) * halfDepth));
                uv.Add(new Vector2((float)j / RingSides, t));
            }
        }

        for (int i = 0; i < LimbSteps; i++)
        for (int j = 0; j < RingSides; j++)
        {
            int a = i * RingSides + j;
            int b = i * RingSides + (j + 1) % RingSides;
            int c = (i + 1) * RingSides + j;
            int d = (i + 1) * RingSides + (j + 1) % RingSides;
            // Negative-Y limb needs inverted winding; both are outward-facing.
            if (sign > 0)
            {
                Triangle(indices, a, c, b);
                Triangle(indices, b, c, d);
            }
            else
            {
                Triangle(indices, a, b, c);
                Triangle(indices, b, d, c);
            }
        }

        int nearCap = vertices.Count;
        vertices.Add(LimbPoint(0f, sign));
        uv.Add(new Vector2(.5f, 0f));
        int farCap = vertices.Count;
        vertices.Add(LimbPoint(1f, sign));
        uv.Add(new Vector2(.5f, 1f));
        int last = LimbSteps * RingSides;
        for (int j = 0; j < RingSides; j++)
        {
            int next = (j + 1) % RingSides;
            if (sign > 0)
            {
                Triangle(indices, nearCap, j, next);
                Triangle(indices, farCap, last + next, last + j);
            }
            else
            {
                Triangle(indices, nearCap, next, j);
                Triangle(indices, farCap, last + j, last + next);
            }
        }
        return NewMesh("Jemparingan curved limb " + sign, vertices, uv, indices);
    }

    private Mesh BuildInlayMesh(int sign)
    {
        // A slim front-facing wood highlight which follows the actual curve.
        // Front is negative Z because the seated viewer stands behind the bow.
        const int segments = 12;
        var vertices = new List<Vector3>((segments + 1) * 2);
        var uv = new List<Vector2>((segments + 1) * 2);
        var triangles = new List<int>(segments * 6);
        for (int i = 0; i <= segments; i++)
        {
            float fraction = (float)i / segments;
            float t = Mathf.Lerp(0.13f, 0.86f, fraction);
            Vector3 spine = LimbPoint(t, sign);
            float frontZ = spine.z - HalfDepth(t) - 0.0020f;
            float halfStripe = Mathf.Lerp(0.0055f, 0.0020f, fraction);
            vertices.Add(new Vector3(spine.x - halfStripe, spine.y, frontZ));
            vertices.Add(new Vector3(spine.x + halfStripe, spine.y, frontZ));
            uv.Add(new Vector2(0f, fraction));
            uv.Add(new Vector2(1f, fraction));
            if (i == segments) continue;
            int n = i * 2;
            if (sign > 0)
            {
                Triangle(triangles, n, n + 2, n + 1);
                Triangle(triangles, n + 1, n + 2, n + 3);
            }
            else
            {
                Triangle(triangles, n, n + 1, n + 2);
                Triangle(triangles, n + 1, n + 3, n + 2);
            }
        }
        return NewMesh("Jemparingan limb inlay " + sign, vertices, uv, triangles);
    }

    private void BuildGrip(Transform bow)
    {
        // The 18cm grip overlaps the roots of both limbs: no visible gap.
        JemparinganArt.Part("Carved dark teak grip core", PrimitiveType.Cylinder,
            bow, Vector3.zero, new Vector3(.026f, .092f, .025f),
            JemparinganArt.Tone.BowTeak);
        JemparinganArt.Part("Leather-wrapped main grip", PrimitiveType.Cylinder,
            bow, Vector3.zero, new Vector3(.030f, .075f, .029f),
            JemparinganArt.Tone.BowLeather);

        // One stitched helix replaces 14 separate ring and beam objects.
        // It follows the leather surface without adding runtime updates.
        AddMesh("Single stitched leather spiral", bow, BuildGripWrapMesh(),
            JemparinganArt.Tone.BowStitch);

        foreach (float y in new[] {-.090f, .090f})
            JemparinganArt.Part("Muted brass grip collar", PrimitiveType.Cylinder,
                bow, new Vector3(0f, y, 0f), new Vector3(.028f, .006f, .027f),
                JemparinganArt.Tone.BowBrass);
    }

    private Mesh BuildGripWrapMesh()
    {
        // Unity's primitive cylinder has radius 0.5 before scaling.
        // The existing leather handle is .030 x .075 x .029; keep the
        // contact surface and original transform untouched.
        const int segments = 120;
        const float turns = 7f;
        const float radiusX = .01565f;
        const float radiusZ = .01515f;
        const float stripeHalfWidth = .00145f;
        var vertices = new List<Vector3>((segments + 1) * 2);
        var uv = new List<Vector2>((segments + 1) * 2);
        var triangles = new List<int>(segments * 6);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * turns * Mathf.PI * 2f;
            float x = Mathf.Sin(angle) * radiusX;
            float z = -Mathf.Cos(angle) * radiusZ;
            float y = Mathf.Lerp(-.066f, .066f, t);
            vertices.Add(new Vector3(x, y - stripeHalfWidth, z));
            vertices.Add(new Vector3(x, y + stripeHalfWidth, z));
            uv.Add(new Vector2(0f, t * turns));
            uv.Add(new Vector2(1f, t * turns));
            if (i == segments) continue;
            int n = i * 2;
            // Up then circumferential travel: outward-facing normals.
            Triangle(triangles, n, n + 1, n + 2);
            Triangle(triangles, n + 1, n + 3, n + 2);
        }
        return NewMesh("Leather wrap | single curved ribbon", vertices,
            uv, triangles);
    }

    private void BuildTips(Transform bow)
    {
        foreach (int sign in new[] {-1, 1})
        {
            Vector3 tip = LimbPoint(1f, sign);
            JemparinganArt.Part(sign > 0 ? "Upper tip nock" : "Lower tip nock",
                PrimitiveType.Sphere, bow, tip,
                new Vector3(.014f, .023f, .015f),
                JemparinganArt.Tone.BowEdge);
            JemparinganArt.Part(sign > 0 ? "Upper tip string knot" : "Lower tip string knot",
                PrimitiveType.Sphere, bow,
                tip + Vector3.back * .009f,
                new Vector3(.009f, .010f, .009f),
                JemparinganArt.Tone.BowIvory);
        }
    }

    private Mesh NewMesh(string name, List<Vector3> vertices, List<Vector2> uv,
        List<int> triangles)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        _generatedMeshes.Add(mesh);
        return mesh;
    }

    private static void Triangle(List<int> indices, int a, int b, int c)
    {
        indices.Add(a);
        indices.Add(b);
        indices.Add(c);
    }

    private static void AddMesh(string name, Transform parent, Mesh mesh,
        JemparinganArt.Tone tone)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = JemparinganArt.Material(tone);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void OnDestroy()
    {
        // These are runtime-only meshes. Do not leave native mesh allocations
        // behind when stopping Play Mode or destroying the procedural bow.
        foreach (Mesh mesh in _generatedMeshes)
            if (mesh != null) Destroy(mesh);
        _generatedMeshes.Clear();
    }
}

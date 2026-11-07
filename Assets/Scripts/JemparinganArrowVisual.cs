using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Render-only art shared by the nocked preview and the existing projectile pool.
// One mesh per game, built once; no colliders, per-frame work or shot allocations.
public sealed class JemparinganArrowVisual : MonoBehaviour
{
    private const float Length = .94f;
    // Match the existing JemparinganArrow swept tip without moving its root.
    private const float FlyingTipZ = .36f;
    private const int Sides = 12;
    private Mesh _mesh;
    private Material[] _materials;

    public static void Build(Transform owner, Transform parent, bool flying,
        bool outdoor = true)
    {
        var art = owner.GetComponent<JemparinganArrowVisual>();
        if (art == null) art = owner.gameObject.AddComponent<JemparinganArrowVisual>();
        if (art._mesh == null) art.BuildMesh();

        var obj = new GameObject("Traditional arrow | shared cosmetic mesh");
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(0f, 0f,
            flying ? FlyingTipZ - Length : 0f);
        // Keep the legacy indoor arrow a little slimmer, with the same tip Z.
        float width = flying && !outdoor ? .8f : 1f;
        obj.transform.localScale = new Vector3(width, width, 1f);
        obj.AddComponent<MeshFilter>().sharedMesh = art._mesh;
        var renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = art._materials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void BuildMesh()
    {
        // Shared submeshes: bamboo, steel, ivory (quills/nock), brown binding, red feathers.
        var vertices = new List<Vector3>();
        var triangles = new[] { new List<int>(), new List<int>(),
            new List<int>(), new List<int>(), new List<int>() };

        // Fine, gently tapered shaft; shallow bamboo nodes replace the old
        // floating rectangular highlight. All dimensions here are cosmetic.
        Tube(vertices, triangles[0], .018f, .838f, .0046f, .0040f);
        Tube(vertices, triangles[0], .326f, .331f, .0049f, .0049f);
        Tube(vertices, triangles[0], .596f, .601f, .0046f, .0046f);
        Tube(vertices, triangles[1], .824f, .868f, .0054f, .0048f);
        Head(vertices, triangles[1]);

        // Three low-profile, swept feathers, each a solid thin silhouette.
        // A raised quill and brown root bindings read at close range.
        for (int f = 0; f < 3; f++)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, 30f + f * 120f);
            Feather(vertices, triangles[4], rotation, .054f, .175f,
                .0044f, .023f);
            Box(vertices, triangles[2], new Vector3(.0052f, 0f, .116f),
                new Vector3(.0018f, .0018f, .114f), rotation);
        }
        // Restrained thread bindings at feather roots and head socket.
        foreach (float z in new[] { .043f, .047f, .184f, .188f, .815f, .819f })
            Tube(vertices, triangles[3], z, z + .002f, .0053f, .0053f);

        // A compact ivory collar and two clean, narrow cheeks.
        // The open slot faces the string at Z=0; no oversized nock sphere.
        Tube(vertices, triangles[2], .011f, .030f, .0052f, .0046f);
        Box(vertices, triangles[2], new Vector3(-.0037f, 0f, .007f),
            new Vector3(.0035f, .008f, .014f), Quaternion.identity);
        Box(vertices, triangles[2], new Vector3(.0037f, 0f, .007f),
            new Vector3(.0035f, .008f, .014f), Quaternion.identity);

        _mesh = new Mesh { name = "Jemparingan shared bamboo arrow" };
        _mesh.SetVertices(vertices);
        _mesh.subMeshCount = triangles.Length;
        for (int i = 0; i < triangles.Length; i++)
            _mesh.SetTriangles(triangles[i], i);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _materials = new[] {
            JemparinganArt.Material(JemparinganArt.Tone.LightWood),
            JemparinganArt.Material(JemparinganArt.Tone.ArrowSteel),
            JemparinganArt.Material(JemparinganArt.Tone.BowIvory),
            JemparinganArt.Material(JemparinganArt.Tone.BowLeather),
            JemparinganArt.Material(JemparinganArt.Tone.Red)
        };
    }

    private static void Tube(List<Vector3> v, List<int> t, float start,
        float end, float startRadius, float endRadius)
    {
        for (int s = 0; s < Sides; s++)
        {
            float a = s * Mathf.PI * 2f / Sides;
            float b = (s + 1) * Mathf.PI * 2f / Sides;
            Vector3 p = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            Vector3 q = new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0f);
            Vector3 back = Vector3.forward * start, front = Vector3.forward * end;
            Quad(v, t, back + p * startRadius, back + q * startRadius,
                front + q * endRadius, front + p * endRadius);
            Tri(v, t, back, back + q * startRadius, back + p * startRadius);
            Tri(v, t, front, front + p * endRadius, front + q * endRadius);
        }
    }

    private static void Head(List<Vector3> v, List<int> t)
    {
        // Leaf-shaped steel point with a central ridge and beveled faces.
        Vector3 rear = new Vector3(0f, 0f, .852f);
        Vector3 left = new Vector3(-.012f, 0f, .875f);
        Vector3 right = new Vector3(.012f, 0f, .875f);
        Vector3 tip = new Vector3(0f, 0f, Length);
        Vector3 top = new Vector3(0f, .0032f, .880f);
        Vector3 bottom = new Vector3(0f, -.0032f, .880f);
        Tri(v, t, rear, left, top);
        Tri(v, t, left, tip, top);
        Tri(v, t, tip, right, top);
        Tri(v, t, right, rear, top);
        Tri(v, t, rear, bottom, left);
        Tri(v, t, left, bottom, tip);
        Tri(v, t, tip, bottom, right);
        Tri(v, t, right, bottom, rear);
    }

    private static void Feather(List<Vector3> v, List<int> t,
        Quaternion rotation, float rear, float front, float root, float height)
    {
        // Convex profile with a long swept leading edge; .9 mm thick so the
        // feathers remain visible from either side without two-sided shaders.
        var profile = new[] {
            new Vector2(root, rear), new Vector2(height * .72f, rear + .008f),
            new Vector2(height, rear + (front - rear) * .30f),
            new Vector2(height * .88f, rear + (front - rear) * .60f),
            new Vector2(root, front)
        };
        Vector3 center = Vector3.zero;
        foreach (Vector2 p in profile) center += new Vector3(p.x, 0f, p.y);
        center /= profile.Length;
        Vector3 thickness = new Vector3(0f, .00045f, 0f);
        for (int i = 0; i < profile.Length; i++)
        {
            Vector2 p = profile[i], q = profile[(i + 1) % profile.Length];
            Vector3 a = new Vector3(p.x, 0f, p.y), b = new Vector3(q.x, 0f, q.y);
            Tri(v, t, rotation * (center + thickness),
                rotation * (b + thickness), rotation * (a + thickness));
            Tri(v, t, rotation * (center - thickness),
                rotation * (a - thickness), rotation * (b - thickness));
            Quad(v, t, rotation * (b - thickness), rotation * (a - thickness),
                rotation * (a + thickness), rotation * (b + thickness));
        }
    }

    private static void Box(List<Vector3> v, List<int> t, Vector3 center,
        Vector3 size, Quaternion rotation)
    {
        Vector3 h = size * .5f;
        var p = new Vector3[8];
        for (int i = 0; i < 8; i++)
            p[i] = rotation * (center + new Vector3((i & 1) == 0 ? -h.x : h.x,
                (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z));
        Quad(v, t, p[0], p[2], p[3], p[1]);
        Quad(v, t, p[4], p[5], p[7], p[6]);
        Quad(v, t, p[0], p[4], p[6], p[2]);
        Quad(v, t, p[1], p[3], p[7], p[5]);
        Quad(v, t, p[0], p[1], p[5], p[4]);
        Quad(v, t, p[2], p[6], p[7], p[3]);
    }

    private static void Quad(List<Vector3> v, List<int> t,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Tri(v, t, a, b, c);
        Tri(v, t, a, c, d);
    }

    private static void Tri(List<Vector3> v, List<int> t,
        Vector3 a, Vector3 b, Vector3 c)
    {
        int index = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        t.Add(index); t.Add(index + 1); t.Add(index + 2);
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }
}

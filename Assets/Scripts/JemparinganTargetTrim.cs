using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Small, collision-free detail meshes for the already-existing scoring targets.
// Each trim is generated once at Build time; no per-frame work or new materials.
public sealed class JemparinganTargetTrim : MonoBehaviour
{
    private Mesh _ownedMesh;

    // Traditional bandulan: a few fine binding threads and subdued grain
    // highlights. Stay strictly inside the existing silhouette and never
    // extend or visually redefine Molo/Jangga/Awak scoring sections.
    public static void BuildTraditional(Transform parent)
    {
        if (parent.Find("Bandulan | woven binding and wood grain") != null) return;
        var builder = new Builder(2);
        float[] ties = { -.199f, -.104f, .030f, .075f, .176f };
        for (int i = 0; i < ties.Length; i++)
        {
            // Unity's built-in Cylinder radius is 0.5, so the visible
            // target's scale ~.048 yields radius ~.024, not .048.
            // Keep the binds very thin and immediately above that surface.
            float radius = i == 0 || i == 1 || i == 2 ? .0246f : .0251f;
            builder.CylinderBand(0, ties[i], radius, .0025f, 16);
        }

        // Two pale strands provide a natural handmade texture without
        // painting new bright scoring stripes or adding separate renderers.
        const float front = -1.570796327f;
        builder.VerticalRibbon(1, -.181f, -.023f, .0248f,
            front - .37f, .045f);
        builder.VerticalRibbon(1, -.181f, -.023f, .0248f,
            front + .37f, .045f);
        Create("Bandulan | woven binding and wood grain",
            parent, builder, new[]
            {
                JemparinganArt.Material(JemparinganArt.Tone.BowLeather),
                JemparinganArt.Material(JemparinganArt.Tone.BowIvory)
            });
    }

    // A wooden lip on the outer BACKBOARD of the Modern target. All detail
    // remains outside the scored 0.50m radius and never covers a colored ring.
    public static void BuildModernFace(Transform face)
    {
        if (face.Find("Modern | wooden target rim and pins") != null) return;
        var builder = new Builder(2);
        builder.Annulus(0, .502f, .538f, -.0205f, 48);
        // Eight restrained pinheads, not eight primitive GameObjects.
        for (int pin = 0; pin < 8; pin++)
        {
            float angle = (Mathf.PI * 2f * pin) / 8f;
            builder.FacePin(1, .520f, angle, .010f, -.0225f);
        }
        Create("Modern | wooden target rim and pins", face, builder,
            new[]
            {
                JemparinganArt.Material(JemparinganArt.Tone.BowEdge),
                JemparinganArt.Material(JemparinganArt.Tone.BowBrass)
            });
    }

    // A single combined static mesh reinforces the leg silhouette behind
    // the existing stand. The scored face, moving root, and collision plane
    // remain exactly where they were.
    public static void BuildModernStand(Transform target)
    {
        if (target.Find("Modern | joined stand braces") != null) return;
        var builder = new Builder(1);
        builder.BoxBeam(0,
            new Vector3(-.035f, -.66f, .20f),
            new Vector3(-.29f, -1.115f, .20f), .025f);
        builder.BoxBeam(0,
            new Vector3(.035f, -.66f, .20f),
            new Vector3(.29f, -1.115f, .20f), .025f);
        Create("Modern | joined stand braces", target, builder,
            new[] { JemparinganArt.Material(JemparinganArt.Tone.BowTeak) });
    }

    private static void Create(string name, Transform parent,
        Builder builder, Material[] materials)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var owner = go.AddComponent<JemparinganTargetTrim>();
        owner._ownedMesh = builder.ToMesh(name);
        go.AddComponent<MeshFilter>().sharedMesh = owner._ownedMesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void OnDestroy()
    {
        if (_ownedMesh != null) Destroy(_ownedMesh);
    }

    // Each contiguous trim assembly uses one shared mesh. Submeshes only
    // separate the existing wood/brass material slots; no dynamic batches.
    private sealed class Builder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int>[] _indices;

        public Builder(int submeshCount)
        {
            _indices = new List<int>[submeshCount];
            for (int i = 0; i < submeshCount; i++)
                _indices[i] = new List<int>();
        }

        private void Quad(int submesh,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int start = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _vertices.Add(d);
            var triangles = _indices[submesh];
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        public void CylinderBand(int submesh, float y, float radius,
            float height, int segments)
        {
            float bottom = y - height * .5f;
            float top = y + height * .5f;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float b = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 lowerA = new Vector3(
                    radius * Mathf.Cos(a), bottom, radius * Mathf.Sin(a));
                Vector3 upperA = new Vector3(
                    radius * Mathf.Cos(a), top, radius * Mathf.Sin(a));
                Vector3 upperB = new Vector3(
                    radius * Mathf.Cos(b), top, radius * Mathf.Sin(b));
                Vector3 lowerB = new Vector3(
                    radius * Mathf.Cos(b), bottom, radius * Mathf.Sin(b));
                Quad(submesh, lowerA, upperA, upperB, lowerB);
            }
        }

        public void VerticalRibbon(int submesh, float bottom, float top,
            float radius, float angle, float widthRadians)
        {
            float a = angle - widthRadians * .5f;
            float b = angle + widthRadians * .5f;
            Quad(submesh,
                new Vector3(radius * Mathf.Cos(a), bottom,
                    radius * Mathf.Sin(a)),
                new Vector3(radius * Mathf.Cos(a), top,
                    radius * Mathf.Sin(a)),
                new Vector3(radius * Mathf.Cos(b), top,
                    radius * Mathf.Sin(b)),
                new Vector3(radius * Mathf.Cos(b), bottom,
                    radius * Mathf.Sin(b)));
        }

        public void Annulus(int submesh, float inner, float outer, float z,
            int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float b = (i + 1) * Mathf.PI * 2f / segments;
                // Front of target faces local -Z.
                Quad(submesh,
                    new Vector3(inner * Mathf.Cos(a),
                        inner * Mathf.Sin(a), z),
                    new Vector3(inner * Mathf.Cos(b),
                        inner * Mathf.Sin(b), z),
                    new Vector3(outer * Mathf.Cos(b),
                        outer * Mathf.Sin(b), z),
                    new Vector3(outer * Mathf.Cos(a),
                        outer * Mathf.Sin(a), z));
            }
        }

        public void FacePin(int submesh, float radius,
            float angle, float size, float z)
        {
            float x = radius * Mathf.Cos(angle);
            float y = radius * Mathf.Sin(angle);
            float half = size * .5f;
            // Two triangles facing -Z, just ahead of the rim.
            Quad(submesh,
                new Vector3(x - half, y - half, z),
                new Vector3(x - half, y + half, z),
                new Vector3(x + half, y + half, z),
                new Vector3(x + half, y - half, z));
        }

        public void BoxBeam(int submesh, Vector3 from, Vector3 to, float width)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < .000001f) return;
            Vector3 middle = (from + to) * .5f;
            Quaternion rotation = Quaternion.FromToRotation(
                Vector3.up, delta.normalized);
            float half = width * .5f;
            float lengthHalf = delta.magnitude * .5f;
            Vector3[] p = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                float x = (i & 1) == 0 ? -half : half;
                float y = (i & 2) == 0 ? -lengthHalf : lengthHalf;
                float z = (i & 4) == 0 ? -half : half;
                p[i] = middle + rotation * new Vector3(x, y, z);
            }
            Quad(submesh, p[0], p[4], p[6], p[2]);
            Quad(submesh, p[5], p[1], p[3], p[7]);
            Quad(submesh, p[0], p[1], p[5], p[4]);
            Quad(submesh, p[2], p[6], p[7], p[3]);
            Quad(submesh, p[0], p[2], p[3], p[1]);
            Quad(submesh, p[4], p[5], p[7], p[6]);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(_vertices);
            mesh.subMeshCount = _indices.Length;
            for (int i = 0; i < _indices.Length; i++)
                mesh.SetTriangles(_indices[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Visual-only pass over the committed v5 landscape. Runs once when the scene
// loads, with no Editor menu, scene regeneration, or per-frame updates.
// Uses existing scene materials; preserves all target, gameplay and camera data.
public static class JemparinganLandscapePolish
{
    private const string PlayableSceneName = "JemparinganPlayableEnvironment_v05";
    private const string ScenicSceneName = "JemparinganEnvironmentPreview_v01";
    private const string LandscapeName = "03 | Rice fields, bamboo and trees";
    private const string HorizonName = "04 | Distant mountains and clouds";
    public const string FieldDetailName = "F | Landscape polish v1 - rice and leaves";
    public const string HorizonDetailName = "E | Landscape polish v1 - mountain facets";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        // Safe for repeated Play Mode entries when domain reload is disabled.
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
        Apply(SceneManager.GetActiveScene());
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply(scene);
    }

    private static void Apply(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded ||
            (scene.name != PlayableSceneName && scene.name != ScenicSceneName))
            return;

        Transform landscape = FindRoot(scene, LandscapeName);
        Transform horizon = FindRoot(scene, HorizonName);
        if (landscape == null || horizon == null ||
            landscape.Find(FieldDetailName) != null ||
            horizon.Find(HorizonDetailName) != null)
            return;

        // One-time allocation, never run by a MonoBehaviour Update method.
        var fieldGroup = new GameObject(FieldDetailName);
        fieldGroup.transform.SetParent(landscape, false);
        var fieldLifetime = fieldGroup.AddComponent<JemparinganLandscapeMeshLifetime>();

        var ridgeGroup = new GameObject(HorizonDetailName);
        ridgeGroup.transform.SetParent(horizon, false);
        var ridgeLifetime = ridgeGroup.AddComponent<JemparinganLandscapeMeshLifetime>();

        int rice = AddRiceBlades(landscape, fieldGroup.transform, fieldLifetime);
        int leaves = AddLeafAccents(landscape, fieldGroup.transform, fieldLifetime);
        int peaks = AddMountainFacets(horizon, ridgeGroup.transform, ridgeLifetime);

        Debug.Log("Landscape polish v1: " + rice + " rice clumps, " +
            leaves + " foliage accents, " + peaks + " distant mountain facets; " +
            "visual-only, static combined meshes, no extra colliders or lights.");
    }

    private static Transform FindRoot(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name) return go.transform;
        return null;
    }

    private static int AddRiceBlades(Transform landscape, Transform parent,
        JemparinganLandscapeMeshLifetime lifetime)
    {
        Transform fields = landscape.Find("A | Irregular sawah paddy terraces");
        if (fields == null) return 0;

        // One combined mesh per existing rice material (normally three renderers).
        var batches = new Dictionary<Material, SurfaceMesh>();
        int clumps = 0;
        int plotIndex = 0;
        foreach (Transform plot in fields)
        {
            Transform field = plot.Find("Rice field");
            if (field == null) continue;
            MeshRenderer source = field.GetComponent<MeshRenderer>();
            if (source == null || source.sharedMaterial == null) continue;
            SurfaceMesh batch;
            if (!batches.TryGetValue(source.sharedMaterial, out batch))
            {
                batch = new SurfaceMesh();
                batches.Add(source.sharedMaterial, batch);
            }

            // All original paddy plots are unrotated boxes. Leave a generous
            // margin from bunds and both sides of the shooting corridor.
            Vector3 center = landscape.InverseTransformPoint(field.position);
            float width = field.lossyScale.x;
            float length = field.lossyScale.z;
            float top = center.y + field.lossyScale.y * .5f;
            for (int rz = 0; rz < 3; rz++)
            for (int cx = 0; cx < 4; cx++)
            {
                float phase = (plotIndex * 0.73f + rz * 2.19f + cx * 1.37f);
                float jitterX = .12f * Mathf.Sin(phase * 1.7f);
                float jitterZ = .12f * Mathf.Cos(phase * 1.3f);
                float x = center.x + ((cx + .5f) / 4f - .5f) *
                    (width - .65f) + jitterX;
                float z = center.z + ((rz + .5f) / 3f - .5f) *
                    (length - .65f) + jitterZ;
                Vector3 bottom = new Vector3(x, top + .013f, z);
                float height = .23f + .055f * (1f + Mathf.Sin(phase));
                float angle = .36f * Mathf.Sin(phase * 1.11f);
                AddCrossedRice(batch, bottom, height, angle);
                clumps++;
            }
            plotIndex++;
        }

        foreach (KeyValuePair<Material, SurfaceMesh> entry in batches)
            AddRenderer("Batched rice blades | " + entry.Key.name,
                parent, entry.Value, entry.Key, lifetime);
        return clumps;
    }

    private static void AddCrossedRice(SurfaceMesh batch, Vector3 basePosition,
        float height, float angle)
    {
        for (int blade = 0; blade < 2; blade++)
        {
            float theta = angle + blade * Mathf.PI * .5f;
            Vector3 across = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));
            Vector3 leaning = new Vector3(Mathf.Sin(theta), 0f,
                -Mathf.Cos(theta)) * (.055f + .018f * blade);
            float width = .047f + blade * .011f;
            Vector3 up = basePosition + Vector3.up * (height * (1f - blade * .12f)) +
                leaning;
            batch.Quad(basePosition - across * width,
                basePosition + across * width,
                up + across * .018f,
                up - across * .018f);
        }
    }

    private static int AddLeafAccents(Transform landscape, Transform parent,
        JemparinganLandscapeMeshLifetime lifetime)
    {
        Transform trees = landscape.Find("D | Tropical tree clusters");
        Transform fence = landscape.Find("C | Bamboo boundary");
        if (trees == null || fence == null) return 0;

        Material leafMaterial = null;
        var mesh = new SurfaceMesh();
        int accents = 0;
        int treeIndex = 0;
        foreach (Transform tree in trees)
        {
            Transform crown = tree.Find("Crown center");
            if (crown == null) continue;
            if (leafMaterial == null)
            {
                MeshRenderer source = crown.GetComponent<MeshRenderer>();
                if (source != null) leafMaterial = source.sharedMaterial;
            }
            Vector3 center = landscape.InverseTransformPoint(crown.position);
            for (int i = 0; i < 9; i++)
            {
                float theta = (i + treeIndex * .33f) * (Mathf.PI * 2f / 9f);
                Vector3 direction = new Vector3(Mathf.Cos(theta),
                    .20f + .13f * Mathf.Sin(i * 2.3f),
                    Mathf.Sin(theta));
                Vector3 origin = center + new Vector3(
                    direction.x * 1.03f, -.12f + (i % 3) * .32f,
                    direction.z * 1.03f);
                Leaf(mesh, origin, direction, .55f, .18f);
                accents++;
            }
            treeIndex++;
        }

        int postIndex = 0;
        foreach (Transform post in fence)
        {
            if (post.name != "Bamboo fence post") continue;
            if (postIndex++ % 2 != 0) continue;
            Vector3 center = landscape.InverseTransformPoint(post.position);
            float side = Mathf.Sign(center.x);
            // Fan away from the target lane: all details remain at |x| > 4.5m.
            for (int i = 0; i < 3; i++)
            {
                Vector3 direction = new Vector3(side * (.70f + .12f * i),
                    .35f, (i - 1) * .65f);
                Leaf(mesh, center + new Vector3(side * .15f,
                    .42f + i * .16f, 0f), direction, .51f, .14f);
                accents++;
            }
        }

        if (leafMaterial != null)
            AddRenderer("Batched tropical leaves and bamboo sprigs",
                parent, mesh, leafMaterial, lifetime);
        return accents;
    }

    private static void Leaf(SurfaceMesh mesh, Vector3 origin,
        Vector3 direction, float length, float halfWidth)
    {
        Vector3 outward = direction.normalized;
        Vector3 sideways = new Vector3(-outward.z, 0f, outward.x).normalized;
        Vector3 tip = origin + outward * length;
        mesh.Triangle(origin - sideways * halfWidth,
            origin + sideways * halfWidth, tip);
    }

    private static int AddMountainFacets(Transform horizon, Transform parent,
        JemparinganLandscapeMeshLifetime lifetime)
    {
        Transform ridge = horizon.Find("Middle blue mountain ridge");
        Transform far = horizon.Find("Far mountain ridge");
        if (ridge == null || far == null) return 0;
        MeshFilter filter = ridge.GetComponent<MeshFilter>();
        MeshRenderer highlight = far.GetComponent<MeshRenderer>();
        if (filter == null || filter.sharedMesh == null ||
            highlight == null || highlight.sharedMaterial == null)
            return 0;

        Vector3[] original = filter.sharedMesh.vertices;
        var mesh = new SurfaceMesh();
        int patches = 0;
        // Read existing ridge crest rather than regenerate it or alter its asset.
        // Thin, broken facets sit just forward of its surface and add depth.
        for (int segment = 4; segment + 4 < original.Length / 2; segment += 6)
        {
            Vector3 a = original[segment * 2 + 1] + ridge.localPosition;
            Vector3 b = original[(segment + 4) * 2 + 1] + ridge.localPosition;
            a.z -= .12f;
            b.z -= .12f;
            Vector3 low = Vector3.Lerp(a, b, .58f) + Vector3.down *
                (1.5f + .35f * Mathf.Sin(segment * .8f));
            mesh.Triangle(a, b, low);
            patches++;
        }
        AddRenderer("Distant blue ridge | broken pale rock facets",
            parent, mesh, highlight.sharedMaterial, lifetime);
        return patches;
    }

    private static void AddRenderer(string name, Transform parent,
        SurfaceMesh batch, Material material,
        JemparinganLandscapeMeshLifetime lifetime)
    {
        if (batch.TriangleCount == 0 || material == null) return;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Mesh mesh = batch.ToMesh(name);
        lifetime.Own(mesh);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // Every face is two-sided without requiring a new shader/material.
    // Reverse-face vertices are separate so their normals do not cancel out.
    private sealed class SurfaceMesh
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();
        public int TriangleCount { get { return triangles.Count / 3; } }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Triangle(a, b, c);
            Triangle(a, c, d);
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(a);
            vertices.Add(c);
            vertices.Add(b);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
            triangles.Add(start + 4);
            triangles.Add(start + 5);
        }

        public Mesh ToMesh(string name)
        {
            Mesh mesh = new Mesh { name = name };
            // Rice has ~23k vertices; ordinary 16-bit indices are sufficient.
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

// Runtime-built meshes are released when their scene objects are unloaded.
[DisallowMultipleComponent]
public sealed class JemparinganLandscapeMeshLifetime : MonoBehaviour
{
    private readonly List<Mesh> ownedMeshes = new List<Mesh>();

    public void Own(Mesh mesh)
    {
        if (mesh != null) ownedMeshes.Add(mesh);
    }

    private void OnDestroy()
    {
        foreach (Mesh mesh in ownedMeshes)
            if (mesh != null) Destroy(mesh);
        ownedMeshes.Clear();
    }
}

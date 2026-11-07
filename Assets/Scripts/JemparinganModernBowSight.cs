using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Modern desktop only: a modest bow-mounted optical sight, not a HUD overlay.
// A translucent glass disc carries a fine "+" reticle in the aperture.
// It is sighted in once at settled full draw and then remains fixed to bow.
// This visual has no effect on projectile ballistics, input, or scoring.
public sealed class JemparinganModernBowSight : MonoBehaviour
{
    // 30% larger than the previous .012m aperture, not a screen-sized scope.
    private const int RimSegments = 24;
    private const float RimRadius = .0156f;
    private const float SightForwardOffset = .055f;
    private static readonly Vector3 RestingPin = new Vector3(
        -.085f, .185f, SightForwardOffset);

    private Transform _sightRoot;
    private Transform _aperture;
    private Transform _supportRod;
    private Transform _mount;
    private Mesh _lensMesh;
    private Mesh _metalMesh;
    private Material _lensMaterial;
    private bool _hasCalibration;

    public void Build()
    {
        if (_sightRoot != null) return;

        _sightRoot = new GameObject(
            "Modern | compact glass sight fixed to bow riser").transform;
        _sightRoot.SetParent(transform, false);

        _mount = JemparinganArt.Part("Sight bracket on bow riser",
            PrimitiveType.Cube, _sightRoot,
            new Vector3(0f, .115f, .007f),
            new Vector3(.013f, .026f, .014f),
            JemparinganArt.Tone.BowBrass).transform;

        _supportRod = JemparinganArt.Part("Single slim sight support",
            PrimitiveType.Cylinder, _sightRoot,
            Vector3.zero, Vector3.one * .01f,
            JemparinganArt.Tone.BowEdge).transform;

        _aperture = new GameObject(
            "Optical aperture | blue glass and plus reticle").transform;
        _aperture.SetParent(_sightRoot, false);
        _aperture.localRotation = Quaternion.identity;
        _aperture.localPosition = RestingPin;

        // Previously 24 individual cylinders formed the rim and two more
        // the reticle. Build one lightweight mesh with two shared material
        // slots instead: a clean brass ring and fine dark crosshair.
        // The reticle center, aperture transform and calibration stay fixed.
        BuildMetalRimAndReticle();
        BuildGlassLens();

        // A tiny contrasting center bead remains a single reusable visual.
        JemparinganArt.Part("Small central reticle bead", PrimitiveType.Sphere,
            _aperture, new Vector3(0f, 0f, -.0045f),
            Vector3.one * .0017f, JemparinganArt.Tone.Scarlet);

        PlaceSupport();
        _sightRoot.gameObject.SetActive(false);
    }

    // Submeshes keep the same existing brass and dark-wood URP materials.
    // A shallow solid rim catches light without a forest of separate
    // cylinder renderers. The glass stays behind the viewer-facing plus.
    private void BuildMetalRimAndReticle()
    {
        const float innerRadius = RimRadius - .0010f;
        const float outerRadius = RimRadius + .0010f;
        const float frontZ = -.0013f;
        const float backZ = .0015f;
        const float reticleZ = -.004f;
        const float reticleLength = .0108f;
        const float reticleHalfWidth = .00048f;

        var vertices = new List<Vector3>(RimSegments * 12 + 8);
        var rimTriangles = new List<int>(RimSegments * 18);
        var crossTriangles = new List<int>(12);
        for (int i = 0; i < RimSegments; i++)
        {
            float a = Mathf.PI * 2f * i / RimSegments;
            float b = Mathf.PI * 2f * (i + 1) / RimSegments;
            float cosA = Mathf.Cos(a), sinA = Mathf.Sin(a);
            float cosB = Mathf.Cos(b), sinB = Mathf.Sin(b);
            Vector3 fiA = new Vector3(
                cosA * innerRadius, sinA * innerRadius, frontZ);
            Vector3 fiB = new Vector3(
                cosB * innerRadius, sinB * innerRadius, frontZ);
            Vector3 foA = new Vector3(
                cosA * outerRadius, sinA * outerRadius, frontZ);
            Vector3 foB = new Vector3(
                cosB * outerRadius, sinB * outerRadius, frontZ);
            Vector3 boA = new Vector3(
                cosA * outerRadius, sinA * outerRadius, backZ);
            Vector3 boB = new Vector3(
                cosB * outerRadius, sinB * outerRadius, backZ);
            Vector3 biA = new Vector3(
                cosA * innerRadius, sinA * innerRadius, backZ);
            Vector3 biB = new Vector3(
                cosB * innerRadius, sinB * innerRadius, backZ);
            // Front faces toward the shooter (-Z), with a back and
            // outward edge for depth when the bow tilts.
            AddQuad(vertices, rimTriangles, fiA, fiB, foB, foA);
            AddQuad(vertices, rimTriangles, boA, boB, biB, biA);
            AddQuad(vertices, rimTriangles, foA, foB, boB, boA);
        }

        AddQuad(vertices, crossTriangles,
            new Vector3(-reticleLength, -reticleHalfWidth, reticleZ),
            new Vector3(-reticleLength, reticleHalfWidth, reticleZ),
            new Vector3(reticleLength, reticleHalfWidth, reticleZ),
            new Vector3(reticleLength, -reticleHalfWidth, reticleZ));
        AddQuad(vertices, crossTriangles,
            new Vector3(-reticleHalfWidth, -reticleLength, reticleZ),
            new Vector3(-reticleHalfWidth, reticleLength, reticleZ),
            new Vector3(reticleHalfWidth, reticleLength, reticleZ),
            new Vector3(reticleHalfWidth, -reticleLength, reticleZ));

        _metalMesh = new Mesh
            { name = "Modern sight | batched brass rim and fine plus" };
        _metalMesh.SetVertices(vertices);
        _metalMesh.subMeshCount = 2;
        _metalMesh.SetTriangles(rimTriangles, 0);
        _metalMesh.SetTriangles(crossTriangles, 1);
        _metalMesh.RecalculateNormals();
        _metalMesh.RecalculateBounds();

        var group = new GameObject("Optical rim and plus | one renderer");
        group.transform.SetParent(_aperture, false);
        group.AddComponent<MeshFilter>().sharedMesh = _metalMesh;
        var renderer = group.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = new[]
        {
            JemparinganArt.Material(JemparinganArt.Tone.BowBrass),
            JemparinganArt.Material(JemparinganArt.Tone.DarkWood)
        };
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void AddQuad(List<Vector3> vertices, List<int> triangles,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int first = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        triangles.Add(first);
        triangles.Add(first + 1);
        triangles.Add(first + 2);
        triangles.Add(first);
        triangles.Add(first + 2);
        triangles.Add(first + 3);
    }

    private void BuildGlassLens()
    {
        // Circular mesh avoids visible square corners of a transparent quad.
        // The tint is intentionally very low opacity so targets remain visible.
        var lens = new GameObject("Transparent blue optical glass");
        lens.transform.SetParent(_aperture, false);
        lens.transform.localPosition = new Vector3(0f, 0f, .0006f);

        Vector3[] vertices = new Vector3[RimSegments + 2];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[RimSegments * 3];
        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(.5f, .5f);
        for (int i = 0; i <= RimSegments; i++)
        {
            float angle = Mathf.PI * 2f * i / RimSegments;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            vertices[i + 1] = new Vector3(x, y, 0f) * (RimRadius - .0015f);
            uv[i + 1] = new Vector2((x + 1f) * .5f, (y + 1f) * .5f);
            if (i < RimSegments)
            {
                int tri = i * 3;
                // Facing back (-Z), toward the player behind the bow.
                triangles[tri] = 0;
                triangles[tri + 1] = i + 2;
                triangles[tri + 2] = i + 1;
            }
        }
        for (int i = 0; i < normals.Length; i++)
            normals[i] = Vector3.back;

        _lensMesh = new Mesh { name = "Modern optical sight glass disc" };
        _lensMesh.vertices = vertices;
        _lensMesh.normals = normals;
        _lensMesh.uv = uv;
        _lensMesh.triangles = triangles;
        lens.AddComponent<MeshFilter>().sharedMesh = _lensMesh;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        bool urp = shader != null;
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("Modern sight: no transparent glass shader found.");
            return; // Ring and reticle are still usable without glass.
        }

        _lensMaterial = new Material(shader)
            { name = "Modern optical glass | translucent blue" };
        Color tint = new Color(.37f, .76f, .90f, .12f);
        if (_lensMaterial.HasProperty("_BaseColor"))
            _lensMaterial.SetColor("_BaseColor", tint);
        if (_lensMaterial.HasProperty("_Color"))
            _lensMaterial.SetColor("_Color", tint);

        // Both URP and Built-in Standard require explicit transparent blend.
        if (urp && _lensMaterial.HasProperty("_Surface"))
        {
            _lensMaterial.SetFloat("_Surface", 1f);
            if (_lensMaterial.HasProperty("_Blend"))
                _lensMaterial.SetFloat("_Blend", 0f);
            _lensMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _lensMaterial.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        }
        else if (_lensMaterial.HasProperty("_Mode"))
        {
            _lensMaterial.SetFloat("_Mode", 3f);
            _lensMaterial.EnableKeyword("_ALPHABLEND_ON");
        }
        if (_lensMaterial.HasProperty("_SrcBlend"))
            _lensMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (_lensMaterial.HasProperty("_DstBlend"))
            _lensMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (_lensMaterial.HasProperty("_ZWrite"))
            _lensMaterial.SetFloat("_ZWrite", 0f);
        if (_lensMaterial.HasProperty("_Cull"))
            _lensMaterial.SetFloat("_Cull", 0f);
        _lensMaterial.SetOverrideTag("RenderType", "Transparent");
        _lensMaterial.renderQueue = (int)RenderQueue.Transparent;

        var renderer = lens.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _lensMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // Only a previously calibrated, actively visible reticle can be used
    // as a trustworthy shot ray. Returns the actual camera-to-PLUS direction
    // AFTER the bow viewmodel has been positioned for this frame.
    public bool HasCalibration
    {
        get { return _hasCalibration; }
    }

    public bool TryGetCalibratedRay(Camera camera, out Vector3 rayDirection)
    {
        rayDirection = Vector3.forward;
        if (!_hasCalibration || _aperture == null || _sightRoot == null ||
            camera == null || !_sightRoot.gameObject.activeInHierarchy)
            return false;

        Vector3 fromEye = _aperture.position - camera.transform.position;
        float depth = Vector3.Dot(fromEye, camera.transform.forward);
        if (depth <= camera.nearClipPlane + .02f)
            return false;

        rayDirection = fromEye.normalized;
        return true;
    }

    public void ResetCalibration()
    {
        _hasCalibration = false;
        if (_aperture == null) return;
        _aperture.localPosition = RestingPin;
        _aperture.localRotation = Quaternion.identity;
        PlaceSupport();
    }

    public void Present(Camera camera, bool visible, bool calibrate)
    {
        if (_sightRoot == null) return;
        if (_sightRoot.gameObject.activeSelf != visible)
            _sightRoot.gameObject.SetActive(visible);
        if (!visible || !calibrate || _hasCalibration || camera == null)
            return;

        // Zero the physical sight ONCE. Find where the camera forward RAY
        // meets the fixed bow-local sight plane, not an approximate depth
        // followed by changing Z (which previously produced parallax error).
        Vector3 planePoint = transform.TransformPoint(
            new Vector3(0f, 0f, SightForwardOffset));
        Vector3 planeNormal = transform.TransformDirection(Vector3.forward);
        Ray cameraRay = new Ray(camera.transform.position,
            camera.transform.forward);
        float denominator = Vector3.Dot(cameraRay.direction, planeNormal);
        if (Mathf.Abs(denominator) < .0001f) return;
        float distance = Vector3.Dot(planePoint - cameraRay.origin,
            planeNormal) / denominator;
        if (distance <= camera.nearClipPlane + .02f) return;

        Vector3 localPoint = transform.InverseTransformPoint(
            cameraRay.GetPoint(distance));
        // Reject implausibly long mounting hardware in unusual camera rigs.
        if (localPoint.x < -.20f || localPoint.x > .04f ||
            localPoint.y < .10f || localPoint.y > .34f)
            return;

        _aperture.localPosition = localPoint;
        _hasCalibration = true;
        PlaceSupport();
    }

    private void PlaceSupport()
    {
        if (_supportRod == null || _mount == null || _aperture == null) return;
        Vector3 from = _mount.localPosition + new Vector3(0f, .012f, .006f);
        Vector3 to = _aperture.localPosition + Vector3.right * RimRadius;
        Vector3 delta = to - from;
        if (delta.sqrMagnitude < .000001f) return;
        _supportRod.localPosition = (from + to) * .5f;
        _supportRod.localRotation =
            Quaternion.FromToRotation(Vector3.up, delta.normalized);
        _supportRod.localScale = new Vector3(
            .0028f, delta.magnitude * .5f, .0028f);
    }

    private void OnDestroy()
    {
        if (_lensMesh != null) Destroy(_lensMesh);
        if (_metalMesh != null) Destroy(_metalMesh);
        if (_lensMaterial != null) Destroy(_lensMaterial);
    }
}

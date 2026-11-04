using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Traditional-only render geometry. No colliders, input, aim or projectile state.
// Contact frames are the real leather handle and the real string junction.
public sealed class JemparinganTraditionalArcher : MonoBehaviour
{
    private const int Sides = 8;
    private const float UpperLength = .36f, ForearmLength = .34f;
    private readonly List<Mesh> _meshes = new List<Mesh>();
    private Transform _handle, _nock, _bow, _chest, _arms, _leftHand, _rightHand;
    private Mesh _leftSleeve, _rightSleeve, _rightHandMesh;
    private Vector3[] _closedFingers, _openFingers, _posedFingers;
    private float _fingerOpenness;
    private readonly Vector3[] _leftVertices = new Vector3[7 * Sides + 2];
    private readonly Vector3[] _rightVertices = new Vector3[7 * Sides + 2];
    private Vector3 _releasedContact;
    private Quaternion _releasedRotation;
    private float _releaseTime = 1f, _torsoYaw;
    private bool _wasDrawing;
    private static readonly Vector3 LeftWrist = new Vector3(-.026f, -.012f, -.095f);
    private static readonly Vector3 RightWrist = new Vector3(.065f, -.015f, -.10f);

    public void Build(Transform handle, Transform nock, Transform bow)
    {
        _handle = handle;
        _nock = nock;
        _bow = bow;
        _chest = new GameObject("Traditional | shirt and shoulder frame").transform;
        _chest.SetParent(transform, false);
        // The eye is in front of the chest, rather than inside its front face.
        _chest.localPosition = new Vector3(0f, -.94f, -.12f);
        BuildShirt();
        _arms = new GameObject("Traditional | arms and hands").transform;
        _arms.SetParent(transform, false);
        _leftHand = BuildHand("Left hand | fingers around leather handle", false);
        _rightHand = BuildHand("Right hand | split fingers at string nock", true);
        _leftSleeve = MakeMesh("Continuous left sleeve", true);
        _rightSleeve = MakeMesh("Continuous right sleeve", true);
        AddRenderer("Left shoulder to wrist", _arms, _leftSleeve,
            JemparinganArt.Tone.TraditionalCloth);
        AddRenderer("Right shoulder to wrist", _arms, _rightSleeve,
            JemparinganArt.Tone.TraditionalCloth);
        _arms.gameObject.SetActive(false);
    }

    public void ResetPose()
    {
        _wasDrawing = false;
        _releaseTime = 1f;
        _fingerOpenness = 0f;
    }

    public void HideArms()
    {
        if (_arms != null) _arms.gameObject.SetActive(false);
        ResetPose();
    }

    public void Present(bool drawing)
    {
        if (_handle == null || _nock == null) return;
        if (!_arms.gameObject.activeSelf) _arms.gameObject.SetActive(true);
        // Only the upper torso turns. The seat and crossed legs never inherit
        // camera yaw/pitch. All IK points below use this one seat-local frame.
        Vector3 facing = transform.InverseTransformDirection(_bow.forward);
        float yaw = Mathf.Clamp(Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg,
            -50f, 50f);
        _torsoYaw = Mathf.LerpAngle(_torsoYaw, yaw,
            1f - Mathf.Exp(-10f * Time.deltaTime));
        _chest.localRotation = Quaternion.Euler(0f, _torsoYaw, 0f);

        // Grip in the lower half of the *actual* 15 cm leather wrap. This is
        // a point on the handle, not a camera-space approximation of its origin.
        Vector3 leftContact = _handle.TransformPoint(Vector3.down * .55f);
        _leftHand.SetPositionAndRotation(leftContact, _handle.rotation);

        Vector3 contact = _nock.position;
        Quaternion rotation = _bow.rotation;
        if (_wasDrawing && !drawing)
        {
            _releaseTime = 0f;
            _releasedContact = _rightHand.localPosition;
            _releasedRotation = _rightHand.localRotation;
        }
        if (!drawing && _releaseTime < .32f)
        {
            _releaseTime += Time.deltaTime;
            // The fingers let go; the string can spring forward immediately
            // without dragging the hand with it. Brief follow-through, then
            // an eased return to the current nock (also while turning).
            Vector3 back = transform.InverseTransformDirection(-_bow.forward) * .035f;
            Vector3 follow = _releasedContact + back *
                Mathf.Sin(Mathf.Clamp01(_releaseTime / .32f) * Mathf.PI);
            float t = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(.08f, .32f, _releaseTime));
            contact = transform.TransformPoint(Vector3.Lerp(follow,
                transform.InverseTransformPoint(_nock.position), t));
            rotation = transform.rotation * Quaternion.Slerp(_releasedRotation,
                Quaternion.Inverse(transform.rotation) * _bow.rotation, t);
        }
        _rightHand.SetPositionAndRotation(contact, rotation);
        float open = !drawing && _releaseTime < .26f ? 1f : 0f;
        _fingerOpenness = Mathf.MoveTowards(_fingerOpenness, open, Time.deltaTime * 18f);
        for (int i = 0; i < _posedFingers.Length; i++)
            _posedFingers[i] = Vector3.Lerp(_closedFingers[i], _openFingers[i], _fingerOpenness);
        _rightHandMesh.vertices = _posedFingers;
        _rightHandMesh.RecalculateNormals();
        _rightHandMesh.RecalculateBounds();
        _wasDrawing = drawing;

        Vector3 leftShoulder = transform.InverseTransformPoint(
            _chest.TransformPoint(new Vector3(-.20f, .60f, .17f)));
        Vector3 rightShoulder = transform.InverseTransformPoint(
            _chest.TransformPoint(new Vector3(.20f, .60f, .17f)));
        Vector3 leftWrist = transform.InverseTransformPoint(_leftHand.TransformPoint(LeftWrist));
        Vector3 rightWrist = transform.InverseTransformPoint(_rightHand.TransformPoint(RightWrist));
        Vector3 leftBend = transform.InverseTransformDirection(
            _chest.TransformDirection(new Vector3(-.7f, -.8f, -.1f)));
        Vector3 rightBend = transform.InverseTransformDirection(
            _chest.TransformDirection(new Vector3(.8f, -.6f, -.15f)));
        PoseSleeve(_leftSleeve, _leftVertices, leftShoulder, leftWrist, leftBend,
            transform.InverseTransformDirection(_leftHand.TransformDirection(new Vector3(0f, .012f, .065f))));
        PoseSleeve(_rightSleeve, _rightVertices, rightShoulder, rightWrist, rightBend,
            transform.InverseTransformDirection(_rightHand.TransformDirection(new Vector3(-.031f, .003f, .058f))));
    }

    private void PoseSleeve(Mesh mesh, Vector3[] vertices,
        Vector3 shoulder, Vector3 wrist, Vector3 bendHint, Vector3 wristDirection)
    {
        Vector3 delta = wrist - shoulder;
        float distance = delta.magnitude;
        Vector3 axis = delta.normalized;
        // Small scapular protraction keeps the chain reachable at the edges
        // of the desktop view, without stretching either bone or moving a hand.
        if (distance > UpperLength + ForearmLength - .015f)
        {
            shoulder += axis * (distance - (UpperLength + ForearmLength - .015f));
            delta = wrist - shoulder;
            distance = delta.magnitude;
        }
        float d = Mathf.Clamp(distance, .025f, UpperLength + ForearmLength - .001f);
        float along = (UpperLength * UpperLength - ForearmLength * ForearmLength + d * d) / (2f * d);
        float height = Mathf.Sqrt(Mathf.Max(0f, UpperLength * UpperLength - along * along));
        Vector3 bend = Vector3.ProjectOnPlane(bendHint, axis).normalized;
        if (bend.sqrMagnitude < .01f) bend = Vector3.ProjectOnPlane(Vector3.down, axis).normalized;
        Vector3 elbow = shoulder + axis * along + bend * height;
        Vector3 upper = (elbow - shoulder).normalized;
        Vector3 lower = (wrist - elbow).normalized;
        for (int ring = 0; ring < 7; ring++)
        {
            Vector3 point, direction;
            float radius;
            if (ring < 3)
            {
                float t = ring / 3f;
                point = Vector3.Lerp(shoulder, elbow, t);
                direction = upper;
                radius = Mathf.Lerp(.070f, .050f, t);
            }
            else
            {
                float t = (ring - 3) / 3f;
                point = Vector3.Lerp(elbow, wrist, t);
                direction = ring == 3 ? (upper + lower).normalized : lower;
                radius = Mathf.Lerp(.050f, .026f, t);
                if (ring == 6) direction = wristDirection.normalized;
            }
            Vector3 across = Vector3.Cross(direction, bend).normalized;
            Vector3 normal = Vector3.Cross(direction, across).normalized;
            for (int side = 0; side < Sides; side++)
            {
                float a = side * Mathf.PI * 2f / Sides;
                vertices[ring * Sides + side] = point + radius *
                    (across * Mathf.Cos(a) + normal * Mathf.Sin(a));
            }
        }
        vertices[7 * Sides] = shoulder;
        vertices[7 * Sides + 1] = wrist;
        mesh.vertices = vertices;
        if (mesh.GetIndexCount(0) == 0) mesh.triangles = TubeIndices(7);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void BuildShirt()
    {
        var vertices = new Vector3[6 * Sides + 2];
        float[] levels = { 0f, .12f, .36f, .53f, .62f, .67f };
        float[] widths = { .17f, .18f, .22f, .235f, .20f, .085f };
        float[] depths = { .105f, .12f, .135f, .13f, .11f, .075f };
        for (int r = 0; r < 6; r++)
        for (int s = 0; s < Sides; s++)
        {
            float a = s * Mathf.PI * 2f / Sides;
            vertices[r * Sides + s] = new Vector3(Mathf.Cos(a) * widths[r],
                levels[r], levels[r] * .22f + Mathf.Sin(a) * depths[r]);
        }
        vertices[6 * Sides] = Vector3.zero;
        vertices[6 * Sides + 1] = new Vector3(0f, .67f, .1474f);
        Mesh mesh = MakeMesh("Shaped traditional shirt", false);
        mesh.vertices = vertices;
        mesh.triangles = TubeIndices(6);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AddRenderer("Traditional shirt", _chest, mesh, JemparinganArt.Tone.TraditionalCloth);
    }

    private Transform BuildHand(string name, bool right)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        HandGeometry(right, false, vertices, triangles);
        Mesh mesh = MakeMesh(name, right);
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        if (right)
        {
            _rightHandMesh = mesh;
            _closedFingers = vertices.ToArray();
            _posedFingers = new Vector3[_closedFingers.Length];
            vertices.Clear(); triangles.Clear();
            HandGeometry(true, true, vertices, triangles);
            _openFingers = vertices.ToArray();
        }
        return AddRenderer(name, _arms, mesh, JemparinganArt.Tone.TraditionalSkin);
    }

    private static void HandGeometry(bool right, bool open, List<Vector3> vertices, List<int> triangles)
    {
        Vector3 wrist = right ? RightWrist : LeftWrist;
        Vector3 palm = right ? new Vector3(.034f, -.012f, -.042f) : new Vector3(-.026f, 0f, -.030f);
        AddTube(vertices, triangles, new[] { wrist, Vector3.Lerp(wrist, palm, .45f), palm },
            new[] { .025f, .027f, .031f }, .65f);
        for (int finger = 0; finger < 4; finger++)
        {
            // Index above the arrow; middle/ring below it. Leave an actual
            // gap for the shaft rather than curling a finger through the nock.
            float y = right ? (finger == 0 ? .022f : -.020f - (finger - 1) * .019f)
                : .021f - finger * .017f;
            Vector3[] path = right
                ? new[] { new Vector3(.035f, y, -.038f), new Vector3(.012f, y, -.015f),
                    new Vector3(-.010f, y, -.010f), new Vector3(-.013f, y, .007f), new Vector3(.002f, y, .010f) }
                : new[] { new Vector3(-.032f, y, -.022f), new Vector3(-.006f, y, -.026f),
                    new Vector3(.023f, y, -.017f), new Vector3(.026f, y, .008f), new Vector3(.008f, y, .022f) };
            if (right && open)
                path = new[] { new Vector3(.035f, y, -.038f), new Vector3(.012f, y, -.022f),
                    new Vector3(-.012f, y, -.015f), new Vector3(-.035f, y, -.011f), new Vector3(-.047f, y, -.008f) };
            AddTube(vertices, triangles, path, new[] { .009f, .009f, .0085f, .008f, .006f }, 1f);
        }
        AddTube(vertices, triangles, right
            ? new[] { palm + Vector3.down * .023f, new Vector3(.020f, -.035f, -.015f), new Vector3(.006f, -.018f, -.008f) }
            : new[] { palm + Vector3.down * .028f, new Vector3(-.027f, -.035f, .003f), new Vector3(-.005f, -.023f, .021f) },
            new[] { .013f, .011f, .008f }, 1f);
    }

    private static void AddTube(List<Vector3> vertices, List<int> triangles,
        Vector3[] path, float[] radii, float flatten)
    {
        int start = vertices.Count;
        for (int r = 0; r < path.Length; r++)
        {
            Vector3 tangent = (path[Mathf.Min(r + 1, path.Length - 1)] - path[Mathf.Max(0, r - 1)]).normalized;
            Vector3 across = Vector3.Cross(tangent, Vector3.up).normalized;
            if (across.sqrMagnitude < .01f) across = Vector3.right;
            Vector3 normal = Vector3.Cross(tangent, across).normalized;
            for (int s = 0; s < Sides; s++)
            {
                float a = s * Mathf.PI * 2f / Sides;
                vertices.Add(path[r] + radii[r] *
                    (across * Mathf.Cos(a) * Mathf.Lerp(1f, flatten, (float)r / (path.Length - 1)) + normal * Mathf.Sin(a)));
            }
        }
        vertices.Add(path[0]); vertices.Add(path[path.Length - 1]);
        foreach (int index in TubeIndices(path.Length)) triangles.Add(start + index);
    }

    private static int[] TubeIndices(int rings)
    {
        var indices = new List<int>((rings - 1) * Sides * 6 + Sides * 6);
        for (int r = 0; r < rings - 1; r++)
        for (int s = 0; s < Sides; s++)
        {
            int a = r * Sides + s, b = r * Sides + (s + 1) % Sides;
            int c = a + Sides, d = b + Sides;
            indices.Add(a); indices.Add(b); indices.Add(c);
            indices.Add(b); indices.Add(d); indices.Add(c);
        }
        for (int s = 0; s < Sides; s++)
        {
            int next = (s + 1) % Sides;
            indices.Add(rings * Sides); indices.Add(next); indices.Add(s);
            indices.Add(rings * Sides + 1); indices.Add((rings - 1) * Sides + s);
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

    private static Transform AddRenderer(string name, Transform parent, Mesh mesh, JemparinganArt.Tone tone)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = JemparinganArt.Material(tone);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return go.transform;
    }

    private void OnDestroy()
    {
        foreach (Mesh mesh in _meshes) if (mesh != null) Destroy(mesh);
    }
}

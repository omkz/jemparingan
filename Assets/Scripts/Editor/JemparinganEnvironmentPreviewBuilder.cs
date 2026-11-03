#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Additive, editor-only environment blockout. Does not reference or alter gameplay.
// Generate the scene explicitly from Tools > Jemparingan > Environment.
public static class JemparinganEnvironmentPreviewBuilder
{
    private const string ScenePath = "Assets/Jemparingan/Scenes/JemparinganEnvironmentPreview_v01.unity";
    private const string AssetRoot = "Assets/Jemparingan/EnvironmentPreview";
    private const string MaterialFolder = AssetRoot + "/Materials";
    private const string MeshFolder = AssetRoot + "/Meshes";
    private const string TargetName = "Preview Bandulan | movable target";
    // A little deeper inside the pavilion makes the overhead joglo/eaves visible
    // while still preserving a clear view of the bandulan and distant landscape.
    private const float SeatZ = 1.25f;
    private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

    [MenuItem("Tools/Jemparingan/Environment/Create Joglo Preview v2")]
    public static void CreateSafePreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before creating the Jemparingan environment preview.");
            return;
        }

        // Never discard unsaved changes to a currently open scene.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog(
                "Preview scene already exists",
                "Only the separate environment preview scene will be overwritten. Your original Jemparingan gameplay scene will not be touched. Overwrite the preview?",
                "Overwrite preview", "Cancel"))
            return;

        EnsureFolder("Assets/Jemparingan");
        EnsureFolder("Assets/Jemparingan/Scenes");
        EnsureFolder(AssetRoot);
        EnsureFolder(MaterialFolder);
        EnsureFolder(MeshFolder);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            Materials.Clear();
            ConfigureAtmosphere();
            CreateCamera();
            CreateLight();

            var pendopo = Group("01 | Joglo pendopo - saka guru, tumpang sari, brunjung", null);
            BuildPendopo(pendopo);

            var courtyard = Group("02 | Archery courtyard and 30m lane", null);
            BuildCourtyard(courtyard);

            var target = Group(TargetName, null);
            target.position = new Vector3(0f, 0f, SeatZ + 10f);
            BuildBandulan(target);

            var landscape = Group("03 | Rice fields, bamboo and trees", null);
            BuildLandscape(landscape);

            var horizon = Group("04 | Distant mountains and clouds", null);
            BuildHorizon(horizon);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Unity could not save the preview scene.");

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = GameObject.Find("Player viewpoint | seated 1.35m");
            Debug.Log("Jemparingan Joglo v2 environment preview created at " + ScenePath +
                ". Open Game view or Play Mode; use Tools > Jemparingan > Environment > Set Target 10m/20m/30m. No gameplay scripts were changed.");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Debug.LogError("Preview generation failed. The original gameplay scene has not been edited.");
        }
    }

    [MenuItem("Tools/Jemparingan/Environment/Set Target 10m")]
    private static void Set10m() { SetTargetDistance(10f); }

    [MenuItem("Tools/Jemparingan/Environment/Set Target 20m")]
    private static void Set20m() { SetTargetDistance(20f); }

    [MenuItem("Tools/Jemparingan/Environment/Set Target 30m")]
    private static void Set30m() { SetTargetDistance(30f); }

    private static void SetTargetDistance(float distance)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogWarning("Open " + ScenePath + " first. Target controls only work in the separate preview scene.");
            return;
        }

        GameObject target = GameObject.Find(TargetName);
        if (target == null)
        {
            Debug.LogError("Preview bandulan not found. Regenerate the preview scene from the Environment menu.");
            return;
        }

        Undo.RecordObject(target.transform, "Set Jemparingan preview target distance");
        target.transform.position = new Vector3(0f, 0f, SeatZ + distance);
        EditorSceneManager.MarkSceneDirty(scene);
        SceneView.RepaintAll();
        Debug.Log("Preview bandulan set to " + distance + "m from the seated player (positive Z). Save the scene to keep this setting.");
    }

    private static void ConfigureAtmosphere()
    {
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.70f, 0.82f, 0.85f);
        RenderSettings.fogStartDistance = 65f;
        RenderSettings.fogEndDistance = 180f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.76f, 0.81f, 0.85f);
        RenderSettings.ambientEquatorColor = new Color(0.83f, 0.76f, 0.65f);
        RenderSettings.ambientGroundColor = new Color(0.35f, 0.39f, 0.31f);
        RenderSettings.ambientIntensity = 1.0f;
    }

    private static void CreateCamera()
    {
        var go = new GameObject("Player viewpoint | seated 1.35m");
        go.tag = "MainCamera";
        Camera camera = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.57f, 0.76f, 0.88f);
        // Editor-only composition preview: a wider vertical view includes the joglo roof.
        // The physical Quest field of view will be determined by the headset.
        camera.fieldOfView = 84f;
        camera.nearClipPlane = 0.04f;
        camera.farClipPlane = 220f;
        go.transform.position = new Vector3(0f, 1.35f, SeatZ);
        // Slight upward bias makes the timber roof frame the shot without hiding the target.
        go.transform.LookAt(new Vector3(0f, 1.95f, SeatZ + 25f));
    }

    private static void CreateLight()
    {
        var go = new GameObject("Sun | warm afternoon - preview only");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1.0f, 0.87f, 0.68f);
        light.intensity = 1.5f;
        // No realtime shadows during initial scene layout validation.
        light.shadows = LightShadows.None;
        go.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
    }

    // Joglo-inspired environment blockout: four central saka guru support a steep
    // brunjung roof; surrounding columns support the shallower penanggap roof.
    // The silhouette/geometry is intentionally simplified, not a heritage-accurate
    // reconstruction or a final Blender art asset.
    private static void BuildPendopo(Transform parent)
    {
        Material stone = Mat("Stone", 139, 137, 124);
        Material teak = Mat("Teak", 113, 72, 46);
        Material darkWood = Mat("Dark_Teak", 73, 44, 35);
        Material trim = Mat("Amber_Trim", 174, 127, 73);
        Material roof = Mat("Clay_Roof", 99, 58, 48);
        Material mat = Mat("Woven_Mat", 173, 142, 93);

        var floor = Group("A | Floor, umpak, front step", parent);
        Block("Stone base", floor, new Vector3(0f, -0.16f, 0f),
            new Vector3(8.35f, 0.35f, 8.35f), stone);
        Block("Teak floor", floor, new Vector3(0f, 0.07f, 0f),
            new Vector3(8f, 0.14f, 8f), teak);
        for (int i = -3; i <= 3; i++)
            Block("Floor plank seam " + i, floor, new Vector3(i, 0.15f, 0f),
                new Vector3(0.025f, 0.008f, 7.8f), darkWood);
        Block("Seated woven mat", floor, new Vector3(0f, 0.155f, SeatZ),
            new Vector3(1.4f, 0.025f, 1.15f), mat);
        Block("Low front step", floor, new Vector3(0f, -0.23f, 4.35f),
            new Vector3(5.6f, 0.21f, 0.75f), stone);

        var perimeter = Group("B | Outer penanggap columns", parent);
        foreach (float x in new[] {-3.45f, 3.45f})
        foreach (float z in new[] {-3.45f, 3.45f})
        {
            var column = Group("Outer saka | " + x + ", " + z, perimeter);
            column.localPosition = new Vector3(x, 0f, z);
            Block("Stone umpak", column, new Vector3(0f, 0.28f, 0f),
                new Vector3(0.65f, 0.44f, 0.65f), stone);
            // Rectangular wooden posts read better than plain cylinders.
            Block("Outer teak saka", column, new Vector3(0f, 2.05f, 0f),
                new Vector3(0.31f, 3.18f, 0.31f), teak);
            Block("Foot trim", column, new Vector3(0f, 0.53f, 0f),
                new Vector3(0.40f, 0.13f, 0.40f), darkWood);
            Block("Column capital", column, new Vector3(0f, 3.69f, 0f),
                new Vector3(0.56f, 0.21f, 0.56f), trim);
            Block("Column head", column, new Vector3(0f, 3.81f, 0f),
                new Vector3(0.66f, 0.11f, 0.66f), darkWood);
        }

        var core = Group("C | FOUR SAKA GURU - primary structure", parent);
        foreach (float x in new[] {-1.72f, 1.72f})
        foreach (float z in new[] {-1.72f, 1.72f})
        {
            var pillar = Group("Saka guru | " + x + ", " + z, core);
            pillar.localPosition = new Vector3(x, 0f, z);
            Block("Umpak carved stone placeholder", pillar, new Vector3(0f, 0.30f, 0f),
                new Vector3(0.64f, 0.43f, 0.64f), stone);
            Block("Saka guru teak post", pillar, new Vector3(0f, 2.38f, 0f),
                new Vector3(0.35f, 3.86f, 0.35f), darkWood);
            Block("Saka guru base collar", pillar, new Vector3(0f, 0.57f, 0f),
                new Vector3(0.46f, 0.16f, 0.46f), trim);
            Block("Saka guru head collar", pillar, new Vector3(0f, 4.23f, 0f),
                new Vector3(0.54f, 0.23f, 0.54f), trim);
            Block("Saka guru upper capital", pillar, new Vector3(0f, 4.40f, 0f),
                new Vector3(0.62f, 0.16f, 0.62f), darkWood);
        }

        var interior = Group("D | Blandar, sunduk and tumpang sari", parent);
        BuildTumpangSari(interior, darkWood, teak, trim);

        var roofStructure = Group("E | Rafters, sloped structure and fascia", parent);
        BuildRoofStructure(roofStructure, darkWood, teak, trim);

        var roofPanels = Group("F | Penanggap and steep brunjung roof", parent);
        // Penanggap: gentle lower slope. Brunjung: steeper high central roof.
        // RoofLayer uses an all-new v2 mesh asset so the old v1 asset stays intact.
        RoofLayer("Penanggap lower roof", roofPanels, 4.48f, 2.23f, 3.80f, 4.92f, roof);
        RoofLayer("Brunjung high roof", roofPanels, 2.28f, 0.68f, 4.96f, 6.62f, roof);
        Block("Molo | ridge cap", roofPanels, new Vector3(0f, 6.65f, 0f),
            new Vector3(1.55f, 0.13f, 0.28f), darkWood);
    }

    private static void BuildTumpangSari(Transform parent, Material darkWood,
        Material teak, Material trim)
    {
        // The four main columns are at (+/-1.72, +/-1.72). Their timber
        // crossbeams form the inner rectangular pamidangan above the player.
        foreach (float z in new[] {-1.72f, 1.72f})
        {
            Block("Blandar pamidangan X", parent, new Vector3(0f, 4.33f, z),
                new Vector3(3.85f, 0.27f, 0.23f), darkWood);
            Block("Sunduk lintel X", parent, new Vector3(0f, 3.93f, z),
                new Vector3(3.62f, 0.15f, 0.15f), teak);
        }
        foreach (float x in new[] {-1.72f, 1.72f})
        {
            Block("Blandar pamidangan Z", parent, new Vector3(x, 4.33f, 0f),
                new Vector3(0.23f, 0.27f, 3.85f), darkWood);
            Block("Sunduk lintel Z", parent, new Vector3(x, 3.93f, 0f),
                new Vector3(0.15f, 0.15f, 3.62f), teak);
        }

        // Stacked, progressively wider frames evoke tumpang sari when looking
        // up from the seated player. They do not fill/black out the roof opening.
        float[] halfSpans = {1.78f, 1.89f, 2.01f, 2.13f};
        for (int level = 0; level < halfSpans.Length; level++)
        {
            float half = halfSpans[level];
            float y = 4.48f + 0.14f * level;
            Material beam = level % 2 == 0 ? teak : darkWood;
            var ring = Group("Tumpang sari tier " + (level + 1), parent);
            foreach (float z in new[] {-half, half})
            {
                Block("Tumpang X", ring, new Vector3(0f, y, z),
                    new Vector3(2f * half + 0.15f, 0.13f, 0.14f), beam);
                Block("Gilded slim detail X", ring, new Vector3(0f, y - 0.071f, z),
                    new Vector3(2f * half + 0.08f, 0.024f, 0.075f), trim);
            }
            foreach (float x in new[] {-half, half})
            {
                Block("Tumpang Z", ring, new Vector3(x, y, 0f),
                    new Vector3(0.14f, 0.13f, 2f * half + 0.15f), beam);
            }
        }
    }

    private static void BuildRoofStructure(Transform parent, Material darkWood,
        Material teak, Material trim)
    {
        // Open-sided outer ring: no wall or central beam obstructs the aiming line.
        foreach (float z in new[] {-3.45f, 3.45f})
            Block("Penanggap front/back blandar", parent, new Vector3(0f, 3.79f, z),
                new Vector3(7.62f, 0.24f, 0.24f), darkWood);
        foreach (float x in new[] {-3.45f, 3.45f})
            Block("Penanggap side blandar", parent, new Vector3(x, 3.79f, 0f),
                new Vector3(0.24f, 0.24f, 7.62f), darkWood);

        // Rafters follow both roof pitches. Keep modest count for Quest budgets.
        for (int i = -4; i <= 4; i++)
        {
            float offset = i * 0.88f;
            float topOffset = offset * 0.54f;
            foreach (float sign in new[] {-1f, 1f})
            {
                Beam("Penanggap front/back rafter", parent,
                    new Vector3(offset, 3.83f, sign * 4.30f),
                    new Vector3(topOffset, 4.87f, sign * 2.15f),
                    0.095f, 0.11f, teak);
                Beam("Penanggap side rafter", parent,
                    new Vector3(sign * 4.30f, 3.83f, offset),
                    new Vector3(sign * 2.15f, 4.87f, topOffset),
                    0.095f, 0.11f, teak);
            }
        }

        for (int i = -2; i <= 2; i++)
        {
            float offset = i * 0.75f;
            foreach (float sign in new[] {-1f, 1f})
            {
                Beam("Brunjung steep inner rafter", parent,
                    new Vector3(offset, 4.98f, sign * 2.20f),
                    new Vector3(offset * 0.30f, 6.58f, sign * 0.65f),
                    0.09f, 0.10f, darkWood);
            }
        }

        // The eave's underside is deliberately visible in the preview camera.
        Block("Front roof fascia", parent, new Vector3(0f, 3.66f, 4.25f),
            new Vector3(8.4f, 0.24f, 0.22f), darkWood);
        Block("Front roof fascia narrow gold trim", parent,
            new Vector3(0f, 3.50f, 4.25f),
            new Vector3(8.2f, 0.07f, 0.16f), trim);
        Block("Left side eave fascia", parent, new Vector3(-4.25f, 3.66f, 0f),
            new Vector3(0.22f, 0.24f, 8.4f), darkWood);
        Block("Right side eave fascia", parent, new Vector3(4.25f, 3.66f, 0f),
            new Vector3(0.22f, 0.24f, 8.4f), darkWood);
        Block("Rear eave fascia", parent, new Vector3(0f, 3.66f, -4.25f),
            new Vector3(8.4f, 0.24f, 0.22f), darkWood);
    }

    private static void BuildCourtyard(Transform parent)
    {
        Material grass = Mat("Grass", 115, 147, 82);
        Material path = Mat("Sand_Path", 183, 158, 112);
        Material edge = Mat("Path_Edge", 105, 114, 82);

        Block("Large green ground", parent, new Vector3(0f, -0.55f, 44f),
            new Vector3(190f, 1.05f, 180f), grass);
        Block("Clear archery lane", parent, new Vector3(0f, -0.007f, 20.5f),
            new Vector3(2.55f, 0.025f, 33f), path);
        foreach (float x in new[] {-1.48f, 1.48f})
            Block("Lane border", parent, new Vector3(x, 0.015f, 20.5f),
                new Vector3(0.12f, 0.05f, 33f), edge);

        // Distance markers sit outside the shooting lane.
        Material marker = Mat("Distance_Markers", 136, 93, 61);
        foreach (float distance in new[] {10f, 20f, 30f})
        {
            float z = SeatZ + distance;
            Block("Marker " + distance + "m left", parent, new Vector3(-2.2f, 0.08f, z),
                new Vector3(0.23f, 0.15f, 0.23f), marker);
            Block("Marker " + distance + "m right", parent, new Vector3(2.2f, 0.08f, z),
                new Vector3(0.23f, 0.15f, 0.23f), marker);
        }
    }

    private static void BuildBandulan(Transform parent)
    {
        Material bamboo = Mat("Bamboo", 152, 129, 76);
        Material red = Mat("Bandulan_Red", 166, 48, 48);
        Material white = Mat("Bandulan_White", 232, 220, 195);
        Material rope = Mat("Rope", 105, 78, 54);

        foreach (float x in new[] {-0.65f, 0.65f})
            Cylinder("Bandulan frame upright", parent, new Vector3(x, 1.28f, 0f),
                new Vector3(0.065f, 1.24f, 0.065f), bamboo);
        Block("Bandulan frame top", parent, new Vector3(0f, 2.48f, 0f),
            new Vector3(1.55f, 0.11f, 0.12f), bamboo);
        Cylinder("Suspension cord", parent, new Vector3(0f, 2.16f, 0f),
            new Vector3(0.012f, 0.29f, 0.012f), rope);
        Cylinder("Bandulan white lower body", parent, new Vector3(0f, 1.61f, 0f),
            new Vector3(0.06f, 0.28f, 0.06f), white);
        Cylinder("Bandulan red upper body", parent, new Vector3(0f, 1.95f, 0f),
            new Vector3(0.061f, 0.08f, 0.061f), red);
        Sphere("Bandulan red cap", parent, new Vector3(0f, 2.045f, 0f),
            new Vector3(0.13f, 0.11f, 0.13f), red);
    }

    private static void BuildLandscape(Transform parent)
    {
        Material riceA = Mat("Rice_Green", 104, 163, 70);
        Material riceB = Mat("Rice_Light", 146, 182, 91);
        Material bund = Mat("Paddy_Bund", 111, 117, 70);
        Material bamboo = Mat("Bamboo", 152, 129, 76);
        Material trunk = Mat("Tree_Trunk", 99, 78, 54);
        Material leavesA = Mat("Leaf_Green", 62, 122, 69);
        Material leavesB = Mat("Leaf_Light", 88, 151, 74);

        for (int row = 0; row < 12; row++)
        {
            float z = 24f + row * 5.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int lane = 0; lane < 5; lane++)
                {
                    float x = side * (8.5f + lane * 4.35f);
                    Block("Rice paddy field", parent, new Vector3(x, 0.012f, z),
                        new Vector3(3.55f, 0.035f, 4.15f),
                        ((row + lane) % 2 == 0) ? riceA : riceB);
                    Block("Paddy bund", parent, new Vector3(x, 0.02f, z + 2.20f),
                        new Vector3(3.8f, 0.05f, 0.28f), bund);
                }
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 5.2f;
            for (int row = 0; row <= 10; row++)
            {
                float z = 6f + row * 4.3f;
                Cylinder("Bamboo fence post", parent, new Vector3(x, 0.62f, z),
                    new Vector3(0.055f, 0.62f, 0.055f), bamboo);
            }

            foreach (float y in new[] {0.44f, 0.86f})
                Block("Bamboo fence rail", parent, new Vector3(x, y, 27.5f),
                    new Vector3(0.075f, 0.065f, 44f), bamboo);
        }

        var trees = new [] {
            new Vector3(-10f, 0f, 7f), new Vector3(10f, 0f, 12f),
            new Vector3(-12f, 0f, 22f), new Vector3(15f, 0f, 27f),
            new Vector3(-22f, 0f, 34f), new Vector3(24f, 0f, 41f),
            new Vector3(-32f, 0f, 58f), new Vector3(34f, 0f, 54f),
            new Vector3(-42f, 0f, 74f), new Vector3(45f, 0f, 80f)
        };
        for (int i = 0; i < trees.Length; i++)
        {
            var tree = Group("Tropical tree " + (i + 1), parent);
            tree.localPosition = trees[i];
            float height = 3.2f + (i % 4) * 0.55f;
            Cylinder("Trunk", tree, new Vector3(0f, height * .45f, 0f),
                new Vector3(.20f, height * .45f, .20f), trunk);
            Sphere("Leaf crown", tree, new Vector3(0f, height, 0f),
                new Vector3(2.0f + (i % 3) * .25f, 1.8f, 1.9f),
                i % 2 == 0 ? leavesA : leavesB);
            Sphere("Side leaves", tree, new Vector3(0.7f, height - 0.55f, 0.2f),
                new Vector3(1.5f, 1.15f, 1.6f), leavesA);
        }
    }

    private static void BuildHorizon(Transform parent)
    {
        Material farGreen = Mat("Far_Hills", 96, 130, 109);
        Material mountain = Mat("Blue_Mountain", 100, 134, 149);
        Material mountainLight = Mat("Far_Mountain", 137, 157, 162);
        Material cloud = Mat("Soft_Cloud", 224, 230, 224);
        Material glow = Mat("Warm_Horizon", 188, 182, 145);

        Sphere("Hill left", parent, new Vector3(-55f, 7f, 110f),
            new Vector3(75f, 25f, 30f), farGreen);
        Sphere("Hill right", parent, new Vector3(56f, 8f, 118f),
            new Vector3(85f, 27f, 36f), farGreen);
        Sphere("Mountain distant left", parent, new Vector3(-24f, 21f, 148f),
            new Vector3(70f, 58f, 40f), mountainLight);
        Sphere("Mountain center", parent, new Vector3(13f, 24f, 145f),
            new Vector3(60f, 69f, 42f), mountain);
        Sphere("Mountain distant right", parent, new Vector3(62f, 18f, 155f),
            new Vector3(90f, 53f, 46f), mountainLight);
        Sphere("Golden horizon tint", parent, new Vector3(-64f, 3f, 174f),
            new Vector3(55f, 16f, 8f), glow);

        for (int i = 0; i < 7; i++)
        {
            float x = -65f + i * 22f;
            float y = 36f + (i % 3) * 4f;
            float z = 95f + (i % 2) * 20f;
            Sphere("Soft distant cloud " + i, parent, new Vector3(x, y, z),
                new Vector3(14f, 2.3f, 4.5f), cloud);
        }
    }

    private static Transform Group(string name, Transform parent)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static GameObject Block(string name, Transform parent, Vector3 pos, Vector3 scale, Material material)
    {
        return Primitive(PrimitiveType.Cube, name, parent, pos, scale, material);
    }

    private static GameObject Beam(string name, Transform parent, Vector3 start,
        Vector3 end, float width, float depth, Material material)
    {
        Vector3 direction = end - start;
        if (direction.sqrMagnitude < 0.000001f)
            throw new ArgumentException("Roof beam endpoints must be different.");
        GameObject beam = Block(name, parent, (start + end) * 0.5f,
            new Vector3(width, direction.magnitude, depth), material);
        beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        return beam;
    }

    private static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 scale, Material material)
    {
        // Unity's standard cylinder is 2 units high; scale.y is its half-height.
        return Primitive(PrimitiveType.Cylinder, name, parent, pos, scale, material);
    }

    private static GameObject Sphere(string name, Transform parent, Vector3 pos, Vector3 scale, Material material)
    {
        return Primitive(PrimitiveType.Sphere, name, parent, pos, scale, material);
    }

    private static GameObject Primitive(PrimitiveType primitive, string name, Transform parent,
        Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(primitive);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go;
    }

    private static void RoofLayer(string name, Transform parent, float outside, float inside,
        float bottom, float top, Material material)
    {
        // Versioned asset avoids silently keeping the geometry of the previous
        // generated scene, while leaving any existing v1 mesh assets untouched.
        string assetPath = MeshFolder + "/" + name.Replace(" ", "_") + "_v02.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (mesh == null)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var outer = new[] {
                new Vector3(-outside, bottom, -outside),
                new Vector3(outside, bottom, -outside),
                new Vector3(outside, bottom, outside),
                new Vector3(-outside, bottom, outside)
            };
            var inner = new[] {
                new Vector3(-inside, top, -inside),
                new Vector3(inside, top, -inside),
                new Vector3(inside, top, inside),
                new Vector3(-inside, top, inside)
            };

            // Subdivided square roof facets with a gentle raised-corner eave.
            // Both sides are rendered so the underside reads from inside the
            // pavilion. This is simple geometry, not expensive tessellation.
            const int segmentsPerSide = 8;
            for (int side = 0; side < 4; side++)
            {
                int next = (side + 1) % 4;
                for (int segment = 0; segment < segmentsPerSide; segment++)
                {
                    float t0 = (float)segment / segmentsPerSide;
                    float t1 = (float)(segment + 1) / segmentsPerSide;
                    Vector3 a = Vector3.Lerp(outer[side], outer[next], t0);
                    Vector3 b = Vector3.Lerp(outer[side], outer[next], t1);
                    // The slight eave upsweep appears toward the corners only.
                    a.y += 0.12f * Mathf.Pow(Mathf.Abs(2f * t0 - 1f), 4f);
                    b.y += 0.12f * Mathf.Pow(Mathf.Abs(2f * t1 - 1f), 4f);
                    Vector3 c = Vector3.Lerp(inner[side], inner[next], t1);
                    Vector3 d = Vector3.Lerp(inner[side], inner[next], t0);
                    int baseIndex = vertices.Count;
                    vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                    vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                    triangles.AddRange(new[] {
                        baseIndex, baseIndex + 1, baseIndex + 2,
                        baseIndex, baseIndex + 2, baseIndex + 3,
                        baseIndex + 4, baseIndex + 6, baseIndex + 5,
                        baseIndex + 4, baseIndex + 7, baseIndex + 6
                    });
                }
            }
            mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, assetPath);
        }

        var roofObject = new GameObject(name);
        roofObject.transform.SetParent(parent, false);
        roofObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = roofObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Material Mat(string name, byte r, byte g, byte b)
    {
        Material found;
        if (Materials.TryGetValue(name, out found) && found != null) return found;

        string path = MaterialFolder + "/" + name + ".mat";
        found = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (found == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) throw new InvalidOperationException("No supported material shader found.");
            found = new Material(shader) { name = "Jemparingan_Preview_" + name };
            Color color = new Color32(r, g, b, 255);
            if (found.HasProperty("_BaseColor")) found.SetColor("_BaseColor", color);
            if (found.HasProperty("_Color")) found.SetColor("_Color", color);
            if (found.HasProperty("_Smoothness")) found.SetFloat("_Smoothness", .12f);
            found.enableInstancing = true;
            AssetDatabase.CreateAsset(found, path);
        }
        Materials[name] = found;
        return found;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path);
        string child = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent))
            throw new DirectoryNotFoundException("Missing Unity asset folder: " + parent);
        AssetDatabase.CreateFolder(parent.Replace('\\', '/'), child);
    }
}
#endif

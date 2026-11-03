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
    private const string PlayableScenePath = "Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity";
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
        CreateEnvironment(false);
    }

    [MenuItem("Tools/Jemparingan/Environment/Create Playable Environment v5")]
    public static void CreatePlayablePreview()
    {
        CreateEnvironment(true);
    }

    private static void CreateEnvironment(bool playable)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before creating a Jemparingan environment.");
            return;
        }
        // Never silently discard unsaved changes in an open scene.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string scenePath = playable ? PlayableScenePath : ScenePath;
        if (File.Exists(scenePath) &&
            !EditorUtility.DisplayDialog(
                "Environment scene already exists",
                "Only the separate scene at " + scenePath + " will be overwritten. " +
                "The original Jemparingan v0.9 gameplay scene will not be touched.",
                "Overwrite this preview", "Cancel"))
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
            CreateGoldenHourSkybox();

            var pendopo = Group("01 | Joglo pendopo - saka guru, tumpang sari, brunjung", null);
            BuildPendopo(pendopo);
            var courtyard = Group("02 | Archery courtyard and 30m lane", null);
            BuildCourtyard(courtyard);

            var target = Group(TargetName, null);
            target.position = new Vector3(0f, 0f, SeatZ + 10f);
            // Only one bandulan: standalone art in the scenic preview, or
            // the game's active moving target under this frame during Play.
            BuildBandulan(target, !playable);
            if (playable)
            {
                var anchor = Group("Gameplay bandulan anchor | 1.79m above ground", target);
                anchor.localPosition = new Vector3(0f, 1.79f, 0f);
                AddPlayableGame(anchor);
            }

            var landscape = Group("03 | Rice fields, bamboo and trees", null);
            BuildLandscape(landscape);
            var horizon = Group("04 | Distant mountains and clouds", null);
            BuildHorizon(horizon);

            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new InvalidOperationException("Unity could not save the environment scene.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = GameObject.Find("Player viewpoint | seated 1.35m");
            Debug.Log("Jemparingan environment created: " + scenePath +
                (playable
                    ? " | Play Mode: left mouse draw/release, right mouse aim, R restart."
                    : " | Scenic-only preview (no gameplay).") +
                " Target distance can be changed in Edit Mode from the Environment menu.");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Debug.LogError("Environment generation failed. The original gameplay scene has not been edited.");
        }
    }

    private static void AddPlayableGame(Transform targetAnchor)
    {
        var gameObject = new GameObject("Jemparingan Gameplay | Outdoor mode v5");
        gameObject.AddComponent<DesktopBowInput>();
        gameObject.AddComponent<MetaQuestBowInput>();
        var gameplay = gameObject.AddComponent<JemparinganGame>();

        // Serialise the opt-in bridge into the saved .unity scene, so it still
        // works when the user closes/reopens Unity. No reflection at runtime.
        var serialized = new SerializedObject(gameplay);
        serialized.FindProperty("useOutdoorEnvironment").boolValue = true;
        serialized.FindProperty("outdoorTargetAnchor").objectReferenceValue = targetAnchor;
        serialized.FindProperty("outdoorArrowLifetime").floatValue = 3.0f;
        serialized.FindProperty("minimumArrowSpeed").floatValue = 20f;
        serialized.FindProperty("maximumArrowSpeed").floatValue = 27f;
        serialized.FindProperty("simulatedGravity").floatValue = 4.5f;
        serialized.FindProperty("aimAssist").floatValue = .65f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
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
        if (scene.path != ScenePath && scene.path != PlayableScenePath)
        {
            Debug.LogWarning("Open either the scenic preview or playable v5 scene before changing the target distance.");
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

    // Read-only structural smoke test for the newly generated scene.
    // Runs entirely in Edit Mode and does not change gameplay or saved assets.
    [MenuItem("Tools/Jemparingan/Environment/Validate Joglo Preview v2")]
    private static void ValidateJogloPreview()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded ||
            (scene.path != ScenePath && scene.path != PlayableScenePath))
        {
            Debug.LogError("Open a generated Jemparingan environment preview before running structural validation.");
            return;
        }

        int sakaGuru = 0;
        int tumpangTiers = 0;
        int roofLayers = 0;
        int cameras = 0;
        Camera previewCamera = null;
        Transform target = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == TargetName) target = root.transform;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Saka guru teak post") sakaGuru++;
                if (t.name.StartsWith("Tumpang sari tier ", StringComparison.Ordinal)) tumpangTiers++;
                if (t.name == "Penanggap lower roof" || t.name == "Brunjung high roof") roofLayers++;
            }
            foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
            {
                cameras++;
                if (cam.CompareTag("MainCamera")) previewCamera = cam;
            }
        }

        bool validDistance = false;
        float distance = -1f;
        if (previewCamera != null && target != null)
        {
            distance = Vector3.Distance(
                new Vector3(target.position.x, 0f, target.position.z),
                new Vector3(previewCamera.transform.position.x, 0f,
                    previewCamera.transform.position.z));
            validDistance = Mathf.Abs(distance - 10f) < .03f ||
                Mathf.Abs(distance - 20f) < .03f ||
                Mathf.Abs(distance - 30f) < .03f;
        }

        bool valid = sakaGuru == 4 && tumpangTiers == 4 &&
            roofLayers == 2 && cameras == 1 && validDistance;
        string report = "Joglo v2 preview: saka guru=" + sakaGuru +
            "/4, tumpang sari tiers=" + tumpangTiers +
            "/4, roof layers=" + roofLayers + "/2, cameras=" + cameras +
            "/1, horizontal target distance=" + distance.ToString("F2") + "m.";
        if (valid)
            Debug.Log(report + " Structural checks PASSED. Still requires visual review in Game view.");
        else
            Debug.LogError(report + " Structural checks FAILED. Regenerate the preview or inspect missing objects.");
    }

    // The only v5 test that can run without Play Mode: read-only check of
    // serialized scene wiring and initial projectile tuning.
    [MenuItem("Tools/Jemparingan/Environment/Validate Playable v5 Setup")]
    private static void ValidatePlayableV5()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != PlayableScenePath)
        {
            Debug.LogError("Open " + PlayableScenePath + " before validating playable v5.");
            return;
        }

        JemparinganGame foundGame = null;
        int gameCount = 0, cameraCount = 0, inputCount = 0;
        int staticBandulanCount = 0;
        Transform frame = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == TargetName) frame = root.transform;
            foreach (JemparinganGame game in root.GetComponentsInChildren<JemparinganGame>(true))
            {
                gameCount++;
                foundGame = game;
            }
            cameraCount += root.GetComponentsInChildren<Camera>(true).Length;
            inputCount += root.GetComponentsInChildren<DesktopBowInput>(true).Length;
            foreach (Transform transformInScene in root.GetComponentsInChildren<Transform>(true))
                if (transformInScene.name == "Bandulan white lower body") staticBandulanCount++;
        }

        bool correctlyWired = false;
        float minimumSpeed = 0f, maximumSpeed = 0f;
        if (foundGame != null)
        {
            var serialized = new SerializedObject(foundGame);
            SerializedProperty anchor = serialized.FindProperty("outdoorTargetAnchor");
            SerializedProperty outdoor = serialized.FindProperty("useOutdoorEnvironment");
            SerializedProperty minimum = serialized.FindProperty("minimumArrowSpeed");
            SerializedProperty maximum = serialized.FindProperty("maximumArrowSpeed");
            if (anchor != null && outdoor != null && minimum != null && maximum != null)
            {
                var targetAnchor = anchor.objectReferenceValue as Transform;
                minimumSpeed = minimum.floatValue;
                maximumSpeed = maximum.floatValue;
                correctlyWired = outdoor.boolValue && targetAnchor != null &&
                    frame != null && targetAnchor.IsChildOf(frame) &&
                    maximumSpeed >= minimumSpeed && minimumSpeed >= 15f;
            }
        }

        bool passed = correctlyWired && gameCount == 1 && cameraCount == 1 &&
            inputCount == 1 && staticBandulanCount == 0;
        string report = "Playable v5 setup: gameplay=" + gameCount + "/1, cameras=" +
            cameraCount + "/1, desktop input=" + inputCount +
            "/1, duplicate static bandulan=" + staticBandulanCount +
            "/0, outdoor config=" + correctlyWired +
            ", arrow speed " + minimumSpeed + ".." + maximumSpeed + " m/s.";
        if (passed)
            Debug.Log(report + " PASS. Play Mode shot tests and Quest tests are still required.");
        else
            Debug.LogError(report + " FAIL. Regenerate the playable v5 scene.");
    }

    private static void ConfigureAtmosphere()
    {
        // The procedural skybox is installed after the sun is created, so it can
        // follow the main directional light. Fall back to camera clear color if
        // the skybox shader is unavailable on this Unity installation.
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.74f, 0.78f, 0.72f);
        RenderSettings.fogStartDistance = 75f;
        RenderSettings.fogEndDistance = 210f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.72f, 0.79f, 0.90f);
        RenderSettings.ambientEquatorColor = new Color(0.92f, 0.78f, 0.63f);
        RenderSettings.ambientGroundColor = new Color(0.35f, 0.38f, 0.30f);
        RenderSettings.ambientIntensity = 0.95f;
    }

    private static void CreateCamera()
    {
        var go = new GameObject("Player viewpoint | seated 1.35m");
        go.tag = "MainCamera";
        Camera camera = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.backgroundColor = new Color(0.65f, 0.78f, 0.87f);
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
        light.color = new Color(1.0f, 0.84f, 0.66f);
        light.intensity = 1.32f;
        // No realtime shadows during initial scene layout validation.
        light.shadows = LightShadows.None;
        go.transform.rotation = Quaternion.Euler(48f, -40f, 0f);
        RenderSettings.sun = light;
    }

    private static void CreateGoldenHourSkybox()
    {
        const string skyPath = AssetRoot + "/GoldenHourSky_v03.mat";
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (sky == null)
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                Debug.LogWarning("Procedural skybox shader not found; using camera's blue clear color.");
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                return;
            }

            sky = new Material(shader) { name = "Jemparingan Golden Hour Sky v3" };
            sky.SetColor("_SkyTint", new Color(.53f, .64f, .83f));
            sky.SetColor("_GroundColor", new Color(.62f, .53f, .43f));
            sky.SetFloat("_AtmosphereThickness", 1.15f);
            sky.SetFloat("_Exposure", 1.10f);
            sky.SetFloat("_SunSize", 0.035f);
            sky.SetFloat("_SunSizeConvergence", 4f);
            AssetDatabase.CreateAsset(sky, skyPath);
        }
        RenderSettings.skybox = sky;
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

    private static void BuildBandulan(Transform parent, bool showStaticBandulan)
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
        if (!showStaticBandulan) return;

        // Scenic preview: static stand-in. The playable scene instead spawns a
        // real JemparinganTarget from JemparinganGame at the anchor.
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
        Material riceC = Mat("Rice_YellowGreen_v4", 168, 184, 93);
        Material bund = Mat("Paddy_Bund", 111, 117, 70);
        Material bamboo = Mat("Bamboo", 152, 129, 76);
        Material trunk = Mat("Tree_Trunk", 99, 78, 54);
        Material leavesA = Mat("Leaf_Green", 62, 122, 69);
        Material leavesB = Mat("Leaf_Light", 88, 151, 74);
        Material earth = Mat("Dry_Rustic_Path_v4", 159, 135, 92);
        Material darkLeaves = Mat("Banana_Leaves_v4", 66, 133, 65);

        var fields = Group("A | Irregular sawah paddy terraces", parent);
        // Deterministic offset rows: fewer, larger plots; no chessboard pattern,
        // no random allocations or per-frame vegetation simulation.
        for (int row = 0; row < 10; row++)
        {
            float z = 23f + row * 6.0f;
            for (int side = -1; side <= 1; side += 2)
            for (int lane = 0; lane < 4; lane++)
            {
                float wobble = Mathf.Sin(row * 1.31f + lane * 2.04f);
                float x = side * (8.6f + lane * 5.1f) + wobble * 0.40f;
                float width = 3.95f + 0.3f * Mathf.Sin(lane + row);
                float length = 4.65f + 0.38f * Mathf.Cos(row + 2 * lane);
                Material field = (row + lane) % 4 == 0 ? riceC :
                    (row + lane) % 2 == 0 ? riceA : riceB;
                var plot = Group("Paddy " + side + "-" + row + "-" + lane, fields);
                Block("Rice field", plot, new Vector3(x, 0.010f, z),
                    new Vector3(width, 0.05f, length), field);
                Block("Earth bund", plot,
                    new Vector3(x, 0.016f, z + length * 0.51f),
                    new Vector3(width + 0.35f, 0.055f, 0.28f), bund);
            }
        }

        var approach = Group("B | Natural side access paths", parent);
        foreach (float side in new[] {-1f, 1f})
        {
            for (int i = 0; i < 8; i++)
            {
                float z = 9f + i * 9.5f;
                float x = side * (6.45f + Mathf.Sin(i * .82f) * .65f);
                Block("Soft earth path segment", approach,
                    new Vector3(x, -0.005f, z),
                    new Vector3(1.25f, 0.05f, 9.8f), earth);
            }
        }

        var fence = Group("C | Bamboo boundary", parent);
        foreach (float side in new[] {-1f, 1f})
        {
            float x = side * 5.2f;
            for (int row = 0; row <= 10; row++)
            {
                float z = 6f + row * 4.3f;
                Cylinder("Bamboo fence post", fence, new Vector3(x, 0.62f, z),
                    new Vector3(0.055f, 0.62f, 0.055f), bamboo);
            }
            foreach (float y in new[] {0.44f, 0.86f})
                Block("Bamboo fence rail", fence, new Vector3(x, y, 27.5f),
                    new Vector3(0.075f, 0.065f, 44f), bamboo);
        }

        var trees = Group("D | Tropical tree clusters", parent);
        var treePositions = new [] {
            new Vector3(-10f, 0f, 7f), new Vector3(10f, 0f, 12f),
            new Vector3(-13f, 0f, 23f), new Vector3(15f, 0f, 29f),
            new Vector3(-24f, 0f, 37f), new Vector3(25f, 0f, 43f),
            new Vector3(-34f, 0f, 59f), new Vector3(35f, 0f, 57f),
            new Vector3(-44f, 0f, 74f), new Vector3(47f, 0f, 81f)
        };
        for (int i = 0; i < treePositions.Length; i++)
        {
            var tree = Group("Tree " + (i + 1), trees);
            tree.localPosition = treePositions[i];
            float height = 3.4f + (i % 4) * 0.55f;
            Cylinder("Trunk", tree, new Vector3(0f, height * .46f, 0f),
                new Vector3(.17f, height * .46f, .17f), trunk);
            Sphere("Crown center", tree, new Vector3(0f, height, 0f),
                new Vector3(1.7f, 1.6f, 1.7f), i % 2 == 0 ? leavesA : leavesB);
            Sphere("Crown left", tree, new Vector3(-.88f, height - .46f, .22f),
                new Vector3(1.34f, 1.18f, 1.27f), leavesB);
            Sphere("Crown right", tree, new Vector3(.84f, height - .42f, -.10f),
                new Vector3(1.45f, 1.32f, 1.29f), leavesA);
        }

        var bananas = Group("E | Banana leaf silhouettes", parent);
        foreach (Vector3 position in new [] {
            new Vector3(-7.6f, 0f, 15f), new Vector3(8.0f, 0f, 19f),
            new Vector3(-10.4f, 0f, 47f), new Vector3(9.5f, 0f, 56f)
        })
        {
            var plant = Group("Banana plant", bananas);
            plant.localPosition = position;
            Cylinder("Banana pseudostem", plant, new Vector3(0f, 1.26f, 0f),
                new Vector3(0.11f, 1.25f, 0.11f), bamboo);
            for (int leaf = 0; leaf < 6; leaf++)
            {
                float angle = leaf * Mathf.PI / 3f;
                Vector3 tip = new Vector3(Mathf.Cos(angle) * 1.4f,
                    2.35f + (leaf % 2) * .38f, Mathf.Sin(angle) * 1.30f);
                // Light flat leaf placeholder, to be replaced with Blender meshes.
                GameObject leafBlade = Block("Banana leaf blade", plant,
                    (new Vector3(0f, 2.44f, 0f) + tip) * .5f,
                    new Vector3(.37f, .035f, 1.55f), darkLeaves);
                Vector3 direction = tip - new Vector3(0f, 2.44f, 0f);
                leafBlade.transform.localRotation =
                    Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }
    }

    private static void BuildHorizon(Transform parent)
    {
        Material nearHill = Mat("Far_Hills_v4", 94, 134, 105);
        Material middle = Mat("Blue_Mountain_v4", 100, 134, 149);
        Material far = Mat("Far_Mountain_v4", 150, 168, 166);
        Material cloud = Mat("Soft_Cloud", 224, 230, 224);

        // Procedural 3D ridge meshes remove the giant-sphere appearance of v1.
        // Far mountains fade into linear fog, nearer hills remain warm green.
        Ridge("Far mountain ridge", parent, 160f, 180f, -3f, 27f, 14f, far);
        Ridge("Middle blue mountain ridge", parent, 145f, 155f, -3f,
            22f, 11f, middle);
        Ridge("Front rolling hills", parent, 145f, 122f, -3f,
            10f, 5f, nearHill);

        for (int i = 0; i < 6; i++)
        {
            float x = -61f + i * 24f;
            float y = 34f + (i % 3) * 3.7f;
            float z = 122f + (i % 2) * 22f;
            Sphere("Thin distant cloud " + i, parent,
                new Vector3(x, y, z), new Vector3(14f, 1.5f, 3.7f), cloud);
        }
    }

    private static void Ridge(string name, Transform parent, float halfWidth,
        float z, float bottom, float baseHeight, float variation, Material material)
    {
        string assetPath = MeshFolder + "/" + name.Replace(" ", "_") + "_v04.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (mesh == null)
        {
            const int segments = 48;
            var vertices = new List<Vector3>((segments + 1) * 2);
            var triangles = new List<int>(segments * 6);
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float x = (2f * t - 1f) * halfWidth;
                float undulation = 0.48f * Mathf.Sin(t * 11.7f + z * .017f) +
                    0.29f * Mathf.Sin(t * 24.1f + z * .036f) +
                    0.23f * Mathf.Cos(t * 38.7f - z * .025f);
                // Broad highest peak toward center; low variation on horizon.
                float centerPeak = Mathf.Exp(-Mathf.Pow((t - .58f) * 4f, 2f));
                float height = baseHeight + variation *
                    (0.48f * undulation + 0.54f * centerPeak);
                vertices.Add(new Vector3(x, bottom, z));
                vertices.Add(new Vector3(x, height, z));
                if (i == segments) continue;
                int v = i * 2;
                triangles.Add(v); triangles.Add(v + 1); triangles.Add(v + 2);
                triangles.Add(v + 1); triangles.Add(v + 3); triangles.Add(v + 2);
            }
            mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, assetPath);
        }

        var ridge = new GameObject(name);
        ridge.transform.SetParent(parent, false);
        ridge.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = ridge.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
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

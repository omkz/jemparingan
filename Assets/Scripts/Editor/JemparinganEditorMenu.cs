#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class JemparinganEditorMenu
{
    [MenuItem("Tools/Jemparingan/Create Optimized Scene v0.2")]
    public static void CreateOptimizedScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        var camera = cameraGO.AddComponent<Camera>();
        cameraGO.AddComponent<AudioListener>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.035f, .055f, .085f);
        camera.fieldOfView = 65f;
        camera.nearClipPlane = .04f;
        cameraGO.transform.position = new Vector3(0f, 1.55f, 0f);
        cameraGO.transform.LookAt(new Vector3(0f, 1.5f, 2.65f));

        var lightGO = new GameObject("Sunlight | No Realtime Shadows");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.None;
        light.intensity = 1.65f;
        lightGO.transform.rotation = Quaternion.Euler(47f, -30f, 0f);

        var game = new GameObject("Jemparingan Game");
        game.AddComponent<DesktopBowInput>();
        game.AddComponent<MetaQuestBowInput>();
        game.AddComponent<JemparinganGame>();

        Directory.CreateDirectory("Assets/Jemparingan/Scenes");
        AssetDatabase.Refresh();
        string path = "Assets/Jemparingan/Scenes/JemparinganOptimized_v02.unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("Jemparingan v0.2 scene created at " + path + " — Play; hold LMB, release to shoot, hold RMB to aim; R resets.");
    }

    [MenuItem("Tools/Jemparingan/Run Target Hitbox Checks")]
    public static void RunTargetHitboxChecks()
    {
        // Edit-mode smoke checks. Does not save or modify the open scene.
        var testObject = new GameObject("Jemparingan Hitbox Test (temporary)");
        try
        {
            var target = testObject.AddComponent<JemparinganTarget>();
            bool bullseye;
            bool center = target.TestShot(new Vector3(0f, 0f, -.2f),
                new Vector3(0f, 0f, .2f), out bullseye) && bullseye;
            bool outer = target.TestShot(new Vector3(.06f, .16f, -.2f),
                new Vector3(.06f, .16f, .2f), out bullseye) && !bullseye;
            bool miss = !target.TestShot(new Vector3(.15f, 0f, -.2f),
                new Vector3(.15f, 0f, .2f), out bullseye);
            if (center && outer && miss)
                Debug.Log("Jemparingan v0.5 hitbox checks passed (bullseye, outer hit, miss).");
            else
                Debug.LogError("Jemparingan v0.5 hitbox check failed: center=" + center +
                    ", outer=" + outer + ", miss=" + miss);
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }
    }

    // Deterministic rule smoke tests without NUnit or extra packages.
    // This does NOT replace a full Unity Play Mode or Quest device playtest.
    [MenuItem("Tools/Jemparingan/Run Gameplay QA v0.9")]
    public static void RunGameplayQA()
    {
        int total = 0;
        int failed = 0;

        Check("outer hit = 5", JemparinganScoreRules.PointsForHit(false, false, 1) == 5, ref total, ref failed);
        Check("bullseye = 10", JemparinganScoreRules.PointsForHit(true, false, 1) == 10, ref total, ref failed);
        Check("steady outer = 8", JemparinganScoreRules.PointsForHit(false, true, 1) == 8, ref total, ref failed);
        Check("third hit combo = 7", JemparinganScoreRules.PointsForHit(false, false, 3) == 7, ref total, ref failed);
        Check("bullseye + RASA + combo = 15",
            JemparinganScoreRules.PointsForHit(true, true, 3) == 15, ref total, ref failed);

        Check("accuracy 0/0 = 0", JemparinganScoreRules.AccuracyPercent(0, 0) == 0, ref total, ref failed);
        Check("accuracy 9/12 = 75", JemparinganScoreRules.AccuracyPercent(9, 12) == 75, ref total, ref failed);
        Check("stage first", JemparinganScoreRules.StageIndex(0) == 0 &&
            JemparinganScoreRules.StageIndex(3) == 0, ref total, ref failed);
        Check("stage second at shot 4", JemparinganScoreRules.StageIndex(4) == 1 &&
            JemparinganScoreRules.StageIndex(7) == 1, ref total, ref failed);
        Check("stage third at shot 8", JemparinganScoreRules.StageIndex(8) == 2 &&
            JemparinganScoreRules.StageIndex(12) == 2, ref total, ref failed);
        Check("stage label", JemparinganScoreRules.StageTitle(8).StartsWith("BABAK III"),
            ref total, ref failed);
        Check("round title considers composure",
            JemparinganScoreRules.ResultTitle(9, 4).Contains("R A S A"), ref total, ref failed);
        Check("round title distinguishes accuracy",
            JemparinganScoreRules.ResultTitle(9, 1).Contains("P A N A H"), ref total, ref failed);
        Check("arrow pool default 12",
            JemparinganScoreRules.PoolCapacity(12, 14) == 14, ref total, ref failed);
        Check("arrow pool 30",
            JemparinganScoreRules.PoolCapacity(30, 14) == 30, ref total, ref failed);

        Vector3 flat = JemparinganTrajectory.Position(Vector3.zero, Vector3.forward * 10f, 0f, 1f);
        Vector3 falling = JemparinganTrajectory.Position(Vector3.zero, Vector3.forward * 10f, 2f, 1f);
        Vector3 initial = JemparinganTrajectory.Position(new Vector3(1f, 2f, 3f), Vector3.forward, 2f, 0f);
        Check("trajectory no gravity", Vector3.Distance(flat, Vector3.forward * 10f) < .0001f,
            ref total, ref failed);
        Check("trajectory gravity at 1 sec", Vector3.Distance(falling,
            new Vector3(0f, -1f, 10f)) < .0001f, ref total, ref failed);
        Check("trajectory t0 remains at origin", Vector3.Distance(initial,
            new Vector3(1f, 2f, 3f)) < .0001f, ref total, ref failed);

        var testObject = new GameObject("Jemparingan QA target (temporary)");
        try
        {
            var target = testObject.AddComponent<JemparinganTarget>();
            bool bullseye;
            bool center = target.TestShot(new Vector3(0f, 0f, -.2f),
                new Vector3(0f, 0f, .2f), out bullseye) && bullseye;
            bool outer = target.TestShot(new Vector3(.06f, .16f, -.2f),
                new Vector3(.06f, .16f, .2f), out bullseye) && !bullseye;
            bool miss = !target.TestShot(new Vector3(.15f, 0f, -.2f),
                new Vector3(.15f, 0f, .2f), out bullseye);
            Check("center hitbox", center, ref total, ref failed);
            Check("outer hitbox", outer, ref total, ref failed);
            Check("outside hitbox misses", miss, ref total, ref failed);
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }

        if (failed == 0)
            Debug.Log("Jemparingan gameplay QA v0.9 PASSED (" + total +
                " deterministic rule/hitbox checks). Still requires full Play Mode testing.");
        else
            Debug.LogError("Jemparingan gameplay QA v0.9 FAILED (" + failed +
                " of " + total + " checks). Fix above errors before building.");
    }

    private static void Check(string name, bool condition, ref int total, ref int failed)
    {
        total++;
        if (condition) return;
        failed++;
        Debug.LogError("Jemparingan QA FAIL: " + name);
    }

    // SDK-free preflight check: works in the desktop prototype before Meta packages
    // are installed. Package/asset checks are advisory; runtime XR features require
    // the official Meta Project Setup Tool and an actual Quest test.
    [MenuItem("Tools/Jemparingan/Check Quest Prerequisites")]
    public static void CheckQuestPrerequisites()
    {
        int issues = 0;
        string manifestPath = "Packages/manifest.json";
        string manifest = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : "";

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            issues++;
            Debug.LogWarning("Quest preflight: active target is not Android / Meta Quest. Switch via File > Build Profiles.");
        }

        if (!manifest.Contains("\"com.unity.xr.management\""))
        {
            issues++;
            Debug.LogWarning("Quest preflight: XR Plug-in Management is not in manifest.json.");
        }

        if (!manifest.Contains("\"com.unity.xr.openxr\""))
        {
            issues++;
            Debug.LogWarning("Quest preflight: Unity OpenXR Plugin is not in manifest.json. Use Unity OpenXR, not deprecated Oculus XR Plugin.");
        }

        bool metaCoreAvailable = manifest.Contains("\"com.meta.xr.sdk.all\"") ||
            manifest.Contains("\"com.meta.xr.sdk.core\"") ||
            AssetDatabase.FindAssets("OVRManager t:MonoScript").Length > 0;
        if (!metaCoreAvailable)
        {
            issues++;
            Debug.LogWarning("Quest preflight: Meta XR Core SDK not detected. Install a compatible current Meta XR SDK.");
        }

        bool rigPrefabAvailable = AssetDatabase.FindAssets("OVRCameraRig t:Prefab").Length > 0;
        if (!rigPrefabAvailable)
        {
            issues++;
            Debug.LogWarning("Quest preflight: OVRCameraRig prefab not found yet. It is supplied by Meta XR SDK.");
        }

        bool enabledScene = false;
        foreach (EditorBuildSettingsScene item in EditorBuildSettings.scenes)
        {
            if (!item.enabled) continue;
            enabledScene = true;
            break;
        }
        if (!enabledScene)
        {
            issues++;
            Debug.LogWarning("Quest preflight: no enabled scene in the Build Profiles scene list.");
        }

        // Scene check is useful only after opening the intended Quest scene.
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid() && scene.isLoaded)
        {
            int cameraCount = 0;
            bool hasRig = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.Contains("OVRCameraRig")) hasRig = true;
                Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
                cameraCount += cameras.Length;
            }

            if (!hasRig)
            {
                issues++;
                Debug.LogWarning("Quest preflight: active scene has no OVRCameraRig. Keep the desktop scene; create a separate Quest scene.");
            }
            if (cameraCount > 1)
            {
                issues++;
                Debug.LogWarning("Quest preflight: active scene has multiple Cameras. Remove the old desktop Main Camera when using OVRCameraRig.");
            }
        }

        Debug.Log("Jemparingan Quest preflight finished. " + issues +
            " potential setup item(s) detected. Check warnings above. " +
            "Also verify Android OpenXR feature groups, hand tracking, passthrough, " +
            "arm64, and Meta Project Setup Tool manually. This does not validate an APK or headset.");
    }

    // Read-only readiness audit. Works before Meta XR SDK is imported,
    // and does not modify the desktop scene or ProjectSettings.
    [MenuItem("Tools/Jemparingan/Quest/Validate Active Scene")]
    public static void ValidateQuestScene()
    {
        var scene = SceneManager.GetActiveScene();
        int problems = 0;
        int warnings = 0;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Quest audit: open the intended Quest scene first.");
            return;
        }

        string manifestPath = "Packages/manifest.json";
        if (!File.Exists(manifestPath))
        {
            Debug.LogError("Quest audit: Packages/manifest.json not found.");
            problems++;
        }
        else
        {
            string manifest = File.ReadAllText(manifestPath);
            if (!manifest.Contains("com.unity.xr.management"))
            {
                Debug.LogWarning("Quest audit: XR Plug-in Management is not listed. Install it from Unity Package Manager.");
                warnings++;
            }
            if (!manifest.Contains("com.unity.xr.openxr"))
            {
                Debug.LogWarning("Quest audit: Unity OpenXR is not listed. Add OpenXR and enable the Android provider.");
                warnings++;
            }
            if (!manifest.Contains("com.meta.xr.sdk.all") &&
                !manifest.Contains("com.meta.xr.sdk.core"))
            {
                Debug.LogWarning("Quest audit: Meta XR SDK is not listed as a UPM package. Check Package Manager / imported SDK.");
                warnings++;
            }
        }
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.LogWarning("Quest audit: active build target is not Android / Meta Quest.");
            warnings++;
        }

        GameObject rig = null;
        int gameCount = 0;
        int legacyCameraCount = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                if (behaviour is JemparinganGame) gameCount++;
                if (behaviour.GetType().Name == "OVRCameraRig") rig = behaviour.gameObject;
            }
            // Only flag a standalone Main Camera, not cameras within OVRCameraRig.
            if (root.name == "Main Camera") legacyCameraCount++;
        }
        if (gameCount != 1)
        {
            Debug.LogError("Quest audit: expected exactly one JemparinganGame, found " + gameCount + ".");
            problems++;
        }
        if (rig == null)
        {
            Debug.LogError("Quest audit: OVRCameraRig missing. Replace the desktop Main Camera with the SDK rig.");
            problems++;
        }
        if (legacyCameraCount > 0)
        {
            Debug.LogError("Quest audit: standalone Main Camera still present; remove it from the Quest scene.");
            problems++;
        }

        if (rig != null)
        {
            var leftAnchor = rig.transform.Find("TrackingSpace/LeftHandAnchor");
            var rightAnchor = rig.transform.Find("TrackingSpace/RightHandAnchor");
            if (leftAnchor == null || rightAnchor == null)
            {
                Debug.LogError("Quest audit: hand anchors not found under OVRCameraRig/TrackingSpace.");
                problems++;
            }
            else
            {
                if (!HasComponentNamed(leftAnchor, "OVRHand"))
                {
                    Debug.LogError("Quest audit: OVRHand prefab missing under LeftHandAnchor.");
                    problems++;
                }
                if (!HasComponentNamed(rightAnchor, "OVRHand"))
                {
                    Debug.LogError("Quest audit: OVRHand prefab missing under RightHandAnchor.");
                    problems++;
                }
            }
            if (!HasComponentNamed(rig.transform, "OVRPassthroughLayer"))
            {
                Debug.LogWarning("Quest audit: OVRPassthroughLayer missing from the rig; Mixed Reality underlay is not configured.");
                warnings++;
            }
            if (!HasComponentNamed(rig.transform, "OVRManager"))
            {
                Debug.LogError("Quest audit: OVRManager missing from rig.");
                problems++;
            }
        }

#if META_XR_SDK
        var adapters = Object.FindObjectsByType<MetaQuestBowInput>(FindObjectsSortMode.None);
        if (adapters.Length != 1 || adapters[0].leftHand == null || adapters[0].rightHand == null)
        {
            Debug.LogError("Quest audit: MetaQuestBowInput needs both OVRHand references assigned.");
            problems++;
        }
#else
        Debug.LogWarning("Quest audit: META_XR_SDK scripting define is not active; MetaQuestBowInput is in stub mode.");
        warnings++;
#endif

        if (problems == 0 && warnings == 0)
            Debug.Log("Jemparingan Quest scene audit PASSED (static scene/package checks only; requires device tests).");
        else
            Debug.LogWarning("Jemparingan Quest scene audit: " + problems + " blockers, " +
                warnings + " warnings. Review Console and docs/QUEST_SETUP_V10.md.");
    }

    private static bool HasComponentNamed(Transform parent, string componentTypeName)
    {
        foreach (Component component in parent.GetComponentsInChildren<Component>(true))
            if (component != null && component.GetType().Name == componentTypeName) return true;
        return false;
    }

    [MenuItem("Tools/Jemparingan/Create Prototype Scene")]
    public static void CreatePrototypeScene() => CreateOptimizedScene();
}
#endif

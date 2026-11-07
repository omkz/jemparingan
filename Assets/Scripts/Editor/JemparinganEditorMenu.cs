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
            bool head = target.TestShot(new Vector3(0f, .145f, -.2f),
                new Vector3(0f, .145f, .2f), out bullseye) &&
                target.LastTraditionalZone == JemparinganTarget.TraditionalZone.Molo;
            bool neck = target.TestShot(new Vector3(0f, .085f, -.2f),
                new Vector3(0f, .085f, .2f), out bullseye) &&
                target.LastTraditionalZone == JemparinganTarget.TraditionalZone.Jangga;
            bool body = target.TestShot(new Vector3(0f, -.10f, -.2f),
                new Vector3(0f, -.10f, .2f), out bullseye) &&
                target.LastTraditionalZone == JemparinganTarget.TraditionalZone.Awak;
            bool miss = !target.TestShot(new Vector3(.15f, 0f, -.2f),
                new Vector3(.15f, 0f, .2f), out bullseye);
            if (head && neck && body && miss)
                Debug.Log("Traditional hitbox checks passed (molo, jangga, awak, miss).");
            else
                Debug.LogError("Traditional hitbox check failed: molo=" + head +
                    ", jangga=" + neck + ", awak=" + body + ", miss=" + miss);
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }
    }

    // Deterministic rule smoke tests without NUnit or extra packages.
    // This does NOT replace a full Unity Play Mode or Quest device playtest.
    [MenuItem("Tools/Jemparingan/Run Gameplay QA (Traditional + Modern)")]
    public static void RunGameplayQA()
    {
        int total = 0;
        int failed = 0;

        Check("accuracy 0/0 = 0", JemparinganScoreRules.AccuracyPercent(0, 0) == 0, ref total, ref failed);
        Check("accuracy 9/12 = 75", JemparinganScoreRules.AccuracyPercent(9, 12) == 75, ref total, ref failed);
        Check("Traditional pool handles 12 arrows",
            JemparinganScoreRules.PoolCapacity(12, 14) == 14, ref total, ref failed);
        Check("Modern pool handles 16 arrows",
            JemparinganScoreRules.PoolCapacity(16, 14) == 16, ref total, ref failed);
        Check("arrow pool 30",
            JemparinganScoreRules.PoolCapacity(30, 14) == 30, ref total, ref failed);
        Check("Traditional red 3", JemparinganScoreRules.TraditionalPointsForZone(
            JemparinganTarget.TraditionalZone.Molo) == 3, ref total, ref failed);
        Check("Traditional yellow 2", JemparinganScoreRules.TraditionalPointsForZone(
            JemparinganTarget.TraditionalZone.Jangga) == 2, ref total, ref failed);
        Check("Traditional white 1", JemparinganScoreRules.TraditionalPointsForZone(
            JemparinganTarget.TraditionalZone.Awak) == 1, ref total, ref failed);
        Check("Traditional miss 0", JemparinganScoreRules.TraditionalPointsForZone(
            JemparinganTarget.TraditionalZone.None) == 0, ref total, ref failed);

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
            bool head = target.TestShot(new Vector3(0f, .145f, -.2f),
                new Vector3(0f, .145f, .2f), out bullseye) &&
                target.LastTraditionalZone == JemparinganTarget.TraditionalZone.Molo;
            bool neck = target.TestShot(new Vector3(0f, .085f, -.2f),
                new Vector3(0f, .085f, .2f), out bullseye) &&
                target.LastTraditionalZone == JemparinganTarget.TraditionalZone.Jangga;
            bool body = target.TestShot(new Vector3(0f, -.10f, -.2f),
                new Vector3(0f, -.10f, .2f), out bullseye) &&
                target.LastTraditionalZone == JemparinganTarget.TraditionalZone.Awak;
            Check("Traditional red zone", head, ref total, ref failed);
            Check("Traditional yellow zone", neck, ref total, ref failed);
            Check("Traditional white zone", body, ref total, ref failed);
            bool offsetImpact = target.TestShot(new Vector3(.025f, .145f, -.2f),
                new Vector3(.025f, .145f, .2f), out bullseye);
            Check("Traditional hit feedback uses actual impact point",
                offsetImpact &&
                Mathf.Abs(target.LastImpactLocal.x - .025f) < .0001f &&
                Mathf.Abs(target.LastImpactLocal.y - .145f) < .0001f,
                ref total, ref failed);
            bool farMiss = !target.TestShot(new Vector3(.19f, 0f, -.2f),
                new Vector3(.19f, 0f, .2f), out bullseye);
            Check("Traditional missed shot clears cached impact point",
                farMiss && target.LastImpactLocal == Vector3.zero,
                ref total, ref failed);
            Check("Traditional hit FX intensity follows zone score",
                JemparinganTarget.ImpactStrength(JemparinganTarget.TraditionalZone.Molo) >
                JemparinganTarget.ImpactStrength(JemparinganTarget.TraditionalZone.Jangga) &&
                JemparinganTarget.ImpactStrength(JemparinganTarget.TraditionalZone.Jangga) >
                JemparinganTarget.ImpactStrength(JemparinganTarget.TraditionalZone.Awak) &&
                JemparinganTarget.ImpactStrength(JemparinganTarget.TraditionalZone.Awak) > 0f &&
                JemparinganTarget.ImpactStrength(JemparinganTarget.TraditionalZone.None) == 0f,
                ref total, ref failed);
            bool nearNeckBottom = target.TestShot(new Vector3(0f, .055f, -.2f),
                new Vector3(0f, .055f, .2f), out bullseye) &&
                target.LastTraditionalZone == JemparinganTarget.TraditionalZone.Jangga;
            bool nearHeadBottom = target.TestShot(new Vector3(0f, .105f, -.2f),
                new Vector3(0f, .105f, .2f), out bullseye) &&
                target.LastTraditionalZone == JemparinganTarget.TraditionalZone.Molo;
            bool belowNeck = target.TestShot(new Vector3(0f, .045f, -.2f),
                new Vector3(0f, .045f, .2f), out bullseye) &&
                target.LastTraditionalZone == JemparinganTarget.TraditionalZone.Awak;
            bool aboveTop = !target.TestShot(new Vector3(0f, .22f, -.2f),
                new Vector3(0f, .22f, .2f), out bullseye);
            bool belowBottom = !target.TestShot(new Vector3(0f, -.24f, -.2f),
                new Vector3(0f, -.24f, .2f), out bullseye);
            Check("Traditional neck lower visual boundary",
                nearNeckBottom, ref total, ref failed);
            Check("Traditional head lower visual boundary",
                nearHeadBottom, ref total, ref failed);
            Check("Traditional white ends at neck",
                belowNeck, ref total, ref failed);
            Check("Traditional above visible target misses",
                aboveTop, ref total, ref failed);
            Check("Traditional below visible target misses",
                belowBottom, ref total, ref failed);
            // Modern Archery v1 rings: same swept projectile, circular board.
            Check("Modern rings 10 at center",
                JemparinganModernTarget.PointsForRadius(0f) == 10 &&
                JemparinganModernTarget.PointsForRadius(.085f) == 10,
                ref total, ref failed);
            Check("Modern rings 8/6/4/2",
                JemparinganModernTarget.PointsForRadius(.10f) == 8 &&
                JemparinganModernTarget.PointsForRadius(.20f) == 6 &&
                JemparinganModernTarget.PointsForRadius(.32f) == 4 &&
                JemparinganModernTarget.PointsForRadius(.46f) == 2,
                ref total, ref failed);
            Check("Modern rings miss outside",
                JemparinganModernTarget.PointsForRadius(.51f) == 0,
                ref total, ref failed);

            var modernObject = new GameObject("Modern ring QA (temporary)");
            try
            {
                var modern = modernObject.AddComponent<JemparinganModernTarget>();
                Vector3 impact;
                int ringScore;
                Check("Modern center swept hit",
                    modern.TestShot(new Vector3(0f, 0f, -.2f),
                        new Vector3(0f, 0f, .2f), out ringScore, out impact) &&
                    ringScore == 10 && Mathf.Abs(impact.z - JemparinganModernTarget.FaceDepth) < .0001f,
                    ref total, ref failed);
                Check("Modern red swept hit",
                    modern.TestShot(new Vector3(.12f, 0f, -.2f),
                        new Vector3(.12f, 0f, .2f), out ringScore, out impact) &&
                    ringScore == 8, ref total, ref failed);
                Check("Modern off-board swept miss",
                    !modern.TestShot(new Vector3(.60f, 0f, -.2f),
                        new Vector3(.60f, 0f, .2f), out ringScore, out impact),
                    ref total, ref failed);
                Check("Modern parallel segment misses",
                    !modern.TestShot(new Vector3(0f, 0f, -.2f),
                        new Vector3(.2f, 0f, -.2f), out ringScore, out impact),
                    ref total, ref failed);
            }
            finally
            {
                Object.DestroyImmediate(modernObject);
            }
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }

        // Pure pendulum regression checks: trajectory is bounded, smooth
        // around the reversal and visibly more challenging in level 3.
        Vector3 still = JemparinganTarget.PendulumOffset(0f, 0f);
        Vector3 gentle = JemparinganTarget.PendulumOffset(Mathf.PI * .5f, .08f);
        Vector3 harder = JemparinganTarget.PendulumOffset(Mathf.PI * .5f, .20f);
        Vector3 reverse = JemparinganTarget.PendulumOffset(
            Mathf.PI * 1.5f, .20f);
        Check("Traditional level 1 is stationary",
            still.sqrMagnitude < .000001f, ref total, ref failed);
        Check("Traditional level 3 has wider swing than level 2",
            harder.x > gentle.x && gentle.x > 0f &&
            Mathf.Abs(reverse.x + harder.x) < .00001f,
            ref total, ref failed);
        Check("Traditional pendulum follows a shallow rising arc",
            harder.y > gentle.y && gentle.y > 0f && harder.y < .04f,
            ref total, ref failed);
        Vector3 beforeTurn = JemparinganTarget.PendulumOffset(
            Mathf.PI * .5f - .03f, .20f);
        Vector3 afterTurn = JemparinganTarget.PendulumOffset(
            Mathf.PI * .5f + .03f, .20f);
        Check("Traditional pendulum reverses smoothly",
            Mathf.Abs(beforeTurn.x - afterTurn.x) < .0001f,
            ref total, ref failed);
        Vector3 varied = JemparinganTarget.PendulumOffset(
            1.1f, .20f, .12f);
        Check("Traditional harder rhythm is deterministic and bounded",
            Vector3.Distance(varied,
                JemparinganTarget.PendulumOffset(1.1f, .20f, .12f)) < .000001f &&
            Mathf.Abs(varied.x) <= .20001f,
            ref total, ref failed);

        // Traditional shares Modern's optical/ballistic alignment, but
        // must not magnetize a shot towards any red/yellow/white target zone.
        var sightCameraObject = new GameObject("Traditional sight QA camera (temporary)");
        var sightBandulanObject = new GameObject("Traditional sight QA target (temporary)");
        try
        {
            Camera sightCamera = sightCameraObject.AddComponent<Camera>();
            sightCamera.transform.position = Vector3.zero;
            sightCamera.transform.rotation = Quaternion.identity;
            sightBandulanObject.transform.position = Vector3.forward * 10f;

            Vector3 straightDirection;
            bool straightOnBandulan;
            bool straightZero = JemparinganModernAim.TryZeroToBandulanSightRay(
                sightCamera, Vector3.forward,
                new Vector3(.15f, -.16f, .95f), 11f, 2.25f, 4f,
                sightBandulanObject.transform,
                out straightDirection, out straightOnBandulan);
            Check("Traditional sight zero on bandulan plane",
                straightZero && straightOnBandulan &&
                Mathf.Abs(straightDirection.magnitude - 1f) < .001f,
                ref total, ref failed);

            Vector3 outsideDirection;
            bool outsideOnBandulan;
            bool outsideZero = JemparinganModernAim.TryZeroToBandulanSightRay(
                sightCamera, new Vector3(.15f, 0f, 1f).normalized,
                new Vector3(.15f, -.16f, .95f), 11f, 2.25f, 4f,
                sightBandulanObject.transform,
                out outsideDirection, out outsideOnBandulan);
            Check("Traditional off-bandulan aim does not snap",
                outsideZero && !outsideOnBandulan &&
                outsideDirection.x > straightDirection.x,
                ref total, ref failed);
        }
        finally
        {
            Object.DestroyImmediate(sightBandulanObject);
            Object.DestroyImmediate(sightCameraObject);
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

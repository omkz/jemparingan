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

        // Cosmetic bow polish must preserve the existing string attachment
        // points used by gameplay and the physical sight calibration.
        Vector3 upperBowTip = JemparinganBowVisual.UpperTip;
        Vector3 lowerBowTip = JemparinganBowVisual.LowerTip;
        float originalTipX = .015f + .11f * Mathf.Sin(Mathf.PI * .90f);
        Check("Shared bow polish preserves original string tip anchors",
            Mathf.Abs(upperBowTip.x - originalTipX) < .0001f &&
            Mathf.Abs(lowerBowTip.x - originalTipX) < .0001f &&
            Mathf.Abs(upperBowTip.y - .65f) < .0001f &&
            Mathf.Abs(lowerBowTip.y + .65f) < .0001f &&
            Mathf.Abs(upperBowTip.z - .095f) < .0001f &&
            Mathf.Abs(lowerBowTip.z - .095f) < .0001f,
            ref total, ref failed);

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

    // SDK-free checks for Unity XR Hands + OpenXR.
    [MenuItem("Tools/Jemparingan/Check Quest Prerequisites")]
    public static void CheckQuestPrerequisites()
    {
        string path = "Packages/manifest.json";
        string manifest = File.Exists(path) ? File.ReadAllText(path) : "";
        int issues = 0;
        foreach (string dependency in new[]
        {
            "com.unity.xr.management", "com.unity.xr.openxr",
            "com.unity.xr.hands", "com.unity.xr.interaction.toolkit"
        })
        {
            if (manifest.Contains("\"" + dependency + "\"")) continue;
            issues++;
            Debug.LogWarning("XR prerequisite missing: " + dependency);
        }
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            Debug.LogWarning("Quest build target is not Android. Editor XRI playtest does not require Android.");
        Debug.Log("XR package preflight: " + issues +
            " missing dependencies. Android OpenXR hand features and device testing remain separate.");
    }

    [MenuItem("Tools/Jemparingan/Quest/Validate Active Scene")]
    public static void ValidateQuestScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Open the XR-only playtest scene first.");
            return;
        }
        var origin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        var game = Object.FindFirstObjectByType<JemparinganGame>();
        var hands = Object.FindFirstObjectByType<XRHandsBowInput>();
        bool hasSimulator = false;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "Jemparingan | XR Interaction Simulator" ||
                root.name == "XR Interaction Simulator")
                hasSimulator = true;
        if (origin == null || origin.Camera == null || game == null || hands == null || !hasSimulator)
        {
            Debug.LogError("XR scene incomplete: requires XROrigin, JemparinganGame, XRHandsBowInput and XR Interaction Simulator.");
            return;
        }
        Debug.Log("XR scene validation passed (static components only). Hand tracking and Quest APK are NOT verified.");
    }

    [MenuItem("Tools/Jemparingan/Create Prototype Scene")]
    public static void CreatePrototypeScene() => CreateOptimizedScene();
}
#endif

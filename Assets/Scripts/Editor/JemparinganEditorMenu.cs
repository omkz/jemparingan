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

    [MenuItem("Tools/Jemparingan/Create Prototype Scene")]
    public static void CreatePrototypeScene() => CreateOptimizedScene();
}
#endif

#if UNITY_EDITOR && META_XR_SDK
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Editor-only, synthetic regression checks. Never represents device hand testing.
public static class JemparinganQuestQA
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object Get(object obj, string field) => obj.GetType().GetField(field, Flags).GetValue(obj);
    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Flags).SetValue(obj, value);
    private static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Flags).Invoke(obj, args);
    private static void Save(string name, object value) => File.WriteAllText(JemparinganQuestSetup.Evidence + name,
        JsonConvert.SerializeObject(value, Formatting.Indented));

    [MenuItem("Tools/Jemparingan/Quest VR v1/Check Environment Preservation")]
    public static void Preservation()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run preservation checks in Edit Mode.");
        var quest = SceneManager.GetActiveScene();
        if (quest.path != JemparinganQuestSetup.QuestScene || quest.isDirty)
            throw new InvalidOperationException("Open the saved Quest scene first.");
        var desktop = EditorSceneManager.OpenScene(JemparinganQuestSetup.DesktopScene, OpenSceneMode.Additive);
        try
        {
            var source = Transforms(desktop);
            var copy = Transforms(quest);
            var errors = new List<string>();
            foreach (var pair in source)
            {
                if (!copy.TryGetValue(pair.Key, out var actual)) { errors.Add("Missing: " + pair.Key); continue; }
                var expected = pair.Value;
                if (expected.localPosition != actual.localPosition || expected.localRotation != actual.localRotation ||
                    expected.localScale != actual.localScale || expected.gameObject.activeSelf != actual.gameObject.activeSelf)
                    errors.Add("Transform/active state: " + pair.Key);
                var expectedRenderer = expected.GetComponent<Renderer>();
                var actualRenderer = actual.GetComponent<Renderer>();
                if (expectedRenderer != null && (actualRenderer == null ||
                    !expectedRenderer.sharedMaterials.SequenceEqual(actualRenderer.sharedMaterials)))
                    errors.Add("Materials: " + pair.Key);
                var expectedLight = expected.GetComponent<Light>();
                var actualLight = actual.GetComponent<Light>();
                if (expectedLight != null && (actualLight == null || expectedLight.type != actualLight.type ||
                    expectedLight.color != actualLight.color || expectedLight.intensity != actualLight.intensity || expectedLight.shadows != actualLight.shadows))
                    errors.Add("Light: " + pair.Key);
            }
            Save("environment-preservation.json", new { pass = errors.Count == 0, sourceTransforms = source.Count,
                copyTransforms = copy.Count, errors, method = "All original transform/active states, renderer material references and lights compared in saved scenes; added XR hierarchy excluded from original entries." });
            if (errors.Count > 0) throw new Exception("Quest preservation failed: " + string.Join(", ", errors));
            Debug.Log("Quest environment preservation PASS: " + source.Count + " original transforms/materials/lights unchanged.");
        }
        finally { EditorSceneManager.CloseScene(desktop, true); SceneManager.SetActiveScene(quest); }
    }

    private static Dictionary<string, Transform> Transforms(Scene scene)
    {
        var result = new Dictionary<string, Transform>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string key = "";
                for (var p = t; p != null; p = p.parent)
                    key = "/" + p.GetSiblingIndex() + ":" + p.name + key;
                result.Add(key, t);
            }
        return result;
    }

    [MenuItem("Tools/Jemparingan/Quest VR v1/Run Synthetic Runtime Checks")]
    public static async void RuntimeChecks()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != JemparinganQuestSetup.QuestScene)
            throw new InvalidOperationException("Enter Play Mode in the Quest scene first.");
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        var rows = new List<object>();
        bool pass = true;
        try
        {
            var game = Object.FindFirstObjectByType<JemparinganGame>();
            if (game == null || !(bool)Get(game, "_ready")) throw new Exception("Gameplay did not initialize.");
            game.ChooseMode(JemparinganGame.GameMode.Traditional);
            // Actual game Update must cancel its old draw when the real adapter
            // cannot supply tracked hands. No injected headset state is used.
            Set(game, "_wasDrawing", true);
            Set(game, "_wasBowHeld", true);
            Set(game, "_lastDraw", .4f);
            Set(game, "_drawStartedAt", Time.time - 1f);
            await Frames(3);
            bool canceled = (int)Get(game, "_shots") == 0 && !(bool)Get(game, "_wasDrawing");
            pass &= canceled;
            rows.Add(new { check = "Unavailable real hand data cancels pending draw with zero shots", pass = canceled });
            float assist = (float)Get(game, "aimAssist");
            Set(game, "aimAssist", 0f);
            try
            {
                foreach (var mode in new[] { JemparinganGame.GameMode.Traditional, JemparinganGame.GameMode.Modern })
                {
                    game.ChooseMode(mode);
                    for (int i = 0; i < 12; i++)
                    {
                        var target = mode == JemparinganGame.GameMode.Traditional
                            ? ((JemparinganTarget)Get(game, "_target")).transform
                            : ((JemparinganModernTarget[])Get(game, "_modernTargets"))[0].transform;
                        float x = mode == JemparinganGame.GameMode.Traditional ? (i % 4 == 3 ? .8f : 0f)
                            : new[] { 0f, .12f, .23f, .35f, .47f, .7f }[i % 6];
                        float y = mode == JemparinganGame.GameMode.Traditional ? new[] { .145f, .075f, -.1f, 0f }[i % 4] : 0f;
                        int expected = mode == JemparinganGame.GameMode.Traditional ? new[] { 3, 2, 1, 0 }[i % 4]
                            : new[] { 10, 8, 6, 4, 2, 0 }[i % 6];
                        int before = (int)Get(game, "_score");
                        Call(game, "Shoot", target.TransformPoint(new Vector3(x, y, -.8f)), target.forward, .4f, false);
                        float deadline = Time.realtimeSinceStartup + 8f;
                        while ((bool)Call(game, "AnyArrowFlying") && Time.realtimeSinceStartup < deadline) await Frames(1);
                        await Frames(3);
                        int delta = (int)Get(game, "_score") - before;
                        bool shotPass = delta == expected && (int)Get(game, "_shots") == i + 1;
                        pass &= shotPass;
                        rows.Add(new { mode = mode.ToString(), shot = i + 1, expected, actual = delta,
                            shots = Get(game, "_shots"), stage = (int)Get(game, "_stage") + 1, pass = shotPass });
                    }
                    bool finished = (bool)Get(game, "_finished");
                    pass &= finished;
                    rows.Add(new { mode = mode.ToString(), check = "12-shot round reaches results", pass = finished });
                }
            }
            finally { Set(game, "aimAssist", assist); }
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.enabled).ToArray();
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Where(l => l.enabled).ToArray();
            var badMaterials = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .SelectMany(r => r.sharedMaterials).Where(m => m != null && (m.shader == null || !m.shader.isSupported || m.shader.name == "Hidden/InternalErrorShader"))
                .Select(m => m.name).Distinct().ToArray();
            bool renderPass = cameras.Length == 1 && cameras[0].CompareTag("MainCamera") && listeners.Length == 1 && badMaterials.Length == 0;
            pass &= renderPass;
            rows.Add(new { check = "Runtime one camera/listener and supported materials", pass = renderPass,
                cameras = cameras.Select(c => c.name).ToArray(), listeners = listeners.Length, badMaterials });
            Save("runtime-checks.json", new { pass, method = "Editor Play Mode, real projectile Update/collision/scoring; near-target Shoot injection bypasses gestures and aiming. No device/stereo/performance claim.", rows });
            Debug.Log("Quest synthetic runtime checks " + (pass ? "PASS" : "FAIL") + ". Not a headset input test.");
        }
        catch (Exception e)
        {
            Save("runtime-checks.json", new { pass = false, error = e.ToString(), rows });
            Debug.LogException(e);
        }
        finally { Application.runInBackground = background; }
    }

    private static async Task Frames(int count)
    {
        int frame = Time.frameCount;
        float deadline = Time.realtimeSinceStartup + 10f;
        while (EditorApplication.isPlaying && Time.frameCount < frame + count && Time.realtimeSinceStartup < deadline)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            await Task.Delay(15);
        }
        if (!EditorApplication.isPlaying || Time.frameCount < frame + count) throw new Exception("Play Mode did not advance.");
    }
}
#endif

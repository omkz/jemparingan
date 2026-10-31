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

    [MenuItem("Tools/Jemparingan/Create Prototype Scene")]
    public static void CreatePrototypeScene() => CreateOptimizedScene();
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Intentionally minimal: no OVRManager, tracked hands, gameplay or Joglo.
// If even this scene cannot submit an XR frame, investigate the host/runtime
// graphics path before changing Jemparingan gameplay or renderer settings.
public static class JemparinganOpenXRSmoke
{
    public const string ScenePath = "Assets/Jemparingan/Scenes/JemparinganOpenXRSmoke.unity";

    [MenuItem("Tools/Jemparingan/Quest VR v1/Create/Open Minimal OpenXR Smoke Scene")]
    public static void CreateOrOpen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and compilation before opening the smoke scene.");

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save your open scene changes before creating the smoke scene.");
        }

        // Reopening the diagnostic must not overwrite any existing scene edits.
        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("Opened existing OpenXR smoke scene. Activate Meta XR Simulator, then press Play.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Smoke | XR camera (no tracking input)");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 1.35f, 0f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.16f, 0.24f, 1f);
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 100f;
        camera.stereoTargetEye = StereoTargetEyeMask.Both;
        camera.GetUniversalAdditionalCameraData().allowXRRendering = true;
        cameraObject.AddComponent<AudioListener>();

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Smoke | cube 3m ahead";
        cube.transform.position = new Vector3(0f, 1.35f, 3f);
        cube.transform.localScale = Vector3.one * 0.75f;

        // Use a project URP material, not the default Built-in pipeline material.
        var material = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Resources/Jemparingan/RuntimeLit.mat");
        if (material == null)
            throw new InvalidOperationException("RuntimeLit.mat missing; cannot create URP smoke cube.");
        cube.GetComponent<MeshRenderer>().sharedMaterial = material;

        var lightObject = new GameObject("Smoke | directional light");
        lightObject.transform.rotation = Quaternion.Euler(45f, -25f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new IOException("Could not save OpenXR smoke scene to " + ScenePath);

        Debug.Log("Created " + ScenePath +
            ". Use Configure Editor Simulator OpenXR, activate simulator, and Play. " +
            "This tests frame submission ONLY; no tracking or gestures are tested.");
    }
}
#endif

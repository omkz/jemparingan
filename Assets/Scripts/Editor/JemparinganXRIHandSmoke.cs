#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Editor-only smoke test. It deliberately does not use OpenXR startup,
// Meta XR Simulator, OVRManager or the Jemparingan Quest gameplay scene.
public static class JemparinganXRIHandSmoke
{
    public const string ScenePath = "Assets/Jemparingan/Scenes/JemparinganXRIHandSmoke.unity";

    [MenuItem("Tools/Jemparingan/Simulator/Create/Open XRI Hand Smoke Scene")]
    public static void CreateOrOpen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            Debug.LogError("Stop Play Mode and wait for compilation before opening the XRI smoke scene.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            if (!EnsureScenesSaved()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("Opened XRI Hand Smoke scene. Keep Meta XR Simulator closed and press Play.");
            return;
        }

        // Import these two official samples using Package Manager > XR Interaction Toolkit > Samples.
        // Only accept prefabs from the versioned XRI sample folders, never similarly named Meta prefabs.
        var originPrefab = FindSamplePrefab("Starter Assets", "XR Origin (XR Rig)");
        var simulatorPrefab = FindSamplePrefab("XR Device Simulator", "XR Interaction Simulator")
            ?? FindSamplePrefab("XR Interaction Simulator", "XR Interaction Simulator");

        if (originPrefab == null || simulatorPrefab == null)
        {
            Debug.LogError(
                "Required XRI sample prefabs are missing. In Window > Package Manager > XR Interaction Toolkit > Samples, " +
                "import 'Starter Assets' and the simulator sample ('XR Device Simulator' or 'XR Interaction Simulator'). " +
                "Then run this menu item again. No scene has been changed.");
            return;
        }

        if (!EnsureScenesSaved()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var origin = (GameObject)PrefabUtility.InstantiatePrefab(originPrefab);
        var simulator = (GameObject)PrefabUtility.InstantiatePrefab(simulatorPrefab);
        origin.name = "XRI Smoke | XR Origin (official sample)";
        simulator.name = "XRI Smoke | XR Interaction Simulator (official sample)";

        if (origin.GetComponentsInChildren<Camera>(true).Length == 0)
        {
            Debug.LogError("The imported XR Origin prefab contains no camera. Verify Starter Assets in Package Manager.");
            return;
        }

        if (UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>() == null)
            new GameObject("XRI Smoke | Interaction Manager").AddComponent<XRInteractionManager>();

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "XRI Smoke | grab cube";
        cube.transform.position = new Vector3(0f, 1.35f, 1f);
        cube.transform.localScale = Vector3.one * 0.22f;
        var cubeBody = cube.AddComponent<Rigidbody>();
        cubeBody.useGravity = false;
        cubeBody.constraints = RigidbodyConstraints.FreezeRotation;
        cube.AddComponent<XRGrabInteractable>();

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "XRI Smoke | floor";
        floor.transform.position = new Vector3(0f, -0.05f, 1f);
        floor.transform.localScale = new Vector3(5f, 0.1f, 5f);

        // Reuse an existing URP material so primitives do not appear pink.
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Jemparingan/RuntimeLit.mat");
        if (material != null)
        {
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            floor.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        var lightObject = new GameObject("XRI Smoke | directional light");
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError("Could not save scene: " + ScenePath);
            return;
        }

        Debug.Log("Created XRI smoke scene at " + ScenePath +
            ". Close Meta XR Simulator, press Play, use the XRI simulator controls to switch to Hands and test Pinch. " +
            "This does NOT validate OVRHand input or Quest device rendering.");
    }

    private static GameObject FindSamplePrefab(string sampleFolder, string prefabName)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
            if (!path.StartsWith("Assets/Samples/XR Interaction Toolkit/", StringComparison.OrdinalIgnoreCase) ||
                path.IndexOf("/" + sampleFolder + "/", StringComparison.OrdinalIgnoreCase) < 0 ||
                !string.Equals(Path.GetFileNameWithoutExtension(path), prefabName, StringComparison.OrdinalIgnoreCase))
                continue;

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    private static bool EnsureScenesSaved()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                Debug.LogError("Save your current scene changes before creating or opening the XRI smoke scene.");
                return false;
            }
        }
        return true;
    }
}
#endif

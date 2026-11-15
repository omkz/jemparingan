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
    private const string OldRigName = "XRI Smoke | XR Origin (official sample)";
    private const string HandsRigName = "XRI Smoke | XR Hands Setup (official sample)";

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

        // The Starter Assets origin is controller-only. The Hands Interaction Demo setup
        // provides the hand modality, interactors and hand visualizer for this test.
        // Only accept prefabs from versioned XRI Samples, never similarly named Meta prefabs.
        var originPrefab = FindSamplePrefab("Hands Interaction Demo", "XR Interaction Hands Setup");
        var simulatorPrefab = FindSamplePrefab("XR Device Simulator", "XR Interaction Simulator")
            ?? FindSamplePrefab("XR Interaction Simulator", "XR Interaction Simulator");

        if (originPrefab == null || simulatorPrefab == null)
        {
            Debug.LogError(
                "Missing XRI prefabs: " +
                (originPrefab == null ? "[XR Interaction Hands Setup in Hands Interaction Demo] " : "") +
                (simulatorPrefab == null ? "[XR Interaction Simulator prefab] " : "") +
                ". Open Window > Package Manager > In Project > XR Interaction Toolkit > Samples, " +
                "and import 'Starter Assets', 'Hands Interaction Demo', and 'XR Interaction Simulator' (not Legacy). " +
                "Also import XR Hands > Samples > HandVisualizer. " +
                "Check Assets/Samples/XR Interaction Toolkit/<version>/ for the imported prefabs, " +
                "then run this menu again. No scene was changed.");
            return;
        }

        if (!EnsureScenesSaved()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var origin = (GameObject)PrefabUtility.InstantiatePrefab(originPrefab);
        var simulator = (GameObject)PrefabUtility.InstantiatePrefab(simulatorPrefab);
        origin.name = HandsRigName;
        simulator.name = "XRI Smoke | XR Interaction Simulator (official sample)";

        if (origin.GetComponentsInChildren<Camera>(true).Length == 0)
        {
            Debug.LogError("The imported XR Interaction Hands Setup has no camera. Verify Hands Interaction Demo sample.");
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

        Debug.Log("Created hands-enabled XRI smoke scene at " + ScenePath +
            ". Close Meta XR Simulator, press Play, use the XRI simulator controls to switch to Hands and test Pinch. " +
            "This does NOT validate OVRHand input or Quest device rendering.");
    }

    // Existing generated scenes retain their old controller-only rig when reopened.
    // This upgrade explicitly replaces ONLY that rig, preserving the cube, floor,
    // simulator and the rest of the separate smoke scene.
    [MenuItem("Tools/Jemparingan/Simulator/Upgrade Open XRI Smoke Scene to Hands")]
    public static void UpgradeOpenSceneToHands()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            Debug.LogError("Stop Play Mode and compilation before upgrading the XRI smoke scene.");
            return;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError("Open " + ScenePath + " first; this action cannot modify other scenes.");
            return;
        }

        if (!EnsureScenesSaved()) return;

        var handsSetupPrefab = FindSamplePrefab("Hands Interaction Demo", "XR Interaction Hands Setup");
        if (handsSetupPrefab == null)
        {
            Debug.LogError("XR Interaction Hands Setup prefab missing. Import XR Interaction Toolkit > Samples > " +
                "Hands Interaction Demo and XR Hands > Samples > HandVisualizer. Scene unchanged.");
            return;
        }

        GameObject oldRig = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == HandsRigName)
            {
                Debug.Log("The XRI smoke scene already uses the hands-enabled setup. " +
                    "Activate hand mode in the simulator and try M for pinch.");
                return;
            }

            if (root.name == OldRigName)
                oldRig = root;
        }

        if (oldRig == null)
        {
            Debug.LogError("The controller-only smoke rig could not be found. Scene unchanged.");
            return;
        }

        var newRig = (GameObject)PrefabUtility.InstantiatePrefab(handsSetupPrefab);
        if (newRig == null || newRig.GetComponentsInChildren<Camera>(true).Length == 0)
        {
            if (newRig != null) UnityEngine.Object.DestroyImmediate(newRig);
            Debug.LogError("Hands setup prefab has no usable XR camera. Scene unchanged.");
            return;
        }

        newRig.name = HandsRigName;
        UnityEngine.Object.DestroyImmediate(oldRig);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError("Replaced controller rig, but saving the smoke scene failed. Save it manually.");
            return;
        }

        Debug.Log("Upgraded XRI smoke scene to the official XR Interaction Hands Setup. " +
            "Press Play; select a hand in the XR Interaction Simulator, then use M for pinch. " +
            "This does not exercise OVRHand or Quest device input.");
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

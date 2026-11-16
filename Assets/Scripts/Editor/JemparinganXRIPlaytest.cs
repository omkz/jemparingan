#if UNITY_EDITOR
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

// XR-only Editor playtest; never modifies desktop, Quest or legacy simulator scenes.
public static class JemparinganXRIPlaytest
{
    private const string SourceScene = "Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity";
    private const string PlaytestScene = "Assets/Jemparingan/Scenes/JemparinganXRIOnly.unity";

    [MenuItem("Tools/Jemparingan/Simulator/Create/Open Jemparingan XRI Playtest")]
    public static void CreateOrOpen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            !JemparinganXRIHandSmoke.EnsureScenesSaved())
        {
            Debug.LogError("Stop Play Mode and save open scenes first.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PlaytestScene) != null)
        {
            Scene existing = EditorSceneManager.OpenScene(PlaytestScene, OpenSceneMode.Single);
            var input = Object.FindFirstObjectByType<XRHandsBowInput>(FindObjectsInactive.Include);
            var simulator = Object.FindFirstObjectByType<
                UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator>(
                    FindObjectsInactive.Include);
            if (input == null || simulator == null)
                Debug.LogError("XR-only scene is incomplete. Existing scene left untouched.");
            else
                Debug.Log("XRI PLAYTEST READY: opened existing XR-only scene.");
            return;
        }

        var handsPrefab = JemparinganXRIHandSmoke.FindSamplePrefab(
            "Hands Interaction Demo", "XR Interaction Hands Setup")
            ?? JemparinganXRIHandSmoke.FindSamplePrefab(
                "Hands Interaction Demo", "XR Origin Hands (XR Rig)");
        var simulatorPrefab = JemparinganXRIHandSmoke.FindSamplePrefab(
            "XR Interaction Simulator", "XR Interaction Simulator")
            ?? JemparinganXRIHandSmoke.FindSamplePrefab(
                "XR Device Simulator", "XR Interaction Simulator");
        if (handsPrefab == null || simulatorPrefab == null)
        {
            Debug.LogError("Import XRI 3.5 samples: Hands Interaction Demo and XR Interaction Simulator. No scene changed.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScene) == null ||
            !AssetDatabase.CopyAsset(SourceScene, PlaytestScene))
        {
            Debug.LogError("Could not copy desktop scene. Source scene untouched.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(PlaytestScene, OpenSceneMode.Single);
        var game = Object.FindFirstObjectByType<JemparinganGame>();
        Camera desktopCamera = Camera.main;
        if (game == null || desktopCamera == null)
        {
            Debug.LogError("Copied scene needs JemparinganGame and MainCamera. XR-only scene not configured.");
            return;
        }

        Vector3 position = desktopCamera.transform.position;
        Quaternion rotation = desktopCamera.transform.rotation;
        desktopCamera.gameObject.SetActive(false);

        var hands = (GameObject)PrefabUtility.InstantiatePrefab(handsPrefab, scene);
        hands.name = "Jemparingan | XRI Hands Rig";
        hands.transform.SetPositionAndRotation(position, rotation);
        var origin = hands.GetComponentInChildren<XROrigin>(true);
        if (origin == null || origin.Camera == null || origin.CameraFloorOffsetObject == null)
        {
            Debug.LogError("Imported XR Hands prefab has no complete XR Origin.");
            return;
        }
        origin.CameraYOffset = 0f;
        origin.Camera.tag = "MainCamera";
        origin.Camera.gameObject.SetActive(true);

        var simulator = (GameObject)PrefabUtility.InstantiatePrefab(simulatorPrefab, scene);
        simulator.name = "Jemparingan | XR Interaction Simulator";
        if (Object.FindFirstObjectByType<XRInteractionManager>(FindObjectsInactive.Include) == null)
            new GameObject("Jemparingan | XR Interaction Manager").AddComponent<XRInteractionManager>();

        var desktopInput = game.GetComponent<DesktopBowInput>();
        if (desktopInput != null) desktopInput.enabled = false;
        var legacyMetaInput = game.GetComponent<MetaQuestBowInput>();
        if (legacyMetaInput != null) legacyMetaInput.enabled = false;

        var xrInput = game.GetComponent<XRHandsBowInput>();
        if (xrInput == null) xrInput = game.gameObject.AddComponent<XRHandsBowInput>();
        xrInput.enabled = true;
        xrInput.SetTrackingOrigin(origin.CameraFloorOffsetObject.transform);
        EditorUtility.SetDirty(xrInput);

        var serializedGame = new SerializedObject(game);
        serializedGame.FindProperty("preferQuestHandsInEditor").boolValue = true;
        serializedGame.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, PlaytestScene))
        {
            Debug.LogError("XR-only scene setup could not be saved.");
            return;
        }

        Debug.Log("XRI PLAYTEST READY: XR-only scene created from desktop. Original scenes untouched.");
    }
}
#endif

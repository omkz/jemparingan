#if UNITY_EDITOR
using System;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

// Create a separate, playable Jemparingan copy with XRI simulated hands.
// Never write changes back to the Quest source scene.
public static class JemparinganXRIPlaytest
{
    private const string QuestScene = "Assets/Jemparingan/Scenes/JemparinganQuest.unity";
    private const string PlaytestScene = "Assets/Jemparingan/Scenes/JemparinganXRIPlaytest.unity";

    [MenuItem("Tools/Jemparingan/Simulator/Create/Open Jemparingan XRI Playtest")]
    public static void CreateOrOpen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            !JemparinganXRIHandSmoke.EnsureScenesSaved())
        {
            Debug.LogError("Stop Play and save all scene changes before creating the XRI playtest.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PlaytestScene) != null)
        {
            EditorSceneManager.OpenScene(PlaytestScene, OpenSceneMode.Single);
            Debug.Log("Opened existing XRI Jemparingan playtest. It was not overwritten.");
            return;
        }

        GameObject handsPrefab = JemparinganXRIHandSmoke.FindSamplePrefab(
            "Hands Interaction Demo", "XR Interaction Hands Setup")
            ?? JemparinganXRIHandSmoke.FindSamplePrefab(
                "Hands Interaction Demo", "XR Origin Hands (XR Rig)");
        GameObject simulatorPrefab = JemparinganXRIHandSmoke.FindSamplePrefab(
            "XR Device Simulator", "XR Interaction Simulator")
            ?? JemparinganXRIHandSmoke.FindSamplePrefab(
                "XR Interaction Simulator", "XR Interaction Simulator");
        if (handsPrefab == null || simulatorPrefab == null)
        {
            Debug.LogError("Missing XRI prefabs: " +
                (handsPrefab == null ? "[Hands Interaction Demo: XR Origin Hands (XR Rig)] " : "") +
                (simulatorPrefab == null ? "[XR Interaction Simulator prefab] " : "") +
                ". Import the named sample(s) in Package Manager > XR Interaction Toolkit > Samples. " +
                "No scene was changed.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(QuestScene) == null)
        {
            Debug.LogError("Quest source scene missing: " + QuestScene);
            return;
        }

        Scene source = EditorSceneManager.OpenScene(QuestScene, OpenSceneMode.Single);
        if (UnityEngine.Object.FindFirstObjectByType<JemparinganGame>() == null ||
            UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>() == null)
        {
            Debug.LogError("Quest scene needs JemparinganGame and OVRCameraRig. No source scene changes were saved.");
            return;
        }

        if (!EditorSceneManager.SaveScene(source, PlaytestScene, true))
        {
            Debug.LogError("Could not create playtest scene copy.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(PlaytestScene, OpenSceneMode.Single);
        var game = UnityEngine.Object.FindFirstObjectByType<JemparinganGame>();
        var oldRig = UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>();
        if (game == null || oldRig == null || game.gameObject == oldRig.gameObject)
        {
            Debug.LogError("Could not safely replace the Quest rig in the copied scene.");
            return;
        }

        Vector3 originPosition = oldRig.transform.position;
        Quaternion originRotation = oldRig.transform.rotation;
        if (game.transform.IsChildOf(oldRig.transform))
            game.transform.SetParent(null, true);
        UnityEngine.Object.DestroyImmediate(oldRig.gameObject);

        var hands = (GameObject)PrefabUtility.InstantiatePrefab(handsPrefab);
        hands.name = "Jemparingan | XRI Hands Rig";
        hands.transform.SetPositionAndRotation(originPosition, originRotation);
        var origin = hands.GetComponentInChildren<XROrigin>(true);
        if (origin == null || origin.Camera == null || origin.CameraFloorOffsetObject == null)
        {
            Debug.LogError("XR Interaction Hands Setup sample has an incomplete XR Origin. Playtest copy not saved.");
            return;
        }
        // The authored Quest viewpoint is already seated at Y=1.35 m.
        // Do not add the sample rig's standing-height camera offset again.
        origin.CameraYOffset = 0f;
        origin.Camera.tag = "MainCamera";

        var simulator = (GameObject)PrefabUtility.InstantiatePrefab(simulatorPrefab);
        simulator.name = "Jemparingan | XR Interaction Simulator";

        if (UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>() == null)
            new GameObject("Jemparingan | XR Interaction Manager").AddComponent<XRInteractionManager>();

        var xrInput = game.GetComponent<XRHandsBowInput>();
        if (xrInput == null) xrInput = game.gameObject.AddComponent<XRHandsBowInput>();
        xrInput.SetTrackingOrigin(origin.CameraFloorOffsetObject.transform);

        var questInput = game.GetComponent<MetaQuestBowInput>();
        if (questInput != null) questInput.enabled = false;

        var serializedGame = new SerializedObject(game);
        var preference = serializedGame.FindProperty("preferQuestHandsInEditor");
        if (preference == null)
        {
            Debug.LogError("The gameplay input preference field was not found. Playtest copy not saved.");
            return;
        }
        preference.boolValue = true;
        serializedGame.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, PlaytestScene))
        {
            Debug.LogError("Could not save the XRI playtest scene.");
            return;
        }

        Debug.Log("Created XRI Jemparingan playtest scene. Quest source scene preserved. " +
            "Close Meta XR Simulator, press Play, select both simulated Hands, " +
            "pinch left to hold bow and right to draw/release.");
    }
}
#endif

#if UNITY_EDITOR && META_XR_SDK
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using Object = UnityEngine.Object;

// Modifies only the copied Quest scene. Never calls the environment generator.
public static class JemparinganQuestSetup
{
    public const string DesktopScene = "Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity";
    public const string QuestScene = "Assets/Jemparingan/Scenes/JemparinganQuest.unity";
    public const string Evidence = "docs/validation/quest-vr-v1/";
    private const string Core = "Packages/com.meta.xr.sdk.core/";

    [MenuItem("Tools/Jemparingan/Quest VR v1/Configure Android OpenXR")]
    public static void ConfigureAndroid()
    {
        var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if (general == null) throw new InvalidOperationException("Android XR settings are missing.");
        var manager = general.Manager;
        if (!XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android))
            throw new InvalidOperationException("OpenXR loader assignment failed.");
        general.InitManagerOnStart = true;
        EditorUtility.SetDirty(general);
        EditorUtility.SetDirty(manager);
        // Desktop players keep ordinary mouse/keyboard startup. The editor hook
        // creates transient XR settings only when playing the Quest scene.
        var standalone = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (standalone != null) { standalone.InitManagerOnStart = false; EditorUtility.SetDirty(standalone); }

        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        foreach (var feature in settings.GetFeatures())
        {
            // Meta Core owns the runtime integration. Do not also enable Unity's
            // competing MetaQuestFeature or any controller interaction profiles.
            feature.enabled = new[] { "MetaXRFeature", "MetaXRFoveationFeature", "HandTracking", "MetaHandTrackingAim", "HandInteractionProfile" }
                .Contains(feature.GetType().Name);
            EditorUtility.SetDirty(feature);
        }
        EditorUtility.SetDirty(settings);
        var config = OVRProjectConfig.CachedProjectConfig;
        config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.HandsOnly;
        config.targetDeviceTypes = new List<OVRProjectConfig.DeviceType> { OVRProjectConfig.DeviceType.Quest3, OVRProjectConfig.DeviceType.Quest3S };
        config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.None;
        config.sceneSupport = OVRProjectConfig.FeatureSupport.None;
        config.bodyTrackingSupport = OVRProjectConfig.FeatureSupport.None;
        config.faceTrackingSupport = OVRProjectConfig.FeatureSupport.None;
        config.eyeTrackingSupport = OVRProjectConfig.FeatureSupport.None;
        EditorUtility.SetDirty(config);
        OVRRuntimeSettings.Instance.HandSkeletonVersion = OVRHandSkeletonVersion.OpenXR;
        EditorUtility.SetDirty(OVRRuntimeSettings.Instance);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.omkz.jemparingan");
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        // Both backends together cause an unsupported-input modal on Android.
        // DesktopBowInput uses the small Input System-compatible controls bridge.
        var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        playerSettings.FindProperty("activeInputHandler").intValue = 1;
        playerSettings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        Write("configuration.json", Configuration());
        Debug.Log("Quest VR: Android OpenXR configured, hands only, immersive VR (no passthrough).");
    }

    [MenuItem("Tools/Jemparingan/Quest VR v1/Configure Editor Simulator OpenXR")]
    public static void ConfigureEditorSimulator()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before configuring simulator XR.");
        var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (general == null || general.Manager == null) throw new InvalidOperationException("Standalone XR settings are missing.");
        if (!XRPackageMetadataStore.AssignLoader(general.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Standalone))
            throw new InvalidOperationException("Standalone OpenXR loader assignment failed.");
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
        settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        foreach (var feature in settings.GetFeatures())
        {
            if (!new[] { "MetaXRFeature", "MetaXRFoveationFeature", "HandTracking", "MetaHandTrackingAim", "HandInteractionProfile" }.Contains(feature.GetType().Name)) continue;
            feature.enabled = true;
            EditorUtility.SetDirty(feature);
        }
        EditorUtility.SetDirty(general.Manager);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("Quest Editor simulator configured: Standalone OpenXR and hand features enabled. Quest-scene XR startup is transient; activate Meta XR Simulator before Play.");
    }

    [MenuItem("Tools/Jemparingan/Quest VR v1/Create Quest Scene Copy")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop play/compilation before creating the Quest scene.");
        for (int i = 0; i < SceneManager.sceneCount; ++i)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("An open scene has unsaved changes; save it first.");
        if (File.Exists(QuestScene)) throw new InvalidOperationException("Quest scene already exists. Validate it; do not overwrite it.");
        if (!AssetDatabase.CopyAsset(DesktopScene, QuestScene)) throw new IOException("Scene duplication failed.");
        var scene = EditorSceneManager.OpenScene(QuestScene, OpenSceneMode.Single);
        var desktopCamera = Camera.main;
        if (desktopCamera == null) throw new InvalidOperationException("Authored seated camera is missing.");
        Vector3 seatPosition = desktopCamera.transform.position;
        Quaternion seatRotation = desktopCamera.transform.rotation;
        var game = Object.FindFirstObjectByType<JemparinganGame>();
        if (game == null) throw new InvalidOperationException("Existing gameplay is missing.");

        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            camera.enabled = false;
            camera.tag = "Untagged";
        }
        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            listener.enabled = false;
        foreach (var input in Object.FindObjectsByType<DesktopBowInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            input.enabled = false;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Core + "Prefabs/OVRCameraRig.prefab");
        var rigObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        rigObject.name = "Quest VR | seated OVRCameraRig";
        // Device/EyeLevel origin: initial tracked eye position is zero. The rig
        // carries the authored 1.35m eye height; never add floor height twice.
        rigObject.transform.SetPositionAndRotation(seatPosition, seatRotation);
        var rig = rigObject.GetComponent<OVRCameraRig>();
        rig.usePerEyeCameras = false;
        var manager = rigObject.GetComponent<OVRManager>();
        manager.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
        manager.monoscopic = false;
        manager.usePositionTracking = true;
        manager.useRotationTracking = true;
        manager.resetTrackerOnLoad = false;
        manager.isInsightPassthroughEnabled = false;
        foreach (var camera in rigObject.GetComponentsInChildren<Camera>(true))
        {
            bool center = camera.transform == rig.centerEyeAnchor;
            camera.enabled = center;
            camera.tag = center ? "MainCamera" : "Untagged";
            camera.nearClipPlane = .05f;
            camera.farClipPlane = desktopCamera.farClipPlane;
            camera.clearFlags = desktopCamera.clearFlags;
            camera.backgroundColor = desktopCamera.backgroundColor;
            var urp = camera.GetUniversalAdditionalCameraData();
            urp.allowXRRendering = center;
            urp.renderPostProcessing = false;
            urp.renderType = CameraRenderType.Base;
        }
        foreach (var listener in rigObject.GetComponentsInChildren<AudioListener>(true))
            Object.DestroyImmediate(listener);
        rig.centerEyeAnchor.gameObject.AddComponent<AudioListener>();

        var handMaterial = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Jemparingan/RuntimeLit.mat"));
        handMaterial.name = "Quest Tracked Hands URP";
        handMaterial.SetColor("_BaseColor", new Color(.78f, .59f, .40f, 1f));
        AssetDatabase.CreateAsset(handMaterial, "Assets/Settings/JemparinganURP/QuestTrackedHands.mat");
        var left = CreateHand(rig.leftHandAnchor, false, handMaterial);
        var right = CreateHand(rig.rightHandAnchor, true, handMaterial);
        var adapter = game.GetComponent<MetaQuestBowInput>();
        if (adapter == null) adapter = game.gameObject.AddComponent<MetaQuestBowInput>();
        adapter.leftHand = left;
        adapter.rightHand = right;
        adapter.enabled = true;
        var serializedGame = new SerializedObject(game);
        serializedGame.FindProperty("questInput").objectReferenceValue = adapter;
        serializedGame.FindProperty("preferQuestHandsInEditor").boolValue = true;
        serializedGame.FindProperty("modernEveryShotArrowCam").boolValue = false;
        serializedGame.ApplyModifiedPropertiesWithoutUndo();
        // Preserve the desktop default build list. Android build method below
        // uses an explicit one-scene list and cannot boot the desktop scene.
        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.Add(new EditorBuildSettingsScene(QuestScene, false));
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Validate();
    }

    private static OVRHand CreateHand(Transform parent, bool right, Material material)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Core + "Prefabs/OVRHandPrefab.prefab");
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.name = right ? "Right tracked hand | string pinch" : "Left tracked hand | bow pinch";
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        // SDK setters are internal; serialize the same fields as Meta's official
        // HandTrackingBlockData installer, with matching OpenXR mesh/skeleton.
        SetInt(obj.GetComponent<OVRHand>(), "HandType", right ? 1 : 0);
        SetInt(obj.GetComponent<OVRSkeleton>(), "_skeletonType", (int)(right ? OVRSkeleton.SkeletonType.XRHandRight : OVRSkeleton.SkeletonType.XRHandLeft));
        SetInt(obj.GetComponent<OVRMesh>(), "_meshType", (int)(right ? OVRMesh.MeshType.XRHandRight : OVRMesh.MeshType.XRHandLeft));
        var renderer = obj.GetComponent<SkinnedMeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return obj.GetComponent<OVRHand>();
    }

    private static void SetInt(Object obj, string property, int value)
    {
        var so = new SerializedObject(obj);
        var p = so.FindProperty(property);
        if (p == null) throw new InvalidOperationException("SDK property missing: " + property);
        p.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static object Configuration()
    {
        var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        var issues = new List<OpenXRFeature.ValidationRule>();
        OpenXRProjectValidation.GetCurrentValidationIssues(issues, BuildTargetGroup.Android);
        return new {
            unity = Application.unityVersion, activeTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
            androidSupported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android),
            loaders = general.Manager.activeLoaders.Select(l => l.GetType().FullName).ToArray(),
            autoInitialize = general.InitManagerOnStart, renderMode = settings.renderMode.ToString(),
            features = settings.GetFeatures().Where(f => f.enabled).Select(f => f.GetType().FullName).ToArray(),
            architecture = PlayerSettings.Android.targetArchitectures.ToString(),
            backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android).ToString(),
            minApi = (int)PlayerSettings.Android.minSdkVersion,
            graphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Select(a => a.ToString()).ToArray(),
            activeInputHandling = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]).FindProperty("activeInputHandler").intValue,
            handSupport = OVRProjectConfig.CachedProjectConfig.handTrackingSupport.ToString(),
            passthrough = OVRProjectConfig.CachedProjectConfig.insightPassthroughSupport.ToString(),
            validation = issues.Select(i => new { i.message, i.error, i.fixItAutomatic }).ToArray()
        };
    }

    [MenuItem("Tools/Jemparingan/Quest VR v1/Validate Open Quest Scene")]
    public static void Validate()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != QuestScene) throw new InvalidOperationException("Open the Quest scene first.");
        var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.enabled).ToArray();
        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Where(l => l.enabled).ToArray();
        var rigs = Object.FindObjectsByType<OVRCameraRig>(FindObjectsSortMode.None);
        var hands = Object.FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
        var adapter = Object.FindFirstObjectByType<MetaQuestBowInput>();
        var transforms = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
        int missingScripts = transforms.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        var baseline = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Evidence + "desktop-baseline.json"));
        string expectedHash = (string)baseline["sha256"];
        string actualHash;
        using (var sha = SHA256.Create())
            actualHash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(DesktopScene))).Replace("-", "").ToLowerInvariant();
        string normalizedHash;
        using (var sha = SHA256.Create())
            normalizedHash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(DesktopScene).Replace("\r\n", "\n")))).Replace("-", "").ToLowerInvariant();
        bool desktopUnchanged = actualHash == expectedHash || normalizedHash == (string)baseline["sha256Lf"];
        var issues = new List<OpenXRFeature.ValidationRule>();
        OpenXRProjectValidation.GetCurrentValidationIssues(issues, BuildTargetGroup.Android);
        bool pass = cameras.Length == 1 && cameras[0].CompareTag("MainCamera") &&
            cameras[0].GetUniversalAdditionalCameraData().allowXRRendering &&
            listeners.Length == 1 && listeners[0].transform == cameras[0].transform &&
            rigs.Length == 1 && !rigs[0].usePerEyeCameras && !rigs[0].GetComponent<OVRManager>().monoscopic && hands.Length == 2 &&
            adapter != null && adapter.leftHand != null && adapter.rightHand != null && adapter.leftHand != adapter.rightHand &&
            !Object.FindObjectsByType<DesktopBowInput>(FindObjectsSortMode.None).Any(d => d.enabled) &&
            Object.FindObjectsByType<OVRPassthroughLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0 &&
            !rigs[0].GetComponent<OVRManager>().isInsightPassthroughEnabled && missingScripts == 0 && desktopUnchanged &&
            !issues.Any(i => i.error);
        Write("scene-validation.json", new {
            pass, scene = scene.path, cameras = cameras.Select(c => c.name).ToArray(), audioListeners = listeners.Length,
            rigs = rigs.Length, hands = hands.Select(h => h.name).ToArray(), missingScripts, desktopUnchanged,
            trackingOrigin = rigs.FirstOrDefault()?.GetComponent<OVRManager>().trackingOriginType.ToString(),
            seat = rigs.FirstOrDefault()?.transform.position.ToString(), configuration = Configuration()
        });
        if (!pass) throw new InvalidOperationException("Quest validation failed: see " + Evidence + "scene-validation.json");
        Debug.Log("Quest VR v1 scene validation PASS: one stereo rig/listener, two hands, desktop preserved, no passthrough.");
    }

    [MenuItem("Tools/Jemparingan/Quest VR v1/Build Android Development APK")]
    public static void BuildAndroid()
    {
        Validate();
        Directory.CreateDirectory("Builds/QuestVRv1");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { QuestScene }, target = BuildTarget.Android,
            locationPathName = "Builds/QuestVRv1/JemparinganQuest-dev.apk", options = BuildOptions.Development
        });
        Write("android-build.json", new {
            result = report.summary.result.ToString(), errors = report.summary.totalErrors,
            warnings = report.summary.totalWarnings, bytes = report.summary.totalSize,
            duration = report.summary.totalTime.ToString(), output = report.summary.outputPath,
            messages = report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception)
                .Select(m => m.content).ToArray()
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Android build failed; see evidence.");
    }

    private static void Write(string name, object data)
    {
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + name, JsonConvert.SerializeObject(data, Formatting.Indented));
    }
}
#endif

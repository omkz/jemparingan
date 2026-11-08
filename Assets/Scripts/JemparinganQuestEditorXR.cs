#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

// Editor Play Mode always uses Standalone XR settings, even with Android selected.
// Start XR for the Quest scene using transient settings, so desktop scenes and
// desktop player builds retain their stored non-XR startup configuration.
[InitializeOnLoad]
public static class JemparinganQuestEditorXR
{
    private const string QuestScene = "Assets/Jemparingan/Scenes/JemparinganQuest.unity";
    private const string QuestPlayKey = "Jemparingan.QuestEditorXR.Requested";
    private static XRGeneralSettings _original;
    private static XRGeneralSettings _session;
    private static XRManagerSettings _manager;

    static JemparinganQuestEditorXR()
    {
        EditorApplication.playModeStateChanged += PlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload += StopSession;
        EditorApplication.quitting += StopSession;
    }

    private static void PlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            string startScene = EditorSceneManager.playModeStartScene != null
                ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)
                : SceneManager.GetActiveScene().path;
            SessionState.SetBool(QuestPlayKey, startScene == QuestScene);
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
            StopSession();
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            StopSession();
            SessionState.SetBool(QuestPlayKey, false);
        }
        else if (state == PlayModeStateChange.EnteredPlayMode && _session != null)
        {
            if (_manager.activeLoader != null)
                Debug.Log("Quest Editor XR started: " + _manager.activeLoader.GetType().Name);
            else
                Debug.LogWarning("Quest Editor XR could not initialize an OpenXR runtime. Activate Meta XR Simulator or Quest Link before pressing Play.");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void PrepareQuestSession()
    {
        if (!SessionState.GetBool(QuestPlayKey, false)) return;
        _original = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (_original == null || _original.Manager == null || _original.InitManagerOnStart) return;
        _manager = UnityEngine.Object.Instantiate(_original.Manager);
        _manager.name = "Quest Editor XR | transient manager";
        _manager.hideFlags = HideFlags.HideAndDontSave;
        _manager.automaticLoading = false;
        _manager.automaticRunning = false;
        _session = UnityEngine.Object.Instantiate(_original);
        _session.name = "Quest Editor XR | transient settings";
        _session.hideFlags = HideFlags.HideAndDontSave;
        _session.Manager = _manager;
        _session.InitManagerOnStart = true;
        XRGeneralSettings.Instance = _session;
        // Unity XR Management initializes/starts this instance in its later
        // AfterAssembliesLoaded/BeforeSplashScreen callbacks, before OVRManager.
    }

    private static void StopSession()
    {
        if (_session != null)
        {
            if (_manager != null && _manager.isInitializationComplete)
            {
                _manager.StopSubsystems();
                _manager.DeinitializeLoader();
            }
            if (XRGeneralSettings.Instance == _session) XRGeneralSettings.Instance = _original;
            UnityEngine.Object.DestroyImmediate(_session);
            if (_manager != null) UnityEngine.Object.DestroyImmediate(_manager);
        }
        _session = null;
        _manager = null;
        _original = null;
    }
}
#endif

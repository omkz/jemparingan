using System.Collections;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
#endif

// Starts the XRI editor simulator in tracked-hand mode for this playtest scene.
public sealed class JemparinganXRSimulatorHandsDefault : MonoBehaviour
{
#if UNITY_EDITOR
    private IEnumerator Start()
    {
        yield return null;

        var lifecycle = GetComponent<SimulatedDeviceLifecycleManager>();
        if (lifecycle == null ||
            lifecycle.deviceMode == SimulatedDeviceLifecycleManager.DeviceMode.Hand)
            yield break;

        MethodInfo switchMode = typeof(SimulatedDeviceLifecycleManager).GetMethod(
            "SwitchDeviceMode", BindingFlags.Instance | BindingFlags.NonPublic);
        if (switchMode == null)
        {
            Debug.LogWarning("Jemparingan XR simulator could not switch to Hands mode.", this);
            yield break;
        }

        switchMode.Invoke(lifecycle, null);
    }
#endif
}

using UnityEngine;

// After installing Meta XR All-in-One SDK, add META_XR_SDK to
// Player > Other Settings > Scripting Define Symbols (both Android & Standalone).
// Otherwise this stays a harmless stub and the desktop prototype compiles.
public class MetaQuestBowInput : MonoBehaviour, IJemparinganInput
{
#if META_XR_SDK
    [Header("Assign OVRHand components from left/right hand prefabs")]
    public OVRHand leftHand;
    public OVRHand rightHand;

    public bool TryReadPose(out JemparinganHandPose pose)
    {
        pose = default;
        Camera cam = Camera.main;
        if (cam == null || leftHand == null || rightHand == null) return false;
        if (!leftHand.IsTracked || !rightHand.IsTracked) return false;
        if (leftHand.IsSystemGestureInProgress || rightHand.IsSystemGestureInProgress) return false;

        bool leftPinch = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool rightPinch = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        pose = new JemparinganHandPose
        {
            BowPinched = leftPinch,
            StringPinched = rightPinch,
            BowPosition = leftHand.transform.position,
            StringPosition = rightHand.transform.position,
            AimDirection = cam.transform.forward.normalized
        };
        return true;
    }
#else
    public bool TryReadPose(out JemparinganHandPose pose)
    {
        pose = default;
        return false;
    }
#endif
}

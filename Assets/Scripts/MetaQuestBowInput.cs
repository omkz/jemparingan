using UnityEngine;

// Compatibility stub for the archived Meta Quest scene.
// New XR scenes use XRHandsBowInput; no Meta XR SDK types are required.
public class MetaQuestBowInput : MonoBehaviour, IJemparinganInput
{
    public bool TryReadPose(out JemparinganHandPose pose)
    {
        pose = default;
        return false;
    }
}

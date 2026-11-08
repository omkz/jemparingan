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

    private readonly JemparinganQuestGestureGate _gestureGate = new JemparinganQuestGestureGate();
    private bool _focused = true;
    private bool _paused;

    public void CancelGesture()
    {
        _gestureGate.Interrupt();
        _hasHandAim = false;
    }

    private void OnEnable() { CancelGesture(); }
    private void OnDisable() { CancelGesture(); }
    private void OnApplicationFocus(bool focused)
    {
        _focused = focused;
        CancelGesture();
    }
    private void OnApplicationPause(bool paused)
    {
        _paused = paused;
        CancelGesture();
    }

    // Hand-to-hand direction is the approximate arrow line while drawing.
    // Validate palm/hand-anchor ergonomics and real tracking on Quest hardware.
    private Vector3 _lastValidHandAim;
    private bool _hasHandAim;

    public bool TryReadPose(out JemparinganHandPose pose)
    {
        pose = default;
        Camera cam = Camera.main;
        bool valid = isActiveAndEnabled && _focused && !_paused && cam != null &&
            leftHand != null && rightHand != null &&
            leftHand.isActiveAndEnabled && rightHand.isActiveAndEnabled &&
            leftHand.IsTracked && rightHand.IsTracked &&
            leftHand.IsDataValid && rightHand.IsDataValid &&
            leftHand.IsDataHighConfidence && rightHand.IsDataHighConfidence &&
            leftHand.GetFingerConfidence(OVRHand.HandFinger.Index) == OVRHand.TrackingConfidence.High &&
            rightHand.GetFingerConfidence(OVRHand.HandFinger.Index) == OVRHand.TrackingConfidence.High &&
            !leftHand.IsSystemGestureInProgress && !rightHand.IsSystemGestureInProgress;
        if (OVRManager.instance != null)
            valid &= OVRManager.hasInputFocus && OVRManager.hasVrFocus;
        if (!valid)
        {
            CancelGesture();
            return false;
        }

        bool leftPinch = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool rightPinch = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        if (!_gestureGate.Accept(true, leftPinch, rightPinch)) return false;

        Vector3 bowPosition = leftHand.transform.position;
        Vector3 stringPosition = rightHand.transform.position;
        Vector3 cameraAim = cam.transform.forward.normalized;
        Vector3 handAxis = bowPosition - stringPosition;
        Vector3 handAim = _hasHandAim ? _lastValidHandAim : cameraAim;
        // Head direction is used only while idle / when hand geometry is invalid.
        // With both pinch gestures held, the aim follows the bow-string axis,
        // rather than requiring the player to point their head at the target.
        if (leftPinch && rightPinch)
        {
            // Degenerate geometry cancels instead of firing along cached/head aim.
            // The axis is independent of head direction, allowing the user to look away.
            if (handAxis.sqrMagnitude < 0.0064f || handAxis.sqrMagnitude > 0.64f)
            {
                CancelGesture();
                return false;
            }
            handAim = handAxis.normalized;
            _lastValidHandAim = handAim;
            _hasHandAim = true;
        }
        else if (!leftPinch)
        {
            // Clear cached direction only when the bow hand is actually released.
            // Releasing the string must preserve the last hand-aim vector for the shot.
            handAim = cameraAim;
            _hasHandAim = false;
        }

        pose = new JemparinganHandPose
        {
            BowPinched = leftPinch,
            StringPinched = rightPinch,
            BowPosition = bowPosition,
            StringPosition = stringPosition,
            AimDirection = handAim
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

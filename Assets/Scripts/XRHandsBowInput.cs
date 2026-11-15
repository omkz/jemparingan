using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

// XR Hands adapter. Invalid tracking cancels a draw; a close hand position does not.
public sealed class XRHandsBowInput : MonoBehaviour, IJemparinganInput
{
    private const float MinHandDistance = 0.08f;
    private const float MaxHandDistance = 1.0f;

    [SerializeField] private Transform trackingOrigin;
    [SerializeField, Range(0.015f, 0.06f)] private float pinchEnter = 0.035f;
    [SerializeField, Range(0.03f, 0.08f)] private float pinchExit = 0.05f;

    private readonly List<XRHandSubsystem> _subsystems = new List<XRHandSubsystem>();
    private readonly JemparinganQuestGestureGate _gate = new JemparinganQuestGestureGate();
    private XRHandSubsystem _subsystem;
    private bool _leftPinched, _rightPinched, _hasAim, _focused = true, _paused;
    private Vector3 _lastAim;

    public string FailureHint { get; private set; } = "HAND TRACKING LOST";
    public bool HandsTooClose { get; private set; }

    public void SetTrackingOrigin(Transform origin) { trackingOrigin = origin; }

    private void OnEnable() { Cancel(); }
    private void OnDisable() { Cancel(); }
    private void OnApplicationFocus(bool focused)
    {
        _focused = focused;
        if (!focused) Cancel();
    }
    private void OnApplicationPause(bool paused)
    {
        _paused = paused;
        if (paused) Cancel();
    }

    private void Cancel()
    {
        _gate.Interrupt();
        _leftPinched = _rightPinched = _hasAim = HandsTooClose = false;
    }

    private bool Invalid(string reason)
    {
        FailureHint = reason;
        Cancel();
        return false;
    }

    public bool TryReadPose(out JemparinganHandPose pose)
    {
        pose = default;
        Camera camera = Camera.main;
        if (!isActiveAndEnabled || !_focused || _paused ||
            trackingOrigin == null || camera == null)
            return Invalid("XR INPUT NOT READY");

        if (_subsystem == null || !_subsystem.running)
        {
            SubsystemManager.GetSubsystems(_subsystems);
            _subsystem = null;
            foreach (var subsystem in _subsystems)
            {
                if (!subsystem.running) continue;
                _subsystem = subsystem;
                break;
            }
        }
        if (_subsystem == null) return Invalid("XR HANDS NOT RUNNING");

        XRHand left = _subsystem.leftHand;
        XRHand right = _subsystem.rightHand;
        if (!left.isTracked) return Invalid("LEFT HAND NOT TRACKED");
        if (!right.isTracked) return Invalid("RIGHT HAND NOT TRACKED");
        if (!ReadHand(left, _leftPinched, out Vector3 bow, out bool leftPinch))
            return Invalid("LEFT FINGERS NOT TRACKED");
        if (!ReadHand(right, _rightPinched, out Vector3 str, out bool rightPinch))
            return Invalid("RIGHT FINGERS NOT TRACKED");

        _leftPinched = leftPinch;
        _rightPinched = rightPinch;
        if (!_gate.Accept(true, leftPinch, rightPinch))
        {
            FailureHint = "RELEASE BOTH PINCHES";
            return false;
        }

        Vector3 axis = bow - str;
        Vector3 aim = _hasAim ? _lastAim : camera.transform.forward;
        HandsTooClose = false;
        if (leftPinch && rightPinch)
        {
            if (axis.sqrMagnitude > MaxHandDistance * MaxHandDistance)
                return Invalid("HANDS TOO FAR - RESET PINCHES");

            // Hands often begin almost touching. Allow the pinch to stay held
            // while the right hand moves back instead of cancelling the draw.
            HandsTooClose = axis.sqrMagnitude < MinHandDistance * MinHandDistance;
            if (!HandsTooClose)
            {
                aim = axis.normalized;
                _lastAim = aim;
                _hasAim = true;
            }
        }
        else if (!leftPinch)
        {
            _hasAim = false;
            aim = camera.transform.forward;
        }

        pose = new JemparinganHandPose
        {
            BowPinched = leftPinch,
            StringPinched = rightPinch,
            BowPosition = bow,
            StringPosition = str,
            AimDirection = aim
        };
        return true;
    }

    private bool ReadHand(XRHand hand, bool wasPinched, out Vector3 position, out bool pinched)
    {
        position = default;
        pinched = false;
        if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose index) ||
            !hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumb))
            return false;

        // Use the actual pinch contact instead of the palm: it is a better
        // bow/string anchor and needs fewer tracked joints.
        position = trackingOrigin.TransformPoint((index.position + thumb.position) * 0.5f);
        float distance = Vector3.Distance(index.position, thumb.position);
        pinched = distance < (wasPinched ? Mathf.Max(pinchExit, pinchEnter) : pinchEnter);
        return true;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

// Cross-platform hand input for the XRI Editor simulator and OpenXR providers.
// Quest's existing OVRHand adapter remains available in its original scene.
public sealed class XRHandsBowInput : MonoBehaviour, IJemparinganInput
{
    [SerializeField] private Transform trackingOrigin;
    [SerializeField, Range(0.015f, 0.06f)] private float pinchEnter = 0.035f;
    [SerializeField, Range(0.03f, 0.08f)] private float pinchExit = 0.05f;

    private readonly List<XRHandSubsystem> _subsystems = new List<XRHandSubsystem>();
    private readonly JemparinganQuestGestureGate _gate = new JemparinganQuestGestureGate();
    private XRHandSubsystem _subsystem;
    private bool _leftPinched, _rightPinched, _hasAim;
    private Vector3 _lastAim;

    public void SetTrackingOrigin(Transform origin) { trackingOrigin = origin; }

    private void OnEnable() { Cancel(); }
    private void OnDisable() { Cancel(); }
    private void OnApplicationFocus(bool focus) { if (!focus) Cancel(); }

    private void Cancel()
    {
        _gate.Interrupt();
        _leftPinched = _rightPinched = _hasAim = false;
    }

    public bool TryReadPose(out JemparinganHandPose pose)
    {
        pose = default;
        if (!isActiveAndEnabled || trackingOrigin == null || Camera.main == null)
            return Invalid();

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
        if (_subsystem == null) return Invalid();

        XRHand left = _subsystem.leftHand;
        XRHand right = _subsystem.rightHand;
        if (!left.isTracked || !right.isTracked ||
            !ReadHand(left, _leftPinched, out Vector3 bow, out bool leftPinch) ||
            !ReadHand(right, _rightPinched, out Vector3 str, out bool rightPinch))
            return Invalid();

        _leftPinched = leftPinch;
        _rightPinched = rightPinch;
        if (!_gate.Accept(true, leftPinch, rightPinch)) return false;

        Vector3 axis = bow - str;
        Vector3 aim = _hasAim ? _lastAim : Camera.main.transform.forward;
        if (leftPinch && rightPinch)
        {
            // Reject degenerate and impossible draws; tracking loss cannot fire.
            if (axis.sqrMagnitude < 0.0064f || axis.sqrMagnitude > 0.64f)
                return Invalid();
            aim = axis.normalized;
            _lastAim = aim;
            _hasAim = true;
        }
        else if (!leftPinch)
        {
            _hasAim = false;
            aim = Camera.main.transform.forward;
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

    private bool ReadHand(XRHand hand, bool wasPinched, out Vector3 palm, out bool pinched)
    {
        palm = default;
        pinched = false;
        if (!hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palmPose) ||
            !hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose) ||
            !hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbPose))
            return false;

        // Joint positions are in tracking space, not Unity world space.
        palm = trackingOrigin.TransformPoint(palmPose.position);
        float separation = Vector3.Distance(indexPose.position, thumbPose.position);
        pinched = separation < (wasPinched ? pinchExit : pinchEnter);
        return true;
    }

    private bool Invalid()
    {
        Cancel();
        return false;
    }
}

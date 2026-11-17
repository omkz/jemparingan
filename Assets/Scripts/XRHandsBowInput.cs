using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

// XR Hands adapter. Invalid tracking cancels a draw; a close hand position does not.
public sealed class XRHandsBowInput : MonoBehaviour, IJemparinganInput
{
    private const float MinHandDistance = 0.08f;
    // A short draw magnifies angular error from tiny pinch movements.
    private const float MinAimDistance = 0.14f;
    private const float MaxHandDistance = 1.0f;
    private const float PositionSnapDistance = 0.30f;

    [SerializeField] private Transform trackingOrigin;
    [SerializeField, Range(0.015f, 0.06f)] private float pinchEnter = 0.035f;
    [SerializeField, Range(0.03f, 0.08f)] private float pinchExit = 0.05f;
    [Header("XR tracking stabilization")]
    [SerializeField, Range(0f, 0.10f)] private float gripSmoothingSeconds = 0.025f;
    [SerializeField, Range(0.01f, 0.20f)] private float steadyAimSeconds = 0.085f;
    [SerializeField, Range(0.01f, 0.15f)] private float movingAimSeconds = 0.035f;
    [SerializeField, Range(2f, 20f)] private float movingAimThresholdDegrees = 6f;

    private readonly List<XRHandSubsystem> _subsystems = new List<XRHandSubsystem>();
    private readonly JemparinganQuestGestureGate _gate = new JemparinganQuestGestureGate();
    private XRHandSubsystem _subsystem;
    private bool _leftPinched, _rightPinched, _hasAim, _focused = true, _paused;
    private bool _hasFilteredBow, _hasFilteredString;
    private Vector3 _lastAim, _filteredBow, _filteredString;

#if UNITY_EDITOR
    private string _lastDebugState;

    private void ReportState(string state)
    {
        if (_lastDebugState == state) return;
        _lastDebugState = state;
        Debug.Log("Jemparingan XR Hands: " + state, this);
    }
#endif

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
        _hasFilteredBow = _hasFilteredString = false;
    }

    private bool Invalid(string reason)
    {
        FailureHint = reason;
#if UNITY_EDITOR
        ReportState(reason);
#endif
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

        bool newBowGrip = leftPinch && !_leftPinched;
        bool newStringGrip = rightPinch && !_rightPinched;
        _leftPinched = leftPinch;
        _rightPinched = rightPinch;
        if (!_gate.Accept(true, leftPinch, rightPinch))
        {
            FailureHint = "RELEASE BOTH PINCHES";
#if UNITY_EDITOR
            ReportState(FailureHint);
#endif
            return false;
        }

        // Validate the real tracked separation, before position filtering.
        Vector3 rawAxis = bow - str;
        if (leftPinch && rightPinch &&
            rawAxis.sqrMagnitude > MaxHandDistance * MaxHandDistance)
            return Invalid("HANDS TOO FAR - RESET PINCHES");

        // A new grip must never interpolate from an old shot or a stale hand.
        if (newBowGrip)
        {
            _hasFilteredBow = false;
            _hasAim = false;
        }
        if (newStringGrip)
        {
            _hasFilteredString = false;
            _hasAim = false;
        }
        if (leftPinch)
            bow = StabilizePosition(bow, ref _filteredBow, ref _hasFilteredBow);
        else
            _hasFilteredBow = _hasFilteredString = _hasAim = false;

        if (rightPinch)
            str = StabilizePosition(str, ref _filteredString, ref _hasFilteredString);
        else
            _hasFilteredString = false;

        Vector3 aim = _hasAim ? _lastAim : camera.transform.forward;
        // Hands may start almost touching: keep the gesture active while
        // the string hand moves back, rather than cancelling the draw.
        HandsTooClose = leftPinch && rightPinch &&
            rawAxis.sqrMagnitude < MinHandDistance * MinHandDistance;
        if (leftPinch && rightPinch && !HandsTooClose)
        {
            Vector3 axis = bow - str;
            if (axis.sqrMagnitude >= MinAimDistance * MinAimDistance)
            {
                Vector3 desired = axis.normalized;
                if (!_hasAim)
                    aim = desired;
                else
                {
                    // Low-pass tracking noise near a settled sight line.
                    // Larger intentional re-aiming gets a faster response.
                    float angle = Vector3.Angle(_lastAim, desired);
                    float response = Mathf.InverseLerp(
                        1f, movingAimThresholdDegrees, angle);
                    float seconds = Mathf.Lerp(steadyAimSeconds,
                        movingAimSeconds, response);
                    float weight = 1f - Mathf.Exp(
                        -Time.deltaTime / Mathf.Max(0.001f, seconds));
                    aim = Vector3.Slerp(_lastAim, desired, weight).normalized;
                }
                _lastAim = aim;
                _hasAim = true;
            }
        }
        else if (!leftPinch)
        {
            aim = camera.transform.forward;
        }
        // Releasing the right pinch keeps the previous frame's aim. The
        // gameplay controller then fires along the sight line just held.

#if UNITY_EDITOR
        ReportState("TRACKED | left pinch=" + leftPinch + " | right pinch=" +
            rightPinch + (HandsTooClose ? " | move right hand back" : ""));
#endif
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

    private Vector3 StabilizePosition(Vector3 sample,
        ref Vector3 filtered, ref bool hasSample)
    {
        // Filtering is active only while gripping. A tracker jump/recenter
        // must not interpolate the bow or string across the room.
        if (!hasSample || gripSmoothingSeconds <= 0f ||
            (sample - filtered).sqrMagnitude >
                PositionSnapDistance * PositionSnapDistance)
        {
            filtered = sample;
            hasSample = true;
            return filtered;
        }

        float weight = 1f - Mathf.Exp(-Time.deltaTime /
            Mathf.Max(0.001f, gripSmoothingSeconds));
        filtered = Vector3.Lerp(filtered, sample, weight);
        return filtered;
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

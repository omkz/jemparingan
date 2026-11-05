using UnityEngine;

// Optional, desktop-only cinematic camera. Follows the real pooled projectile
// without moving the player's main camera or changing trajectory/collision.
public sealed class JemparinganArrowFollowCamera : MonoBehaviour
{
    private const float FollowBehind = 0.85f;
    private const float FollowAbove = 0.18f;
    private const float FollowRight = 0.12f;
    private const float ImpactHoldSeconds = 0.28f;
    private const float SafetyTimeoutSeconds = 4.5f;

    private Camera _playerCamera;
    private Camera _followCamera;
    private DesktopBowInput _desktopInput;
    private JemparinganArrow _projectile;
    private float _startedAt;
    private float _impactHoldUntil = -1f;

    public bool IsActive { get; private set; }

    public bool Begin(JemparinganArrow projectile, Camera playerCamera,
        DesktopBowInput desktopInput)
    {
        if (IsActive || projectile == null || !projectile.IsFlying ||
            playerCamera == null || desktopInput == null)
            return false;

        if (_followCamera == null)
        {
            var cameraObject = new GameObject("Challenge Arrow Cam (desktop only)");
            cameraObject.transform.SetParent(transform, false);
            _followCamera = cameraObject.AddComponent<Camera>();
            _followCamera.enabled = false;
            cameraObject.tag = "Untagged"; // Camera.main must remain the seated player camera.
        }

        _playerCamera = playerCamera;
        _desktopInput = desktopInput;
        _projectile = projectile;
        _startedAt = Time.unscaledTime;
        _impactHoldUntil = -1f;

        // Built-in render pipeline: render a full-screen follow view above the
        // unchanged player camera. There is deliberately no second AudioListener.
        _followCamera.CopyFrom(playerCamera);
        _followCamera.depth = playerCamera.depth + 1f;
        _followCamera.clearFlags = playerCamera.clearFlags == CameraClearFlags.Skybox
            ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        _followCamera.targetTexture = null;
        _followCamera.stereoTargetEye = StereoTargetEyeMask.None;
        PlaceBehindArrow();
        _desktopInput.CameraLookSuppressed = true;
        IsActive = true;
        _followCamera.enabled = true;
        return true;
    }

    private void LateUpdate()
    {
        if (!IsActive) return;

        if (_playerCamera == null || _projectile == null ||
            _desktopInput == null || Time.unscaledTime - _startedAt > SafetyTimeoutSeconds)
        {
            Stop();
            return;
        }

        if (_projectile.IsFlying)
        {
            PlaceBehindArrow();
            return;
        }

        // A hit keeps the actual embedded arrow visible for a brief beat.
        // A miss deactivates its pooled object, so restore immediately.
        if (!_projectile.gameObject.activeInHierarchy)
        {
            Stop();
            return;
        }
        if (_impactHoldUntil < 0f)
        {
            PlaceBehindArrow();
            _impactHoldUntil = Time.unscaledTime + ImpactHoldSeconds;
        }
        if (Time.unscaledTime >= _impactHoldUntil) Stop();
    }

    private void PlaceBehindArrow()
    {
        Vector3 forward = _projectile.transform.forward;
        if (forward.sqrMagnitude < 0.01f)
            forward = _playerCamera.transform.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        if (right.sqrMagnitude < 0.01f)
            right = _playerCamera.transform.right;

        Vector3 position = _projectile.transform.position - forward * FollowBehind
            + Vector3.up * FollowAbove + right * FollowRight;
        Vector3 lookAt = _projectile.transform.position + forward * 4f;
        _followCamera.transform.SetPositionAndRotation(position,
            Quaternion.LookRotation(lookAt - position, Vector3.up));
    }

    public void Stop()
    {
        if (_followCamera != null) _followCamera.enabled = false;
        if (_desktopInput != null) _desktopInput.CameraLookSuppressed = false;
        IsActive = false;
        _projectile = null;
        _playerCamera = null;
        _desktopInput = null;
        _impactHoldUntil = -1f;
    }

    private void OnDisable() { Stop(); }
    private void OnDestroy() { Stop(); }
}

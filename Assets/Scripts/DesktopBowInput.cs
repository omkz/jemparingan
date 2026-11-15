using UnityEngine;
using Input = JemparinganDesktopControls;

// Mouse input for the desktop version.
public class DesktopBowInput : MonoBehaviour, IJemparinganInput
{
    [SerializeField] private float simulatedDrawSeconds = 1.0f;
    // The shared outdoor bow draws quickly in Traditional and Modern modes.
    [SerializeField, Range(0.10f, 0.70f)] private float modernFullDrawSeconds = .28f;
    [SerializeField] private float lookSensitivity = 2.5f;
    // Preserve the legacy close-up pose unless the outdoor view is enabled.
    [SerializeField] private bool outdoorBowPresentation = false;
    private float _drawStart;

    // Shared outdoor modes aim with the mouse while holding LMB.
    public bool ModernSingleButtonAim { get; set; }

    // Arrow Cam temporarily suppresses camera look.
    public bool CameraLookSuppressed { get; set; }

    private void Update()
    {
        if (CameraLookSuppressed) return;
        if (Input.GetMouseButtonDown(0)) _drawStart = Time.time;

        // Shared outdoor modes aim during LMB draw; legacy mode uses RMB.
        bool aimHeld = ModernSingleButtonAim
            ? Input.GetMouseButton(0)
            : Input.GetMouseButton(1);
        if (aimHeld && Camera.main != null)
        {
            // Reduce mouse sensitivity in outdoor mode.
            float multiplier = (outdoorBowPresentation || ModernSingleButtonAim)
                ? (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                    ? 0.12f : 0.32f)
                : 1f;
            var cam = Camera.main.transform;
            cam.Rotate(Vector3.up, Input.GetAxis("Mouse X") *
                lookSensitivity * multiplier, Space.World);
            cam.Rotate(Vector3.right, -Input.GetAxis("Mouse Y") *
                lookSensitivity * multiplier, Space.Self);
            Vector3 angles = cam.eulerAngles;
            if (Mathf.Abs(angles.z) > 0.001f)
                cam.eulerAngles = new Vector3(angles.x, angles.y, 0f);
        }
    }

    public bool TryReadPose(out JemparinganHandPose pose)
    {
        pose = default;
        if (Camera.main == null) return false;
        Transform cam = Camera.main.transform;
        bool drawing = Input.GetMouseButton(0);
        float drawDuration = ModernSingleButtonAim
            ? Mathf.Max(.10f, modernFullDrawSeconds)
            : Mathf.Max(.10f, simulatedDrawSeconds);
        float progress = drawing
            ? Mathf.Clamp01((Time.time - _drawStart) / drawDuration)
            : 0f;
        Vector3 forward = cam.forward.normalized;
        Vector3 bow = outdoorBowPresentation
            ? cam.position + forward * 0.92f - cam.right * 0.33f - cam.up * 0.18f
            : cam.position + forward * 0.55f - cam.right * 0.07f - cam.up * 0.12f;
        pose = new JemparinganHandPose
        {
            BowPinched = true,
            StringPinched = drawing,
            BowPosition = bow,
            StringPosition = bow - forward * (0.04f + progress * 0.38f),
            AimDirection = forward
        };
        return true;
    }
}

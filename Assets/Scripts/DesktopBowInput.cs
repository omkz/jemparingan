using UnityEngine;
using Input = JemparinganDesktopControls;

// Editor keyboard/mouse prototype only. Not the competition's Quest input.
public class DesktopBowInput : MonoBehaviour, IJemparinganInput
{
    [SerializeField] private float simulatedDrawSeconds = 1.0f;
    // Modern-only: a short visible pull, then a held full draw for aiming.
    // Classic/Traditional/Challenge keep their original one-second draw.
    [SerializeField, Range(0.10f, 0.70f)] private float modernFullDrawSeconds = .28f;
    [SerializeField] private float lookSensitivity = 2.5f;
    // Opt-in for a readable first-person bow in the outdoor Editor preview.
    // Legacy scenes keep the v0.9 close-up pose.
    [SerializeField] private bool outdoorBowPresentation = false;
    private float _drawStart;

    // Set by JemparinganGame only while desktop Modern Archery is active.
    // Holding LMB both draws and allows mouse movement to aim; no RMB.
    public bool ModernSingleButtonAim { get; set; }

    // The optional Challenge Arrow Cam pauses desktop camera input briefly.
    // Input pose and the seated camera remain otherwise unchanged.
    public bool CameraLookSuppressed { get; set; }

    private void Update()
    {
        if (CameraLookSuppressed) return;
        if (Input.GetMouseButtonDown(0)) _drawStart = Time.time;

        // Modern follows Archery Master-style controls: LMB hold to draw
        // AND move the camera for aiming, then release LMB to shoot.
        // Every other mode retains RMB camera look.
        bool aimHeld = ModernSingleButtonAim
            ? Input.GetMouseButton(0)
            : Input.GetMouseButton(1);
        if (aimHeld && Camera.main != null)
        {
            // Apply outdoor fine-aim sensitivity in Modern regardless of
            // the older outdoorBowPresentation scene Inspector flag.
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

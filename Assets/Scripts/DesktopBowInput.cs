using UnityEngine;

// Editor keyboard/mouse prototype only. Not the competition's Quest input.
public class DesktopBowInput : MonoBehaviour, IJemparinganInput
{
    [SerializeField] private float simulatedDrawSeconds = 1.0f;
    [SerializeField] private float lookSensitivity = 2.5f;
    // Opt-in for a readable first-person bow in the outdoor Editor preview.
    // Legacy scenes keep the v0.9 close-up pose.
    [SerializeField] private bool outdoorBowPresentation = false;
    private float _drawStart;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) _drawStart = Time.time;

        // Hold RMB and move mouse to aim on desktop.
        if (Input.GetMouseButton(1) && Camera.main != null)
        {
            // The old 2.5x sensitivity is usable at 2.65m but overshoots a
            // 5cm-wide bandulan at 10–30m. Only the outdoor desktop preview
            // uses a calmer base speed and a finer Shift-aim adjustment.
            float multiplier = outdoorBowPresentation
                ? (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                    ? 0.12f : 0.32f)
                : 1f;
            var cam = Camera.main.transform;
            cam.Rotate(Vector3.up, Input.GetAxis("Mouse X") *
                lookSensitivity * multiplier, Space.World);
            cam.Rotate(Vector3.right, -Input.GetAxis("Mouse Y") *
                lookSensitivity * multiplier, Space.Self);
        }
    }

    public bool TryReadPose(out JemparinganHandPose pose)
    {
        pose = default;
        if (Camera.main == null) return false;
        Transform cam = Camera.main.transform;
        bool drawing = Input.GetMouseButton(0);
        float progress = drawing ? Mathf.Clamp01((Time.time - _drawStart) / simulatedDrawSeconds) : 0f;
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

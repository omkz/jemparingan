#if UNITY_EDITOR
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Editor-only shortcut for testing actual arrow physics without two simulated hands.
public sealed class EditorSpaceBowInput : MonoBehaviour, IJemparinganInput
{
    [SerializeField, Range(0.2f, 1.5f)] private float fullDrawSeconds = 0.6f;
    private float _drawStartedAt;
    private bool _armed, _drawing, _focused = true, _cancelPending;

    private void OnEnable() { _armed = _drawing = false; }
    private void OnDisable() { _armed = _drawing = false; }
    private void OnApplicationFocus(bool focused)
    {
        _focused = focused;
        _armed = _drawing = false;
        _cancelPending = true;
    }

    public bool TryReadPose(out JemparinganHandPose pose)
    {
        pose = default;
        var cam = Camera.main;
        if (!isActiveAndEnabled || !_focused || cam == null) return false;
        // Force gameplay to cancel a held draw after focus is restored.
        if (_cancelPending) { _cancelPending = false; return false; }

#if ENABLE_INPUT_SYSTEM
        bool pressed = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#else
        bool pressed = Input.GetKey(KeyCode.Space);
#endif
        // Require an unpressed key after enabling or losing focus.
        if (!pressed) _armed = true;
        bool drawing = _armed && pressed;
        if (drawing && !_drawing) _drawStartedAt = Time.time;
        _drawing = drawing;

        Transform eye = cam.transform;
        Vector3 aim = eye.forward.normalized;
        Vector3 bow = eye.position + aim * 0.7f - eye.right * 0.22f - eye.up * 0.14f;
        float pull = drawing
            ? Mathf.Lerp(0.04f, 0.4f,
                Mathf.Clamp01((Time.time - _drawStartedAt) / fullDrawSeconds))
            : 0.04f;

        pose = new JemparinganHandPose
        {
            BowPinched = true,
            StringPinched = drawing,
            BowPosition = bow,
            StringPosition = bow - aim * pull,
            AimDirection = aim
        };
        return true;
    }
}
#endif

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

// Desktop controls remain available when Android/OpenXR requires a single
// Input System backend. Quest gameplay never reads this mouse/keyboard route.
public static class JemparinganDesktopControls
{
    public static bool GetMouseButton(int button)
    {
#if ENABLE_INPUT_SYSTEM
        return MouseButton(button)?.isPressed ?? false;
#else
        return UnityEngine.Input.GetMouseButton(button);
#endif
    }

    public static bool GetMouseButtonDown(int button)
    {
#if ENABLE_INPUT_SYSTEM
        return MouseButton(button)?.wasPressedThisFrame ?? false;
#else
        return UnityEngine.Input.GetMouseButtonDown(button);
#endif
    }

    public static bool GetKey(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        return KeyboardKey(key)?.isPressed ?? false;
#else
        return UnityEngine.Input.GetKey(key);
#endif
    }

    public static bool GetKeyDown(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        return KeyboardKey(key)?.wasPressedThisFrame ?? false;
#else
        return UnityEngine.Input.GetKeyDown(key);
#endif
    }

    public static float GetAxis(string axis)
    {
#if ENABLE_INPUT_SYSTEM
        var delta = Mouse.current?.delta.ReadValue() ?? Vector2.zero;
        // Matches Mouse X/Y sensitivity 0.1 in ProjectSettings/InputManager.asset.
        return axis == "Mouse X" ? delta.x * .1f : axis == "Mouse Y" ? delta.y * .1f : 0f;
#else
        return UnityEngine.Input.GetAxis(axis);
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static ButtonControl MouseButton(int button)
    {
        var mouse = Mouse.current;
        return mouse == null ? null : button == 0 ? mouse.leftButton : button == 1 ? mouse.rightButton : button == 2 ? mouse.middleButton : null;
    }

    private static KeyControl KeyboardKey(KeyCode code)
    {
        Key key;
        switch (code)
        {
            case KeyCode.Alpha1: key = Key.Digit1; break;
            case KeyCode.Alpha2: key = Key.Digit2; break;
            case KeyCode.M: key = Key.M; break;
            case KeyCode.G: key = Key.G; break;
            case KeyCode.V: key = Key.V; break;
            case KeyCode.R: key = Key.R; break;
            case KeyCode.T: key = Key.T; break;
            case KeyCode.Escape: key = Key.Escape; break;
            case KeyCode.LeftShift: key = Key.LeftShift; break;
            case KeyCode.RightShift: key = Key.RightShift; break;
            default: return null;
        }
        return Keyboard.current?[key];
    }
#endif
}

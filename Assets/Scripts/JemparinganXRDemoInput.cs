using UnityEngine;

/// <summary>
/// Editor-only one-shot demonstration for the XRI simulator playtest.
/// It supplies the same hand-pose interface as real tracking so gameplay,
/// projectile physics, scoring, and the normal controls remain unchanged.
/// </summary>
public sealed class JemparinganXRDemoInput : MonoBehaviour, IJemparinganInput
{
    private const float HoldDuration = 0.35f;
    private const float DrawDuration = 1.25f;
    private const float ReleaseDuration = 0.30f;
    private const float FullDraw = 0.40f;
    private const float FallbackArrowSpeed = 11f;
    private const float FallbackArrowGravity = 2.25f;

    private JemparinganGame _game;
    private JemparinganTarget _target;
    private float _startedAt;

    public bool IsPlaying { get; private set; }

    public void PlayDemo()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying || IsPlaying)
            return;

        GameObject targetObject = GameObject.Find("Bandulan | Target");
        _game = GetComponent<JemparinganGame>();
        _target = targetObject != null
            ? targetObject.GetComponent<JemparinganTarget>() : null;
        _startedAt = Time.time;
        IsPlaying = true;
        Debug.Log("Jemparingan XR demo: started", this);
#endif
    }

    public bool TryReadPose(out JemparinganHandPose pose)
    {
        pose = default;
#if UNITY_EDITOR
        if (!IsPlaying)
            return false;

        Camera eye = Camera.main;
        if (eye == null)
        {
            FinishDemo();
            return false;
        }

        float elapsed = Time.time - _startedAt;
        float releaseAt = HoldDuration + DrawDuration;
        if (elapsed >= releaseAt + ReleaseDuration)
        {
            FinishDemo();
            return false;
        }

        Vector3 bowPosition = eye.transform.position + eye.transform.forward * 0.55f
            - eye.transform.right * 0.22f - eye.transform.up * 0.08f;
        float arrowSpeed = _game != null
            ? _game.DemoMaximumArrowSpeed : FallbackArrowSpeed;
        float arrowGravity = _game != null
            ? _game.DemoArrowGravity : FallbackArrowGravity;
        float estimatedFlightTime = _target != null
            ? Vector3.Distance(bowPosition, _target.transform.position) /
                arrowSpeed
            : 0f;
        Vector3 targetPosition = _target != null
            ? _target.PredictCenter(estimatedFlightTime)
            : eye.transform.position + eye.transform.forward * 10f;
        Vector3 aim = BallisticDirection(
            bowPosition + (targetPosition - bowPosition).normalized * 0.25f,
            targetPosition, arrowSpeed, arrowGravity);

        float draw = 0f;
        bool stringPinched = false;
        if (elapsed >= HoldDuration && elapsed < releaseAt)
        {
            float progress = Mathf.Clamp01((elapsed - HoldDuration) / DrawDuration);
            draw = Mathf.Lerp(0.04f, FullDraw, Mathf.SmoothStep(0f, 1f, progress));
            stringPinched = true;
        }
        else if (elapsed >= releaseAt)
        {
            // Preserve the final pose for the release frame. Only the right
            // pinch changes, exactly like releasing a real drawn string.
            draw = FullDraw;
        }

        pose = new JemparinganHandPose
        {
            BowPinched = true,
            StringPinched = stringPinched,
            BowPosition = bowPosition,
            StringPosition = bowPosition - aim * draw,
            AimDirection = aim,
        };
        return true;
#else
        return false;
#endif
    }

    private void OnGUI()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;

        Rect buttonRect = new Rect(18f, Screen.height - 64f, 176f, 46f);
        bool wasEnabled = GUI.enabled;
        GUI.enabled = wasEnabled && !IsPlaying;
        if (GUI.Button(buttonRect, IsPlaying ? "DEMO BERJALAN..." : "PLAY DEMO"))
            PlayDemo();
        GUI.enabled = wasEnabled;
#endif
    }

    private void OnDisable()
    {
        IsPlaying = false;
    }

    private void FinishDemo()
    {
        if (!IsPlaying)
            return;
        IsPlaying = false;
        Debug.Log("Jemparingan XR demo: completed", this);
    }

    private static Vector3 BallisticDirection(
        Vector3 origin, Vector3 target, float speed, float gravity)
    {
        Vector3 delta = target - origin;
        Vector3 horizontal = Vector3.ProjectOnPlane(delta, Vector3.up);
        float distance = horizontal.magnitude;
        if (distance < 0.001f || gravity <= 0f)
            return delta.normalized;

        float speedSquared = speed * speed;
        float discriminant = speedSquared * speedSquared - gravity *
            (gravity * distance * distance + 2f * delta.y * speedSquared);
        if (discriminant < 0f)
            return delta.normalized;

        float tangent = (speedSquared - Mathf.Sqrt(discriminant)) /
            (gravity * distance);
        float cosine = 1f / Mathf.Sqrt(1f + tangent * tangent);
        float sine = tangent * cosine;
        return horizontal.normalized * cosine + Vector3.up * sine;
    }
}

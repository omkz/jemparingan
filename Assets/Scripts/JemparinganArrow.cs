using System;
using UnityEngine;

// Pooled projectile with swept-tip collision against a moving bandulan.
// No Rigidbody or allocation per release, no tunneling at high speed.
public class JemparinganArrow : MonoBehaviour
{
    private const float TipOffset = 0.36f;
    private const float MaxLife = 1.6f;
    private Vector3 _velocity;
    private JemparinganTarget _target;
    private Action<JemparinganArrow, bool> _onImpact;
    private Action<JemparinganArrow> _onMiss;
    public bool StableRelease { get; set; }
    private float _lifetime;
    private float _gravity;
    private bool _flying;

    public bool IsFlying => _flying;

    public void Launch(Vector3 origin, Vector3 velocity, float gravity,
        JemparinganTarget target, Action<JemparinganArrow, bool> onImpact,
        Action<JemparinganArrow> onMiss)
    {
        transform.SetPositionAndRotation(origin, Quaternion.LookRotation(velocity.normalized));
        _velocity = velocity;
        _target = target;
        _onImpact = onImpact;
        _onMiss = onMiss;
        _gravity = gravity;
        _lifetime = 0f;
        _flying = true;
        gameObject.SetActive(true);
    }

    public void Cancel() { _flying = false; gameObject.SetActive(false); }

    private void Update()
    {
        if (!_flying) return;
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        Vector3 previousTip = transform.position + transform.forward * TipOffset;
        _velocity += Vector3.down * (_gravity * dt);
        Vector3 nextPos = transform.position + _velocity * dt;
        Quaternion nextRot = Quaternion.LookRotation(_velocity.normalized);
        Vector3 nextTip = nextPos + (nextRot * Vector3.forward) * TipOffset;
        transform.SetPositionAndRotation(nextPos, nextRot);
        _lifetime += dt;

        if (_target != null && _target.TestShot(previousTip, nextTip, out bool bullseye))
        {
            _flying = false;
            _target.Flash();
            _onImpact?.Invoke(this, bullseye);
            gameObject.SetActive(false);
        }
        else if (_lifetime > MaxLife)
        {
            _flying = false;
            gameObject.SetActive(false);
            _onMiss?.Invoke(this);
        }
    }
}

using System;
using UnityEngine;

// Pooled projectile with swept-tip collision and a short-lived stuck-arrow impact.
public class JemparinganArrow : MonoBehaviour
{
    private const float TipOffset = 0.36f;
    private const float MaxLife = 1.6f;
    private const float EmbedSeconds = 1.15f;
    private Vector3 _velocity;
    private JemparinganTarget _target;
    private Action<JemparinganArrow, bool> _onImpact;
    private Action<JemparinganArrow> _onMiss;
    private Transform _poolParent;
    public bool StableRelease { get; set; }
    private float _lifetime;
    private float _gravity;
    private bool _flying;
    private bool _embedded;
    private float _embeddedUntil;

    public bool IsFlying => _flying;

    private void Awake() { _poolParent = transform.parent; }

    public void Launch(Vector3 origin, Vector3 velocity, float gravity,
        JemparinganTarget target, Action<JemparinganArrow, bool> onImpact,
        Action<JemparinganArrow> onMiss)
    {
        if (_poolParent == null) _poolParent = transform.parent;
        if (transform.parent != _poolParent) transform.SetParent(_poolParent, true);
        transform.SetPositionAndRotation(origin, Quaternion.LookRotation(velocity.normalized));
        _velocity = velocity;
        _target = target;
        _onImpact = onImpact;
        _onMiss = onMiss;
        _gravity = gravity;
        _lifetime = 0f;
        _flying = true;
        _embedded = false;
        gameObject.SetActive(true);
    }

    public void Cancel()
    {
        _flying = false;
        _embedded = false;
        if (_poolParent != null && transform.parent != _poolParent)
            transform.SetParent(_poolParent, true);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_embedded)
        {
            if (Time.time >= _embeddedUntil) Cancel();
            return;
        }
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

            // Seat the arrow tip on the target axis so it looks embedded rather than floating.
            Vector3 localTip = _target.transform.InverseTransformPoint(nextTip);
            localTip.x = 0f;
            localTip.z = 0f;
            localTip.y = Mathf.Clamp(localTip.y, -0.21f, 0.21f);
            transform.position += _target.transform.TransformPoint(localTip) - nextTip;
            transform.SetParent(_target.transform, true);
            _embedded = true;
            _embeddedUntil = Time.time + EmbedSeconds;

            _target.Flash(bullseye);
            _onImpact?.Invoke(this, bullseye);
        }
        else if (_lifetime > MaxLife)
        {
            _flying = false;
            gameObject.SetActive(false);
            _onMiss?.Invoke(this);
        }
    }
}

using System;
using UnityEngine;

// Pooled projectile with swept-tip collision and a short-lived stuck-arrow impact.
public class JemparinganArrow : MonoBehaviour
{
    private const float TipOffset = 0.36f;
    private const float DefaultMaxLife = 1.6f;
    private float _maxLife = DefaultMaxLife;
    private const float EmbedSeconds = 1.15f;
    private Vector3 _velocity;
    private JemparinganTarget _target;
    private JemparinganModernTarget[] _modernTargets;
    private Action<JemparinganArrow, JemparinganModernTarget, int> _onModernImpact;
    private Action<JemparinganArrow, bool> _onImpact;
    private Action<JemparinganArrow> _onMiss;
    private Transform _poolParent;
    private TrailRenderer _trail;
    public bool StableRelease { get; set; }
    private float _lifetime;
    private float _gravity;
    private bool _flying;
    private bool _embedded;
    private float _embeddedUntil;

    public bool IsFlying => _flying;

    private void Awake()
    {
        _poolParent = transform.parent;
        _trail = GetComponent<TrailRenderer>();
    }

    public void Launch(Vector3 origin, Vector3 velocity, float gravity,
        JemparinganTarget target, Action<JemparinganArrow, bool> onImpact,
        Action<JemparinganArrow> onMiss, float maxLifeSeconds = DefaultMaxLife,
        JemparinganModernTarget[] modernTargets = null,
        Action<JemparinganArrow, JemparinganModernTarget, int> onModernImpact = null)
    {
        if (_poolParent == null) _poolParent = transform.parent;
        if (transform.parent != _poolParent) transform.SetParent(_poolParent, true);
        transform.SetPositionAndRotation(origin, Quaternion.LookRotation(velocity.normalized));
        _velocity = velocity;
        _target = target;
        _modernTargets = modernTargets;
        _onModernImpact = onModernImpact;
        _onImpact = onImpact;
        _onMiss = onMiss;
        _gravity = gravity;
        _maxLife = Mathf.Max(0.2f, maxLifeSeconds);
        _lifetime = 0f;
        _flying = true;
        _embedded = false;
        gameObject.SetActive(true);
        if (_trail != null)
        {
            _trail.Clear(); // Reused pooled arrows never inherit old shot trails.
            _trail.emitting = true;
        }
    }

    public void Cancel()
    {
        _flying = false;
        _embedded = false;
        _modernTargets = null;
        _onModernImpact = null;
        if (_trail != null)
        {
            _trail.emitting = false;
            _trail.Clear();
        }
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
        // Integrate position before velocity with the same exact gravity curve as
        // the preview dots; avoids frame-rate-dependent early dropping.
        Vector3 nextPos = JemparinganTrajectory.Position(transform.position, _velocity, _gravity, dt);
        _velocity += Vector3.down * (_gravity * dt);
        Quaternion nextRot = Quaternion.LookRotation(_velocity.normalized);
        Vector3 nextTip = nextPos + (nextRot * Vector3.forward) * TipOffset;
        transform.SetPositionAndRotation(nextPos, nextRot);
        _lifetime += dt;

        // Modern Range: resolve the same swept arrow tip against three
        // circular targets. Original bandulan collision remains untouched.
        if (_modernTargets != null)
        {
            for (int i = 0; i < _modernTargets.Length; i++)
            {
                JemparinganModernTarget modern = _modernTargets[i];
                if (modern == null || !modern.gameObject.activeInHierarchy ||
                    !modern.TestShot(previousTip, nextTip, out int ringPoints,
                        out Vector3 impactPoint))
                    continue;

                _flying = false;
                if (_trail != null) _trail.emitting = false;
                transform.position += impactPoint - nextTip;
                transform.SetParent(modern.transform, true);
                _embedded = true;
                _embeddedUntil = Time.time + EmbedSeconds;
                modern.ShowImpact(impactPoint);
                _onModernImpact?.Invoke(this, modern, ringPoints);
                return;
            }
        }

        if (_target != null && _target.TestShot(previousTip, nextTip, out bool bullseye))
        {
            _flying = false;
            if (_trail != null) _trail.emitting = false;

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
        else if (_lifetime > _maxLife)
        {
            _flying = false;
            if (_trail != null) _trail.emitting = false;
            gameObject.SetActive(false);
            _onMiss?.Invoke(this);
        }
    }
}

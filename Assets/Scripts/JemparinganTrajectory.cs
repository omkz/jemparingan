using UnityEngine;

// Shared ballistic equation for visual preview and shot travel.
public static class JemparinganTrajectory
{
    public static Vector3 Position(Vector3 origin, Vector3 initialVelocity,
        float gravity, float time)
    {
        float t = Mathf.Max(0f, time);
        return origin + initialVelocity * t + Vector3.down * (.5f * gravity * t * t);
    }
}

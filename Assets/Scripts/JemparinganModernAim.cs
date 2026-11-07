using UnityEngine;

// Modern Range only: zero the camera-axis reticle to the actual parabolic
// arrow-tip trajectory on the visible target plane. No target-center snapping,
// homing, magnetism, changes to gravity, or changes to scoring/collisions.
public static class JemparinganModernAim
{
    // The arrow collision code tests the swept TIP, not the arrow's root.
    // Keep this constant identical to JemparinganArrow.TipOffset.
    private const float ArrowTipOffset = 0.36f;

    // Trace the crosshair ray against the actual front faces of all Modern
    // boards. Prefer a board currently under the reticle; for a miss use the
    // closest board's PLANE, keeping the intended aim point on the ray.
    // A miss stays a miss; there is never any attraction to the bullseye.
    public static bool TryZeroToCameraRay(Camera camera, Vector3 origin,
        float speed, float gravity, float maxFlightSeconds,
        JemparinganModernTarget[] targets, out Vector3 direction,
        out bool crosshairOnBoard)
    {
        // Original zeroing path is preserved verbatim for shots without
        // physical-sight wobble, including older tests and legacy Modern V1.
        return TryZeroToSightRay(camera,
            camera != null ? camera.transform.forward : Vector3.forward,
            origin, speed, gravity, maxFlightSeconds,
            targets, out direction, out crosshairOnBoard);
    }

    // During Modern optical aim wobble, use the exact ray from the player's
    // eye THROUGH the visible calibrated +. This avoids a hidden mismatch
    // where the reticle moves but arrows still fly to camera screen center.
    // The SAME ballistic solver still compensates gravity and parallax,
    // without any target-center attraction.
    public static bool TryZeroToSightRay(Camera camera,
        Vector3 sightRayDirection, Vector3 origin,
        float speed, float gravity, float maxFlightSeconds,
        JemparinganModernTarget[] targets, out Vector3 direction,
        out bool crosshairOnBoard)
    {
        direction = camera != null ? camera.transform.forward : Vector3.forward;
        crosshairOnBoard = false;
        if (camera == null || targets == null || targets.Length == 0 ||
            speed < .1f)
            return false;

        Vector3 rayOrigin = camera.transform.position;
        Vector3 rayDirection = sightRayDirection.sqrMagnitude > .000001f
            ? sightRayDirection.normalized
            : camera.transform.forward.normalized;
        bool found = false;
        bool foundBoard = false;
        float bestScore = float.PositiveInfinity;
        Vector3 selectedPoint = Vector3.zero;
        for (int i = 0; i < targets.Length; i++)
        {
            JemparinganModernTarget target = targets[i];
            if (target == null || !target.gameObject.activeInHierarchy)
                continue;

            Transform face = target.transform;
            Vector3 faceOrigin = face.TransformPoint(
                new Vector3(0f, 0f, JemparinganModernTarget.FaceDepth));
            Vector3 normal = face.forward;
            float denominator = Vector3.Dot(rayDirection, normal);
            if (Mathf.Abs(denominator) < .0001f) continue;
            float distance = Vector3.Dot(faceOrigin - rayOrigin, normal) /
                denominator;
            if (distance <= .05f) continue;

            Vector3 rayHit = rayOrigin + rayDirection * distance;
            Vector3 localHit = face.InverseTransformPoint(rayHit);
            bool onBoard = (localHit.x * localHit.x +
                localHit.y * localHit.y) <=
                JemparinganModernTarget.Radius * JemparinganModernTarget.Radius;
            // A direct face intersection always wins. Of simultaneous hits,
            // the closer face is the one the player actually sees.
            // Otherwise pick the most aligned target as a range reference,
            // but keep the point ON THE CAMERA RAY, outside its rings.
            float alignment = Vector3.Angle(rayDirection,
                (faceOrigin - rayOrigin).normalized);
            float score = onBoard ? distance : 100000f + alignment;
            if (found && score >= bestScore) continue;

            found = true;
            foundBoard = onBoard;
            bestScore = score;
            selectedPoint = rayHit;
        }
        if (!found) return false;

        // The root is behind the arrow tip; the collision system moves the
        // tip according to the flight velocity's instantaneous direction.
        // Iteratively back the impact root away from the chosen sight point.
        // This corrects both parallax (camera vs launch origin) and drop.
        Vector3 rootGoal = selectedPoint;
        Vector3 velocity = Vector3.zero;
        float flightTime = 0f;
        for (int iteration = 0; iteration < 4; iteration++)
        {
            if (!TryLowArcVelocity(origin, rootGoal, speed, gravity,
                out velocity, out flightTime))
                return false;
            if (flightTime > maxFlightSeconds) return false;
            Vector3 impactVelocity = velocity + Vector3.down *
                (gravity * flightTime);
            if (impactVelocity.sqrMagnitude < .0001f) return false;
            rootGoal = selectedPoint -
                impactVelocity.normalized * ArrowTipOffset;
        }
        // Last correction uses the same fixed speed as Launch().
        if (!TryLowArcVelocity(origin, rootGoal, speed, gravity,
            out velocity, out flightTime) ||
            flightTime > maxFlightSeconds)
            return false;

        direction = velocity.normalized;
        crosshairOnBoard = foundBoard;
        return true;
    }

    // Share Modern's ballistic zeroing with Traditional's narrow bandulan.
    // The visible sight ray determines the exact point on the target plane:
    // never snap towards molo, jangga, awak or the moving target center.
    public static bool TryZeroToBandulanSightRay(Camera camera,
        Vector3 sightRayDirection, Vector3 origin,
        float speed, float gravity, float maxFlightSeconds,
        Transform bandulan, out Vector3 direction,
        out bool reticleOnBandulan)
    {
        direction = camera != null ? camera.transform.forward : Vector3.forward;
        reticleOnBandulan = false;
        if (camera == null || bandulan == null || speed < .1f)
            return false;

        Vector3 rayOrigin = camera.transform.position;
        Vector3 rayDirection = sightRayDirection.sqrMagnitude > .000001f
            ? sightRayDirection.normalized : camera.transform.forward.normalized;
        Vector3 normal = bandulan.forward;
        float denominator = Vector3.Dot(rayDirection, normal);
        if (Mathf.Abs(denominator) < .0001f) return false;
        float distance = Vector3.Dot(bandulan.position - rayOrigin, normal) /
            denominator;
        if (distance <= .05f) return false;
        Vector3 selectedPoint = rayOrigin + rayDirection * distance;
        Vector3 localHit = bandulan.InverseTransformPoint(selectedPoint);
        reticleOnBandulan = Mathf.Abs(localHit.x) <= .07f &&
            localHit.y >= JemparinganTarget.TraditionalBottom &&
            localHit.y <= JemparinganTarget.TraditionalTop;

        Vector3 rootGoal = selectedPoint;
        Vector3 velocity = Vector3.zero;
        float flightTime = 0f;
        for (int iteration = 0; iteration < 4; iteration++)
        {
            if (!TryLowArcVelocity(origin, rootGoal, speed, gravity,
                out velocity, out flightTime) ||
                flightTime > maxFlightSeconds)
                return false;
            Vector3 impactVelocity = velocity + Vector3.down *
                (gravity * flightTime);
            if (impactVelocity.sqrMagnitude < .0001f) return false;
            rootGoal = selectedPoint -
                impactVelocity.normalized * ArrowTipOffset;
        }
        if (!TryLowArcVelocity(origin, rootGoal, speed, gravity,
            out velocity, out flightTime) ||
            flightTime > maxFlightSeconds)
            return false;
        direction = velocity.normalized;
        return true;
    }

    // Solve |delta/t + up*(gravity*t/2)| = fixed launch speed.
    // u=t^2 gives (g^2/4)*u^2 + (g*delta.y-speed^2)*u +
    // |delta|^2 = 0. Choose the shorter flight (low, direct arc).
    private static bool TryLowArcVelocity(Vector3 origin, Vector3 point,
        float speed, float gravity, out Vector3 velocity, out float time)
    {
        velocity = Vector3.zero;
        time = 0f;
        Vector3 delta = point - origin;
        float distance = delta.magnitude;
        if (distance < .05f || speed < .1f) return false;

        gravity = Mathf.Max(0f, gravity);
        if (gravity <= .0001f)
        {
            time = distance / speed;
            velocity = delta / time;
            return true;
        }

        float speed2 = speed * speed;
        float a = .25f * gravity * gravity;
        float b = gravity * delta.y - speed2;
        float c = delta.sqrMagnitude;
        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f) return false; // Unreachable at this speed.

        // Rationalized small root avoids catastrophic cancellation at
        // short range or low gravity.
        float denominator = -b + Mathf.Sqrt(discriminant);
        if (denominator <= .00001f) return false;
        float timeSquared = 2f * c / denominator;
        if (timeSquared <= .00001f) return false;
        time = Mathf.Sqrt(timeSquared);
        velocity = delta / time +
            Vector3.up * (.5f * gravity * time);
        return true;
    }
}

using UnityEngine;

public static class SpringPhysics
{
    /// <summary>
    /// Computes the spring + damping force applied to node A from a spring connecting A to B.
    /// </summary>
    public static Vector2 ComputeSpringForce(
        Vector2 posA, Vector2 posB,
        Vector2 velA, Vector2 velB,
        float restLength, float springK, float damping)
    {
        Vector2 delta = posB - posA;
        float dist = delta.magnitude;
        if (dist < 1e-6f) return Vector2.zero;

        Vector2 dir = delta / dist;
        float stretch = dist - restLength;

        float radialRelVel = Vector2.Dot(velB - velA, dir);

        float forceMag = springK * stretch + damping * radialRelVel;
        return dir * forceMag;
    }

    /// <summary>
    /// Adjusts rawAngle so that it is within pi of prevAngle, producing a continuous unwrapped angle.
    /// </summary>
    public static float UnwrapAngle(float rawAngle, float prevAngle)
    {
        float diff = rawAngle - prevAngle;
        while (diff > Mathf.PI) diff -= 2f * Mathf.PI;
        while (diff < -Mathf.PI) diff += 2f * Mathf.PI;
        return prevAngle + diff;
    }

    /// <summary>
    /// Computes restoring torque for an angular spring.
    /// </summary>
    public static float ComputeAngularTorque(
        float currentAngle, float initialAngle,
        float angularK, float angularVel, float angularDamping)
    {
        float angleDiff = currentAngle - initialAngle;
        return -angularK * angleDiff - angularDamping * angularVel;
    }
}

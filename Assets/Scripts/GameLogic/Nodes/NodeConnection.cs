using UnityEngine;

public class NodeConnection
{
    public Node NodeA;
    public Node NodeB;
    public bool IsBalloonConnection;
    public float RestLength;
    public bool IsBroken;

    private float _initialRelAngleA;
    private float _initialRelAngleB;
    private float _prevRelAngleA;
    private float _prevRelAngleB;
    private float _referenceAngleOffsetA;
    private float _referenceAngleOffsetB;

    public NodeConnection(Node a, Node b)
    {
        NodeA = a;
        NodeB = b;
        IsBalloonConnection = (a is BalloonNode) || (b is BalloonNode);
    }

    public void Initialize()
    {
        Vector2 delta = (Vector2)NodeB.transform.position - (Vector2)NodeA.transform.position;
        RestLength = delta.magnitude;
        IsBroken = false;

        float rawAngle = Mathf.Atan2(delta.y, delta.x);
        _referenceAngleOffsetA = 0f;
        _referenceAngleOffsetB = 0f;

        if (NodeA.CanRotate)
        {
            float nodeAngle = NodeA.Rb.rotation * Mathf.Deg2Rad;
            _initialRelAngleA = rawAngle - nodeAngle;
            _prevRelAngleA = _initialRelAngleA;
        }
        if (NodeB.CanRotate)
        {
            float nodeAngle = NodeB.Rb.rotation * Mathf.Deg2Rad;
            _initialRelAngleB = (rawAngle + Mathf.PI) - nodeAngle;
            _prevRelAngleB = _initialRelAngleB;
        }
    }

    public void ComputeAndApplyForces()
    {
        if (IsBroken) return;
        if (NodeA == null || NodeB == null)
        {
            IsBroken = true;
            return;
        }

        Vector2 posA = NodeA.Rb.position;
        Vector2 posB = NodeB.Rb.position;
        Vector2 velA = NodeA.Rb.velocity;
        Vector2 velB = NodeB.Rb.velocity;

        float dist = Vector2.Distance(posA, posB);
        var cfg = NodeConfig.Instance;

        if (Mathf.Abs(dist - RestLength) > cfg.SpringBreakLength)
        {
            IsBroken = true;
            return;
        }

        // Radial spring force
        Vector2 springForce = SpringPhysics.ComputeSpringForce(
            posA, posB, velA, velB,
            RestLength, cfg.SpringK, cfg.SpringDamping);

        NodeA.Rb.AddForce(springForce);
        NodeB.Rb.AddForce(-springForce);

        // Balloon connection only keeps radial spring; no torque exchange.
        if (IsBalloonConnection) return;

        // Angular spring / tangential forces
        Vector2 delta = posB - posA;
        if (dist < 1e-6f) return;

        float rawAngle = Mathf.Atan2(delta.y, delta.x);

        if (NodeA.CanRotate)
        {
            float nodeAngleRad = NodeA.Rb.rotation * Mathf.Deg2Rad;
            float rawRelAngle = rawAngle - nodeAngleRad;
            float relAngle = SpringPhysics.UnwrapAngle(rawRelAngle, _prevRelAngleA);
            _prevRelAngleA = relAngle;
            _referenceAngleOffsetA += NodeA.GetConnectionReferenceAngularSpeedRad() * Time.fixedDeltaTime;
            float targetRelAngle = _initialRelAngleA + _referenceAngleOffsetA;

            float torque = SpringPhysics.ComputeAngularTorque(
                relAngle, targetRelAngle,
                cfg.AngularSpringK, NodeA.Rb.angularVelocity * Mathf.Deg2Rad,
                cfg.AngularSpringDamping);

            NodeA.Rb.AddTorque(torque);

            float tangentialForceMag = -torque / dist;
            Vector2 tangent = new Vector2(-delta.y, delta.x) / dist;
            NodeB.Rb.AddForce(tangent * tangentialForceMag);
            NodeA.Rb.AddForce(-tangent * tangentialForceMag);
        }

        if (NodeB.CanRotate)
        {
            float nodeAngleRad = NodeB.Rb.rotation * Mathf.Deg2Rad;
            float rawRelAngle = (rawAngle + Mathf.PI) - nodeAngleRad;
            float relAngle = SpringPhysics.UnwrapAngle(rawRelAngle, _prevRelAngleB);
            _prevRelAngleB = relAngle;
            _referenceAngleOffsetB += NodeB.GetConnectionReferenceAngularSpeedRad() * Time.fixedDeltaTime;
            float targetRelAngle = _initialRelAngleB + _referenceAngleOffsetB;

            float torque = SpringPhysics.ComputeAngularTorque(
                relAngle, targetRelAngle,
                cfg.AngularSpringK, NodeB.Rb.angularVelocity * Mathf.Deg2Rad,
                cfg.AngularSpringDamping);

            NodeB.Rb.AddTorque(torque);

            float tangentialForceMag = -torque / dist;
            Vector2 tangent = new Vector2(-delta.y, delta.x) / dist;
            NodeA.Rb.AddForce(-tangent * tangentialForceMag);
            NodeB.Rb.AddForce(tangent * tangentialForceMag);
        }
    }

    public bool Involves(Node node)
    {
        return NodeA == node || NodeB == node;
    }

    public bool IsElectrified()
    {
        if (NodeA == null || NodeB == null) return false;
        return (NodeA.CanCharge && NodeB.IsCharged) ||
               (NodeB.CanCharge && NodeA.IsCharged);
    }
}

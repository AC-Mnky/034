using UnityEngine;

public class NodeConnection
{
    public Node NodeA;
    public Node NodeB;
    public float RestLength;
    public bool IsBroken;

    // Per-endpoint angle tracking for rotating nodes
    private float _initialAngleA;
    private float _initialAngleB;
    private float _prevAngleA;
    private float _prevAngleB;
    private float _initialRelAngleA;
    private float _initialRelAngleB;

    public NodeConnection(Node a, Node b)
    {
        NodeA = a;
        NodeB = b;
    }

    public void Initialize()
    {
        Vector2 delta = (Vector2)NodeB.transform.position - (Vector2)NodeA.transform.position;
        RestLength = delta.magnitude;
        IsBroken = false;

        float rawAngle = Mathf.Atan2(delta.y, delta.x);
        _initialAngleA = rawAngle;
        _prevAngleA = rawAngle;
        _initialAngleB = rawAngle + Mathf.PI;
        _prevAngleB = rawAngle + Mathf.PI;

        if (NodeA.CanRotate)
            _initialRelAngleA = rawAngle - NodeA.transform.eulerAngles.z * Mathf.Deg2Rad;
        if (NodeB.CanRotate)
            _initialRelAngleB = (rawAngle + Mathf.PI) - NodeB.transform.eulerAngles.z * Mathf.Deg2Rad;
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

        if (Mathf.Abs(dist - RestLength) > Node.SpringBreakLength)
        {
            IsBroken = true;
            return;
        }

        // Radial spring force
        Vector2 springForce = SpringPhysics.ComputeSpringForce(
            posA, posB, velA, velB,
            RestLength, Node.SpringK, Node.SpringDamping);

        NodeA.Rb.AddForce(springForce);
        NodeB.Rb.AddForce(-springForce);

        // Angular spring / tangential forces
        Vector2 delta = posB - posA;
        if (dist < 1e-6f) return;

        float rawAngle = Mathf.Atan2(delta.y, delta.x);

        if (NodeA.CanRotate)
        {
            float unwrapped = SpringPhysics.UnwrapAngle(rawAngle, _prevAngleA);
            _prevAngleA = unwrapped;

            float nodeAngleRad = NodeA.transform.eulerAngles.z * Mathf.Deg2Rad;
            float relAngle = unwrapped - nodeAngleRad;

            float torque = SpringPhysics.ComputeAngularTorque(
                relAngle, _initialRelAngleA,
                Node.AngularSpringK, NodeA.Rb.angularVelocity * Mathf.Deg2Rad,
                Node.AngularSpringDamping);

            NodeA.Rb.AddTorque(torque);

            float tangentialForceMag = -torque / dist;
            Vector2 tangent = new Vector2(-delta.y, delta.x) / dist;
            NodeB.Rb.AddForce(tangent * tangentialForceMag);
            NodeA.Rb.AddForce(-tangent * tangentialForceMag);
        }

        if (NodeB.CanRotate)
        {
            float rawAngleB = rawAngle + Mathf.PI;
            float unwrappedB = SpringPhysics.UnwrapAngle(rawAngleB, _prevAngleB);
            _prevAngleB = unwrappedB;

            float nodeAngleRad = NodeB.transform.eulerAngles.z * Mathf.Deg2Rad;
            float relAngle = unwrappedB - nodeAngleRad;

            float torque = SpringPhysics.ComputeAngularTorque(
                relAngle, _initialRelAngleB,
                Node.AngularSpringK, NodeB.Rb.angularVelocity * Mathf.Deg2Rad,
                Node.AngularSpringDamping);

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
}

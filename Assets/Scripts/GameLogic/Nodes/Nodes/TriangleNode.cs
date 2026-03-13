using UnityEngine;

[RequireComponent(typeof(TriangleNodeGeometry))]
public class TriangleNode : Node
{
    protected override void Awake()
    {
        base.Awake();
        CanRotate = true;
    }
}

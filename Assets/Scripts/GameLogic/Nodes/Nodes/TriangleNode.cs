using UnityEngine;

[RequireComponent(typeof(TriangleNodeGeometry))]
[RequireComponent(typeof(TriangleRotateUI))]
public class TriangleNode : Node
{
    protected override void Awake()
    {
        base.Awake();
        CanRotate = true;
    }
}

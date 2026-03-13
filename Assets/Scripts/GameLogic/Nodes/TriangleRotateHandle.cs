using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class TriangleRotateHandle : MonoBehaviour
{
    public TriangleRotateUI Owner;

    private void OnMouseDown()
    {
        Owner?.BeginHandleDrag();
    }

    private void OnMouseDrag()
    {
        Owner?.UpdateRotationFromMouse();
    }

    private void OnMouseUp()
    {
        Owner?.EndHandleDrag();
    }
}

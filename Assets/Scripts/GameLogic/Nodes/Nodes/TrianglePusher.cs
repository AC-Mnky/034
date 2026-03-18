using UnityEngine;

[RequireComponent(typeof(TriangleNodeGeometry))]
[RequireComponent(typeof(TriangleRotateUI))]
[DefaultExecutionOrder(-100)]
public class TrianglePusher : Node
{
    [Header("Pusher")]
    public float ForwardPushForce = 16f;

    protected override void Awake()
    {
        base.Awake();
        CanRotate = true;
        CanConduct = true;
    }

    private void FixedUpdate()
    {
        if (!gameObject.activeInHierarchy || IsInInventory) return;
        if (Rb == null || !HasElectricity) return;
        if (ConnectionManager.Instance == null || ConnectionManager.Instance.CurrentState != LevelState.Run) return;

        Rb.AddForce((Vector2)transform.up * ForwardPushForce, ForceMode2D.Force);
    }
}

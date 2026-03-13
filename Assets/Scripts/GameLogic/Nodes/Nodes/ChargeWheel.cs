using UnityEngine;

public class ChargeWheel : Node
{
    [Header("Charge Wheel")]
    public float PoweredClockwiseAngularSpeedDeg = 360f;

    protected override void Awake()
    {
        base.Awake();
        CanRotate = true;
        CanConduct = true;
    }

    public override float GetConnectionReferenceAngularSpeedRad()
    {
        if (!HasElectricity) return 0f;
        if (ConnectionManager.Instance == null || ConnectionManager.Instance.CurrentState != LevelState.Run)
            return 0f;

        // Unity 2D: positive is CCW, so clockwise is negative?
        return PoweredClockwiseAngularSpeedDeg * Mathf.Deg2Rad;
    }
}
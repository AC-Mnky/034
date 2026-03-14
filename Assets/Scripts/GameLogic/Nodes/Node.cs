using System.Collections.Generic;
using UnityEngine;

public abstract class Node : MonoBehaviour
{
    [Header("Physics Properties")]
    public bool CanRotate;
    public bool CanCollide = true;
    public float Mass = 1f;

    [Header("Electrical Properties")]
    public bool CanCharge;
    public bool CanConduct;

    [HideInInspector] public List<NodeConnection> ActiveConnections = new List<NodeConnection>();
    [HideInInspector] public bool IsInInventory = true;

    public Rigidbody2D Rb { get; private set; }
    public Collider2D Col { get; private set; }
    public bool IsCharged { get; private set; }
    public bool IsConductive => CanCharge || CanConduct;
    public bool HasElectricity => CanCharge || IsCharged;

    public string NodeType => GetType().Name;

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        if (Rb == null)
        {
            Rb = gameObject.AddComponent<Rigidbody2D>();
        }
        Rb.bodyType = RigidbodyType2D.Kinematic;
        Rb.mass = Mass;

        Col = GetComponent<Collider2D>();
        if (Col != null) Col.enabled = false;
    }

    public void EnterRunMode()
    {
        if (IsInInventory)
        {
            gameObject.SetActive(false);
            return;
        }

        Rb.bodyType = RigidbodyType2D.Dynamic;
        Rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (CanCollide)
        {
            if (Col != null) Col.enabled = true;
        }
        else
        {
            // Rb.gravityScale = 0f;
            if (Col != null) Col.enabled = false;
        }

        if (!CanRotate)
        {
            Rb.freezeRotation = true;
        }

        Rb.mass = Mass;
    }

    public void EnterBuildMode()
    {
        Rb.bodyType = RigidbodyType2D.Kinematic;
        Rb.velocity = Vector2.zero;
        Rb.angularVelocity = 0f;
        if (Col != null) Col.enabled = true;
    }

    public virtual void OnRuntimeClick() { }

    // Reference angular speed used by connection angular springs (rad/s).
    // Positive means CCW in Unity 2D; negative means clockwise.
    public virtual float GetConnectionReferenceAngularSpeedRad()
    {
        return 0f;
    }

    public void SetChargedState(bool isCharged)
    {
        IsCharged = isCharged;
    }

    public int GetActiveConnectionCount()
    {
        return ActiveConnections.Count;
    }
}

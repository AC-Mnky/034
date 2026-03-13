using System.Collections.Generic;
using UnityEngine;

public abstract class Node : MonoBehaviour
{
    [Header("Connection Settings")]
    public float MaxConnectRadius = 3f;
    public float MinConnectDistance = 2f;
    public int MaxConnectNumber = 3;

    [Header("Physics Properties")]
    public bool CanRotate;
    public bool CanCollide = true;
    public float Mass = 1f;

    public static float SpringK = 500f;
    public static float SpringDamping = 10f;
    public static float SpringBreakLength = 2f;
    public static float AngularSpringK = 50f;
    public static float AngularSpringDamping = 5f;

    [HideInInspector] public List<NodeConnection> ActiveConnections = new List<NodeConnection>();
    [HideInInspector] public bool IsInInventory = true;

    public Rigidbody2D Rb { get; private set; }
    public Collider2D Col { get; private set; }

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
            Rb.gravityScale = 0f;
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

    public int GetActiveConnectionCount()
    {
        return ActiveConnections.Count;
    }
}

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
    [HideInInspector] public GameObject SourcePrefab;

    public Rigidbody2D Rb { get; private set; }
    public Collider2D Col { get; private set; }
    public bool IsCharged { get; private set; }
    public bool IsConductive => CanCharge || CanConduct;
    public bool HasElectricity => CanCharge || IsCharged;
    private bool _defaultIsTrigger;
    private ParticleSystem[] _particleSystems;
    private bool _lastParticleEmissionEnabled;
    private bool _hasParticleEmissionState;

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
        if (Col != null)
        {
            _defaultIsTrigger = Col.isTrigger;
            Col.enabled = false;
        }

        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        RefreshParticleEmission(force: true);
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
            if (Col != null)
            {
                Col.isTrigger = _defaultIsTrigger;
                Col.enabled = true;
            }
        }
        else
        {
            if (Col != null)
            {
                // Use trigger collider so non-collidable nodes can still reach GoalTrigger.
                Col.isTrigger = true;
                Col.enabled = true;
            }
        }

        if (!CanRotate)
        {
            Rb.freezeRotation = true;
        }

        Rb.mass = Mass;
        RefreshParticleEmission(force: true);
    }

    public void EnterBuildMode()
    {
        Rb.bodyType = RigidbodyType2D.Kinematic;
        Rb.velocity = Vector2.zero;
        Rb.angularVelocity = 0f;
        if (Col != null)
        {
            Col.isTrigger = _defaultIsTrigger;
            Col.enabled = true;
        }

        RefreshParticleEmission(force: true);
    }

    public virtual void OnRuntimeClick() { }

    private void LateUpdate()
    {
        RefreshParticleEmission(force: false);
    }

    // Reference angular speed used by connection angular springs (rad/s).
    // Positive means CCW in Unity 2D; negative means clockwise.
    public virtual float GetConnectionReferenceAngularSpeedRad()
    {
        return 0f;
    }

    public void SetChargedState(bool isCharged)
    {
        IsCharged = isCharged;
        RefreshParticleEmission(force: false);
    }

    public int GetActiveConnectionCount()
    {
        return ActiveConnections.Count;
    }

    private void RefreshParticleEmission(bool force)
    {
        if (_particleSystems == null || _particleSystems.Length == 0) return;

        bool shouldEmit = ShouldEmitParticles();
        if (!force && _hasParticleEmissionState && _lastParticleEmissionEnabled == shouldEmit) return;

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            var ps = _particleSystems[i];
            if (ps == null) continue;

            var emission = ps.emission;
            emission.enabled = shouldEmit;
        }

        _lastParticleEmissionEnabled = shouldEmit;
        _hasParticleEmissionState = true;
    }

    private bool ShouldEmitParticles()
    {
        if (!gameObject.activeInHierarchy || IsInInventory) return false;

        LevelState state = LevelState.Build;
        if (LevelManager.Instance != null)
            state = LevelManager.Instance.CurrentState;
        else if (ConnectionManager.Instance != null)
            state = ConnectionManager.Instance.CurrentState;

        if (state != LevelState.Run) return false;
        if (IsConductive && !HasElectricity) return false;
        return true;
    }
}

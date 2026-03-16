using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public bool IsReached { get; private set; }

    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        ApplyMaterial();
    }

    public void SetReached(bool reached)
    {
        if (IsReached == reached) return;
        IsReached = reached;
        ApplyMaterial();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var node = other.GetComponent<Node>();
        if (node == null) return;
        if (IsReached) return;

        if (LevelManager.Instance == null) return;
        if (LevelManager.Instance.CurrentState != LevelState.Run) return;
        LevelManager.Instance.NotifyGoalTriggered(this);
    }

    private void ApplyMaterial()
    {
        if (_renderer == null) return;
        var cfg = ColorConfig.Instance;
        if (cfg == null) return;

        Material target = IsReached ? cfg.GoalReachedMaterial : cfg.GoalDefaultMaterial;
        if (target == null) return;
        _renderer.sharedMaterial = target;
    }
}

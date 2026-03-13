using UnityEngine;

public class NodeRenderer : MonoBehaviour
{
    [Header("Visual")]
    public Color NormalColor = Color.white;
    public Color InventoryColor = new Color(1f, 1f, 1f, 0.5f);

    private Node _node;
    private SpriteRenderer _sr;
    private MeshRenderer _mr;
    private bool? _lastChargedState;

    private void Awake()
    {
        _node = GetComponent<Node>();
        _sr = GetComponent<SpriteRenderer>();
        _mr = GetComponent<MeshRenderer>();
        if (_sr == null && _mr == null)
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    private void LateUpdate()
    {
        if (_node == null) return;

        if (_sr != null)
            _sr.color = _node.IsInInventory ? InventoryColor : NormalColor;

        UpdateMeshMaterial();
    }

    private void UpdateMeshMaterial()
    {
        if (_mr == null) return;
        if (!_node.IsConductive) return;

        bool isCharged = _node.HasElectricity;
        if (_lastChargedState.HasValue && _lastChargedState.Value == isCharged) return;

        var cfg = ColorConfig.Instance;
        if (cfg == null) return;

        Material target = isCharged ? cfg.ChargedMaterial : cfg.UnchargedMaterial;
        if (target == null) return;

        _mr.sharedMaterial = target;
        _lastChargedState = isCharged;
    }
}

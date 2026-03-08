using UnityEngine;

public class NodeRenderer : MonoBehaviour
{
    [Header("Visual")]
    public Color NormalColor = Color.white;
    public Color InventoryColor = new Color(1f, 1f, 1f, 0.5f);

    private Node _node;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _node = GetComponent<Node>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null)
            _sr = gameObject.AddComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (_node == null || _sr == null) return;
        _sr.color = _node.IsInInventory ? InventoryColor : NormalColor;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class NodeDragHandler : MonoBehaviour
{
    private Node _node;
    private Camera _mainCamera;
    private bool _isDragging;
    private Vector2 _startPosition;
    private bool _wasInInventory;

    [HideInInspector] public Vector2[] BuildAreaPolygon;
    [HideInInspector] public Rect InventoryArea;

    private ConnectionManager _connMgr;
    private List<(Node, Node)> _previewConnections = new List<(Node, Node)>();

    public bool IsDragging => _isDragging;
    public List<(Node, Node)> PreviewConnections => _previewConnections;

    private void Awake()
    {
        _node = GetComponent<Node>();
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        _connMgr = ConnectionManager.Instance;
    }

    private void OnMouseDown()
    {
        if (_connMgr == null || _connMgr.CurrentState != LevelState.Build) return;
        if (TriangleRotateUI.IsAnyHandleDragging) return;
        if (IsMouseOnRotateHandle()) return;

        TriangleRotateUI.OnNodePointerDown(_node);

        _isDragging = true;
        _startPosition = transform.position;
        _wasInInventory = _node.IsInInventory;
    }

    private void OnMouseDrag()
    {
        if (!_isDragging) return;

        Vector3 screenPos = Input.mousePosition;
        screenPos.z = -_mainCamera.transform.position.z;
        Vector2 mousePos = _mainCamera.ScreenToWorldPoint(screenPos);
        transform.position = new Vector3(mousePos.x, mousePos.y, transform.position.z);

        _node.IsInInventory = InventoryArea.Contains(mousePos);

        _connMgr.Strategy.OnNodeDragged(_node, _connMgr);

        _previewConnections = _connMgr.Strategy.GetPreviewConnections(_node, _connMgr.AllNodes);
    }

    private void OnMouseUp()
    {
        if (!_isDragging) return;
        _isDragging = false;
        _previewConnections.Clear();

        Vector2 pos = transform.position;

        bool inBuildArea = BuildAreaPolygon != null && LevelManager.PointInPolygon(pos, BuildAreaPolygon);
        bool inInventory = InventoryArea.Contains(pos);

        if (!inBuildArea && !inInventory)
        {
            transform.position = new Vector3(_startPosition.x, _startPosition.y, transform.position.z);
            _node.IsInInventory = _wasInInventory;
            return;
        }

        if (inBuildArea && HasOverlap(pos))
        {
            transform.position = new Vector3(_startPosition.x, _startPosition.y, transform.position.z);
            _node.IsInInventory = _wasInInventory;
            return;
        }

        _node.IsInInventory = inInventory;

        if (!_node.IsInInventory)
        {
            _connMgr.Strategy.OnNodePlaced(_node, _connMgr.AllNodes, _connMgr);
        }
    }

    private bool HasOverlap(Vector2 pos)
    {
        float checkRadius = 0.3f;
        var hits = Physics2D.OverlapCircleAll(pos, checkRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.GetComponent<Node>() != null)
                return true;
        }
        return false;
    }

    private bool IsMouseOnRotateHandle()
    {
        if (_mainCamera == null) return false;
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = -_mainCamera.transform.position.z;
        Vector2 mouseWorld = _mainCamera.ScreenToWorldPoint(screenPos);

        var hits = Physics2D.OverlapPointAll(mouseWorld);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<TriangleRotateHandle>() != null)
                return true;
        }
        return false;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class NodeDragHandler : MonoBehaviour
{
    private static readonly Collider2D[] OverlapResults = new Collider2D[64];

    private Node _node;
    private Camera _mainCamera;
    private bool _isDragging;
    private Vector2 _startPosition;
    private bool _wasInInventory;
    private Vector2 _dragOffset;

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
        if (LevelManager.IsInputLocked) return;
        if (_connMgr == null || _connMgr.CurrentState != LevelState.Build) return;
        if (TriangleRotateUI.IsAnyHandleDragging) return;
        if (IsMouseOnRotateHandle()) return;

        TriangleRotateUI.OnNodePointerDown(_node);

        _isDragging = true;
        _startPosition = transform.position;
        _wasInInventory = _node.IsInInventory;
        _dragOffset = (Vector2)transform.position - GetMouseWorldPosition();
    }

    private void OnMouseDrag()
    {
        if (LevelManager.IsInputLocked)
        {
            _isDragging = false;
            _previewConnections.Clear();
            return;
        }
        if (!_isDragging) return;

        Vector2 mousePos = GetMouseWorldPosition();
        Vector2 targetPos = mousePos + _dragOffset;
        transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);

        _node.IsInInventory = InventoryArea.Contains(targetPos);

        _connMgr.Strategy.OnNodeDragged(_node, _connMgr);

        _previewConnections = _connMgr.Strategy.GetPreviewConnections(_node, _connMgr.AllNodes);
    }

    private void OnMouseUp()
    {
        if (LevelManager.IsInputLocked)
        {
            _isDragging = false;
            _previewConnections.Clear();
            return;
        }
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
        if (_node == null || _node.Col == null) return false;

        Vector3 oldPos = transform.position;
        bool movedForCheck = (Vector2)oldPos != pos;
        if (movedForCheck)
        {
            transform.position = new Vector3(pos.x, pos.y, oldPos.z);
            Physics2D.SyncTransforms();
        }

        var filter = new ContactFilter2D
        {
            useLayerMask = false,
            useDepth = false,
            useTriggers = true
        };

        int count = _node.Col.OverlapCollider(filter, OverlapResults);

        if (movedForCheck)
        {
            transform.position = oldPos;
            Physics2D.SyncTransforms();
        }

        for (int i = 0; i < count; i++)
        {
            var hit = OverlapResults[i];
            if (hit == null) continue;
            if (hit == _node.Col) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            return true;
        }

        return false;
    }

    private bool IsMouseOnRotateHandle()
    {
        if (_mainCamera == null) return false;
        Vector2 mouseWorld = GetMouseWorldPosition();

        var hits = Physics2D.OverlapPointAll(mouseWorld);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<TriangleRotateHandle>() != null)
                return true;
        }
        return false;
    }

    private Vector2 GetMouseWorldPosition()
    {
        if (_mainCamera == null) return transform.position;
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = -_mainCamera.transform.position.z;
        return _mainCamera.ScreenToWorldPoint(screenPos);
    }
}

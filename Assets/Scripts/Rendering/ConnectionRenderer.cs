using System.Collections.Generic;
using UnityEngine;

public class ConnectionRenderer : MonoBehaviour
{
    public static ConnectionRenderer Instance { get; private set; }

    [Header("Line Settings")]
    public float LineWidth = 0.05f;
    public Material LineMaterial;

    private ConnectionManager _connMgr;
    private List<LineRenderer> _linePool = new List<LineRenderer>();
    private int _activeCount;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _connMgr = ConnectionManager.Instance;
    }

    private void LateUpdate()
    {
        if (_connMgr == null) return;

        _activeCount = 0;
        CollectDragState(out var draggedNodes, out var mergedPreviews);

        foreach (var conn in _connMgr.AllConnections)
        {
            if (conn.IsBroken || conn.NodeA == null || conn.NodeB == null) continue;
            bool isDragRelated = draggedNodes.Contains(conn.NodeA) || draggedNodes.Contains(conn.NodeB);
            bool electrified = isDragRelated
                ? _connMgr.WillConnectionBeElectrified(conn.NodeA, conn.NodeB, mergedPreviews)
                : conn.IsElectrified();
            Color color = GetConnectionColor(electrified, isDragRelated, conn.IsBalloonConnection);
            DrawLine(conn.NodeA.transform.position, conn.NodeB.transform.position, color);
        }

        DrawPreviews(mergedPreviews);

        for (int i = _activeCount; i < _linePool.Count; i++)
        {
            _linePool[i].enabled = false;
        }
    }

    private void DrawPreviews(List<(Node, Node)> mergedPreviews)
    {
        if (_connMgr.CurrentState != LevelState.Build) return;
        if (mergedPreviews.Count == 0) return;

        foreach (var (a, b) in mergedPreviews)
        {
            if (a == null || b == null) continue;
            bool electrified = _connMgr.WillConnectionBeElectrified(a, b, mergedPreviews);
            bool isBalloonConnection = a is BalloonNode || b is BalloonNode;
            Color color = GetConnectionColor(electrified, true, isBalloonConnection);
            DrawLine(a.transform.position, b.transform.position, color);
        }
    }

    private void CollectDragState(out HashSet<Node> draggedNodes, out List<(Node, Node)> mergedPreviews)
    {
        draggedNodes = new HashSet<Node>();
        mergedPreviews = new List<(Node, Node)>();
        if (_connMgr.CurrentState != LevelState.Build) return;

        foreach (var node in _connMgr.AllNodes)
        {
            var drag = node.GetComponent<NodeDragHandler>();
            if (drag == null || !drag.IsDragging) continue;

            draggedNodes.Add(node);
            var previews = drag.PreviewConnections;
            for (int i = 0; i < previews.Count; i++)
            {
                mergedPreviews.Add(previews[i]);
            }
        }
    }

    private Color GetConnectionColor(bool electrified, bool isFaded, bool isBalloonConnection)
    {
        var cfg = ConnectionColorConfig.Instance;
        Color connectionColor = cfg != null ? cfg.ConnectionColor : Color.white;
        Color chargedColor = cfg != null ? cfg.ChargedConnectionColor : Color.green;
        Color previewColor = cfg != null ? cfg.PreviewColor : new Color(1f, 1f, 1f, 0.4f);
        Color balloonColor = cfg != null ? cfg.BalloonConnectionColor : new Color(1f, 0.8f, 0.2f, 1f);

        if (isBalloonConnection)
        {
            return isFaded
                ? new Color(balloonColor.r, balloonColor.g, balloonColor.b, previewColor.a)
                : balloonColor;
        }

        if (electrified)
        {
            return isFaded
                ? new Color(chargedColor.r, chargedColor.g, chargedColor.b, previewColor.a)
                : chargedColor;
        }

        return isFaded ? previewColor : connectionColor;
    }

    private void DrawLine(Vector3 from, Vector3 to, Color color)
    {
        LineRenderer lr;
        if (_activeCount < _linePool.Count)
        {
            lr = _linePool[_activeCount];
            lr.enabled = true;
        }
        else
        {
            var go = new GameObject($"Line_{_activeCount}");
            go.transform.SetParent(transform);
            lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.numCapVertices = 4;
            if (LineMaterial != null)
                lr.material = LineMaterial;
            else
                lr.material = new Material(Shader.Find("Sprites/Default"));
            _linePool.Add(lr);
        }

        lr.startWidth = LineWidth;
        lr.endWidth = LineWidth;
        lr.startColor = color;
        lr.endColor = color;
        lr.SetPosition(0, new Vector3(from.x, from.y, 0f));
        lr.SetPosition(1, new Vector3(to.x, to.y, 0f));
        lr.sortingOrder = 5;

        _activeCount++;
    }
}

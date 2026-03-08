using System.Collections.Generic;
using UnityEngine;

public class ConnectionRenderer : MonoBehaviour
{
    public static ConnectionRenderer Instance { get; private set; }

    [Header("Line Settings")]
    public float LineWidth = 0.05f;
    public Color ConnectionColor = Color.white;
    public Color PreviewColor = new Color(1f, 1f, 1f, 0.4f);
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

        foreach (var conn in _connMgr.AllConnections)
        {
            if (conn.IsBroken || conn.NodeA == null || conn.NodeB == null) continue;
            DrawLine(conn.NodeA.transform.position, conn.NodeB.transform.position, ConnectionColor);
        }

        DrawPreviews();

        for (int i = _activeCount; i < _linePool.Count; i++)
        {
            _linePool[i].enabled = false;
        }
    }

    private void DrawPreviews()
    {
        if (_connMgr.CurrentState != LevelState.Build) return;

        foreach (var node in _connMgr.AllNodes)
        {
            var drag = node.GetComponent<NodeDragHandler>();
            if (drag == null) continue;

            foreach (var (a, b) in drag.PreviewConnections)
            {
                if (a == null || b == null) continue;
                DrawLine(a.transform.position, b.transform.position, PreviewColor);
            }
        }
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

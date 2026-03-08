using System.Collections.Generic;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    public List<NodeConnection> AllConnections = new List<NodeConnection>();
    public List<Node> AllNodes = new List<Node>();

    public LevelState CurrentState = LevelState.Build;

    private IConnectionStrategy _strategy;
    public IConnectionStrategy Strategy => _strategy ??= new DefaultConnectionStrategy();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterNode(Node node)
    {
        if (!AllNodes.Contains(node))
            AllNodes.Add(node);
    }

    public void UnregisterNode(Node node)
    {
        AllNodes.Remove(node);
        RemoveConnectionsOf(node);
    }

    public NodeConnection AddConnection(Node a, Node b)
    {
        if (HasConnectionBetween(a, b)) return null;

        var conn = new NodeConnection(a, b);
        AllConnections.Add(conn);
        a.ActiveConnections.Add(conn);
        return conn;
    }

    public void RemoveConnection(NodeConnection conn)
    {
        AllConnections.Remove(conn);
        if (conn.NodeA != null)
            conn.NodeA.ActiveConnections.Remove(conn);
    }

    public void RemoveConnectionsOf(Node node)
    {
        for (int i = AllConnections.Count - 1; i >= 0; i--)
        {
            if (AllConnections[i].Involves(node))
            {
                var conn = AllConnections[i];
                if (conn.NodeA != null) conn.NodeA.ActiveConnections.Remove(conn);
                AllConnections.RemoveAt(i);
            }
        }
    }

    public bool HasConnectionBetween(Node a, Node b)
    {
        foreach (var conn in AllConnections)
        {
            if ((conn.NodeA == a && conn.NodeB == b) ||
                (conn.NodeA == b && conn.NodeB == a))
                return true;
        }
        return false;
    }

    public void InitializeAllForRun()
    {
        foreach (var conn in AllConnections)
        {
            conn.Initialize();
        }
    }

    public void ClearAll()
    {
        AllConnections.Clear();
        foreach (var node in AllNodes)
        {
            node.ActiveConnections.Clear();
        }
    }

    public void ClearAllNodes()
    {
        ClearAll();
        for (int i = AllNodes.Count - 1; i >= 0; i--)
        {
            if (AllNodes[i] != null)
                Destroy(AllNodes[i].gameObject);
        }
        AllNodes.Clear();
    }

    private void FixedUpdate()
    {
        if (CurrentState != LevelState.Run && CurrentState != LevelState.Victory)
            return;

        for (int i = AllConnections.Count - 1; i >= 0; i--)
        {
            AllConnections[i].ComputeAndApplyForces();

            if (AllConnections[i].IsBroken)
            {
                var conn = AllConnections[i];
                if (conn.NodeA != null) conn.NodeA.ActiveConnections.Remove(conn);
                AllConnections.RemoveAt(i);
            }
        }
    }
}

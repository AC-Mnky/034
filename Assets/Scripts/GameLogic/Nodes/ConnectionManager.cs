using System.Collections.Generic;
using System.Linq;
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
        RefreshChargeStates();
    }

    public void UnregisterNode(Node node)
    {
        AllNodes.Remove(node);
        RemoveConnectionsOf(node);
        RefreshChargeStates();
    }

    public NodeConnection AddConnection(Node a, Node b)
    {
        if (HasConnectionBetween(a, b)) return null;

        var conn = new NodeConnection(a, b);
        AllConnections.Add(conn);
        a.ActiveConnections.Add(conn);
        RefreshChargeStates();
        return conn;
    }

    public void RemoveConnection(NodeConnection conn)
    {
        AllConnections.Remove(conn);
        if (conn.NodeA != null)
            conn.NodeA.ActiveConnections.Remove(conn);
        RefreshChargeStates();
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
        RefreshChargeStates();
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

    public bool AreAllNodesConnected(List<(Node, Node)> extraConnections = null)
    {
        var placed = AllNodes.Where(n => !n.IsInInventory && n.gameObject.activeSelf).ToList();
        if (placed.Count == 0) return false;
        if (placed.Count == 1) return true;

        var visited = new HashSet<Node> { placed[0] };
        var queue = new Queue<Node>();
        queue.Enqueue(placed[0]);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();

            foreach (var conn in AllConnections)
            {
                Node neighbor = null;
                if (conn.NodeA == cur) neighbor = conn.NodeB;
                else if (conn.NodeB == cur) neighbor = conn.NodeA;

                if (neighbor != null && !neighbor.IsInInventory && visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }

            if (extraConnections != null)
            {
                foreach (var (a, b) in extraConnections)
                {
                    Node neighbor = null;
                    if (a == cur) neighbor = b;
                    else if (b == cur) neighbor = a;

                    if (neighbor != null && !neighbor.IsInInventory && visited.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }
        }

        return visited.Count == placed.Count;
    }

    public void InitializeAllForRun()
    {
        foreach (var conn in AllConnections)
        {
            conn.Initialize();
        }
        RefreshChargeStates();
    }

    public void ClearAll()
    {
        AllConnections.Clear();
        foreach (var node in AllNodes)
        {
            node.ActiveConnections.Clear();
        }
        RefreshChargeStates();
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

        RefreshChargeStates();
    }

    public void RefreshChargeStates()
    {
        var poweredNodes = GetPoweredNodes(null);
        for (int i = 0; i < AllNodes.Count; i++)
        {
            var node = AllNodes[i];
            if (node == null) continue;
            node.SetChargedState(!node.CanCharge && poweredNodes.Contains(node));
        }
    }

    public bool WillConnectionBeElectrified(Node a, Node b, List<(Node, Node)> extraConnections)
    {
        if (!IsNodeValidForChargeNetwork(a) || !IsNodeValidForChargeNetwork(b)) return false;
        var poweredNodes = GetPoweredNodes(extraConnections);
        return poweredNodes.Contains(a) && poweredNodes.Contains(b);
    }

    private HashSet<Node> GetPoweredNodes(List<(Node, Node)> extraConnections)
    {
        var queue = new Queue<Node>();
        var visited = new HashSet<Node>();
        for (int i = 0; i < AllNodes.Count; i++)
        {
            var node = AllNodes[i];
            if (!IsNodeValidForChargeNetwork(node)) continue;
            if (!node.CanCharge) continue;
            if (!node.IsConductive) continue;
            if (visited.Add(node))
                queue.Enqueue(node);
        }

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            for (int i = 0; i < AllConnections.Count; i++)
            {
                var conn = AllConnections[i];
                if (conn == null || conn.IsBroken) continue;
                TryVisitNeighbor(cur, conn.NodeA, conn.NodeB, visited, queue);
                TryVisitNeighbor(cur, conn.NodeB, conn.NodeA, visited, queue);
            }

            if (extraConnections == null) continue;
            for (int i = 0; i < extraConnections.Count; i++)
            {
                var (a, b) = extraConnections[i];
                TryVisitNeighbor(cur, a, b, visited, queue);
                TryVisitNeighbor(cur, b, a, visited, queue);
            }
        }

        return visited;
    }

    private static void TryVisitNeighbor(Node cur, Node from, Node to, HashSet<Node> visited, Queue<Node> queue)
    {
        if (from != cur) return;
        if (!IsNodeValidForChargeNetwork(to)) return;
        if (!to.IsConductive) return;
        if (!visited.Add(to)) return;
        queue.Enqueue(to);
    }

    private static bool IsNodeValidForChargeNetwork(Node node)
    {
        return node != null && !node.IsInInventory && node.gameObject.activeSelf;
    }
}

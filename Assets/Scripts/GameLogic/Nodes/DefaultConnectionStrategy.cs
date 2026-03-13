using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DefaultConnectionStrategy : IConnectionStrategy
{
    public void OnNodePlaced(Node node, List<Node> allNodes, ConnectionManager mgr)
    {
        if (node.IsInInventory) return;

        var candidates = GetCandidatesInternal(node, allNodes, mgr);
        foreach (var target in candidates)
        {
            mgr.AddConnection(node, target);
        }
    }

    public void OnNodeDragged(Node node, ConnectionManager mgr)
    {
        var toRemove = new List<NodeConnection>();
        foreach (var conn in mgr.AllConnections)
        {
            if (!conn.Involves(node)) continue;

            if (node.IsInInventory)
            {
                toRemove.Add(conn);
                continue;
            }

            if (conn.NodeA == node)
            {
                // This node's own active connections are cleared during drag;
                // they will be re-established on placement.
                toRemove.Add(conn);
            }
            else
            {
                // Other nodes' active connections to this node: break only if
                // distance exceeds the initiator's MaxConnectRadius.
                float dist = Vector2.Distance(node.transform.position, conn.NodeA.transform.position);
                if (dist > conn.NodeA.MaxConnectRadius)
                {
                    toRemove.Add(conn);
                }
            }
        }

        foreach (var conn in toRemove)
        {
            mgr.RemoveConnection(conn);
        }
    }

    public List<(Node, Node)> GetPreviewConnections(Node node, List<Node> allNodes)
    {
        if (node.IsInInventory) return new List<(Node, Node)>();

        var result = new List<(Node, Node)>();
        var candidates = allNodes
            .Where(n => n != node && !n.IsInInventory)
            .Select(n => new { node = n, dist = Vector2.Distance(node.transform.position, n.transform.position) })
            .Where(x => x.dist >= node.MinConnectDistance && x.dist <= node.MaxConnectRadius)
            .OrderBy(x => x.dist)
            .Take(node.MaxConnectNumber)
            .ToList();

        foreach (var c in candidates)
        {
            result.Add((node, c.node));
        }
        return result;
    }

    private List<Node> GetCandidatesInternal(Node node, List<Node> allNodes, ConnectionManager mgr)
    {
        return allNodes
            .Where(n => n != node && !n.IsInInventory)
            .Where(n => !mgr.HasConnectionBetween(node, n))
            .Where(n => !HasActiveConnectionTo(n, node))
            .Select(n => new { node = n, dist = Vector2.Distance(node.transform.position, n.transform.position) })
            .Where(x => x.dist >= node.MinConnectDistance && x.dist <= node.MaxConnectRadius)
            .OrderBy(x => x.dist)
            .Take(node.MaxConnectNumber - node.GetActiveConnectionCount())
            .Select(x => x.node)
            .ToList();
    }

    private bool HasActiveConnectionTo(Node from, Node to)
    {
        foreach (var conn in from.ActiveConnections)
        {
            if (conn.NodeB == to) return true;
        }
        return false;
    }
}

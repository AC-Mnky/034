using System.Collections.Generic;

public interface IConnectionStrategy
{
    void OnNodePlaced(Node node, List<Node> allNodes, ConnectionManager mgr);
    void OnNodeDragged(Node node, ConnectionManager mgr);
    List<(Node, Node)> GetPreviewConnections(Node node, List<Node> allNodes);
}

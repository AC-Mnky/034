using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class NodeData
{
    public GameObject prefab;
    public string nodeType;
    public bool isInInventory = true;
    public float posX;
    public float posY;
}

[System.Serializable]
public class ConnectionData
{
    public int nodeIndexA;
    public int nodeIndexB;
}

[System.Serializable]
public class BlueprintData
{
    public List<NodeData> nodes = new List<NodeData>();
    public List<ConnectionData> connections = new List<ConnectionData>();

    private static string GetFilePath(int levelIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"blueprint_{levelIndex}.json");
    }

    public static void SaveBlueprint(int levelIndex, BlueprintData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetFilePath(levelIndex), json);
    }

    public static BlueprintData LoadBlueprint(int levelIndex)
    {
        string path = GetFilePath(levelIndex);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<BlueprintData>(json);
        }
        return null;
    }

    public static void DeleteBlueprint(int levelIndex)
    {
        string path = GetFilePath(levelIndex);
        if (File.Exists(path))
            File.Delete(path);
    }

    public BlueprintData DeepCopy()
    {
        var copy = new BlueprintData();
        foreach (var nd in nodes)
        {
            copy.nodes.Add(new NodeData
            {
                prefab = nd.prefab,
                nodeType = nd.nodeType,
                isInInventory = nd.isInInventory,
                posX = nd.posX,
                posY = nd.posY
            });
        }
        foreach (var cd in connections)
        {
            copy.connections.Add(new ConnectionData
            {
                nodeIndexA = cd.nodeIndexA,
                nodeIndexB = cd.nodeIndexB
            });
        }
        return copy;
    }
}

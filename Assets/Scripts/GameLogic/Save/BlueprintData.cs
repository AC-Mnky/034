using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[System.Serializable]
public class NodeData
{
    public GameObject prefab;
    public bool isInInventory = true;
    public float posX;
    public float posY;
    public float rotZ;
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

    private static string SanitizeFileKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return "unknown";
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(key.Length);
        for (int i = 0; i < key.Length; i++)
        {
            char ch = key[i];
            bool bad = false;
            for (int j = 0; j < invalid.Length; j++)
            {
                if (ch != invalid[j]) continue;
                bad = true;
                break;
            }
            sb.Append(bad ? '_' : ch);
        }
        return sb.ToString();
    }

    private static string GetFilePath(string sceneName)
    {
        return Path.Combine(Application.persistentDataPath, $"blueprint_{SanitizeFileKey(sceneName)}.json");
    }

    public static void SaveBlueprint(string sceneName, BlueprintData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetFilePath(sceneName), json);
    }

    public static BlueprintData LoadBlueprint(string sceneName)
    {
        string path = GetFilePath(sceneName);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<BlueprintData>(json);
            if (data == null || data.nodes == null || data.connections == null)
            {
                Debug.LogWarning($"Blueprint data is invalid for scene '{sceneName}', fallback to default blueprint.");
                return null;
            }

            return data;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to load blueprint for scene '{sceneName}', fallback to default blueprint. {ex.Message}");
            return null;
        }
    }

    public static void DeleteBlueprint(string sceneName)
    {
        string path = GetFilePath(sceneName);
        if (File.Exists(path))
            File.Delete(path);
    }

    public static void DeleteAllBlueprints()
    {
        string dir = Application.persistentDataPath;
        if (!Directory.Exists(dir)) return;
        string[] files = Directory.GetFiles(dir, "blueprint_*.json");
        for (int i = 0; i < files.Length; i++)
        {
            File.Delete(files[i]);
        }
    }

    public BlueprintData DeepCopy()
    {
        var copy = new BlueprintData();
        foreach (var nd in nodes)
        {
            copy.nodes.Add(new NodeData
            {
                prefab = nd.prefab,
                isInInventory = nd.isInInventory,
                posX = nd.posX,
                posY = nd.posY,
                rotZ = nd.rotZ
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

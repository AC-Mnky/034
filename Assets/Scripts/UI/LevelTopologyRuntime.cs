using System.Collections.Generic;

public static class LevelTopologyRuntime
{
    private class TopologyNode
    {
        public bool IsAlwaysUnlocked;
        public readonly List<string> UnlockLevels = new List<string>();
    }

    private static readonly Dictionary<string, TopologyNode> Nodes = new Dictionary<string, TopologyNode>();

    public static bool HasTopology => Nodes.Count > 0;

    public static void Rebuild(IEnumerable<AutoLevelButton> buttons)
    {
        Nodes.Clear();
        if (buttons == null) return;

        foreach (var button in buttons)
        {
            if (button == null) continue;
            string sceneName = button.SceneName;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                UnityEngine.Debug.LogWarning("AutoLevelButton has empty GameObject name.");
                continue;
            }

            if (!Nodes.TryGetValue(sceneName, out var node))
            {
                node = new TopologyNode();
                Nodes.Add(sceneName, node);
            }

            node.IsAlwaysUnlocked = button.IsAlwaysUnlocked;
        }

        foreach (var button in buttons)
        {
            if (button == null || string.IsNullOrWhiteSpace(button.SceneName)) continue;
            if (!Nodes.TryGetValue(button.SceneName, out var fromNode)) continue;

            fromNode.UnlockLevels.Clear();
            if (button.UnlockLevels == null) continue;

            for (int i = 0; i < button.UnlockLevels.Count; i++)
            {
                var target = button.UnlockLevels[i];
                if (target == null)
                {
                    UnityEngine.Debug.LogWarning($"AutoLevelButton '{button.SceneName}' has null UnlockLevels entry.");
                    continue;
                }
                string targetSceneName = target.SceneName;
                if (string.IsNullOrWhiteSpace(targetSceneName))
                {
                    UnityEngine.Debug.LogWarning($"AutoLevelButton '{button.SceneName}' points to UnlockLevels with empty GameObject name.");
                    continue;
                }
                if (!Nodes.ContainsKey(targetSceneName))
                {
                    UnityEngine.Debug.LogWarning($"AutoLevelButton '{button.SceneName}' points to unknown UnlockLevels '{targetSceneName}'.");
                    continue;
                }
                if (fromNode.UnlockLevels.Contains(targetSceneName)) continue;
                fromNode.UnlockLevels.Add(targetSceneName);
            }
        }
    }

    public static bool IsLevelUnlocked(string sceneName, ISet<string> completedScenes)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return false;
        if (!Nodes.TryGetValue(sceneName, out var node)) return false;
        if (node.IsAlwaysUnlocked) return true;
        if (completedScenes == null || completedScenes.Count == 0) return false;

        foreach (var pair in Nodes)
        {
            if (!completedScenes.Contains(pair.Key)) continue;
            if (pair.Value.UnlockLevels.Contains(sceneName)) return true;
        }
        return false;
    }

    public static string GetNextScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return null;
        if (!Nodes.TryGetValue(sceneName, out var node)) return null;
        if (node.UnlockLevels.Count == 0) return null;
        return node.UnlockLevels[0];
    }

    public static IEnumerable<string> GetAllLevelNames()
    {
        return Nodes.Keys;
    }
}

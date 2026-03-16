using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SaveManager
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance ??= new SaveManager();

    private const string SaveFileName = "save.json";
    private SavePayload _payload;

    public List<string> CompletedLevelSceneList => _payload.completedLevelScenes;

    private SaveManager()
    {
        Load();
    }

    public void CompleteLevel(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (!_payload.completedLevelScenes.Contains(sceneName))
        {
            _payload.completedLevelScenes.Add(sceneName);
            Save();
        }
    }

    public bool IsLevelCompleted(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return false;
        return _payload.completedLevelScenes.Contains(sceneName);
    }

    public bool IsLevelUnlocked(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return false;
        var completed = new HashSet<string>(_payload.completedLevelScenes);
        return LevelTopologyRuntime.IsLevelUnlocked(sceneName, completed);
    }

    public bool DebugCompleteAllLevelsOnce()
    {
        if (!LevelTopologyRuntime.HasTopology) return false;
        bool changed = false;
        foreach (var sceneName in LevelTopologyRuntime.GetAllLevelNames())
        {
            if (string.IsNullOrEmpty(sceneName)) continue;
            if (_payload.completedLevelScenes.Contains(sceneName)) continue;
            _payload.completedLevelScenes.Add(sceneName);
            changed = true;
        }

        if (changed) Save();
        return changed;
    }

    public void ClearAll()
    {
        _payload = new SavePayload();
        string path = Path.Combine(Application.persistentDataPath, SaveFileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(_payload, true);
        string path = Path.Combine(Application.persistentDataPath, SaveFileName);
        File.WriteAllText(path, json);
    }

    public void Load()
    {
        string path = Path.Combine(Application.persistentDataPath, SaveFileName);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            _payload = JsonUtility.FromJson<SavePayload>(json);
            if (_payload == null) _payload = new SavePayload();
            if (_payload.completedLevelScenes == null) _payload.completedLevelScenes = new List<string>();
            if (_payload.completedLevels == null) _payload.completedLevels = new List<int>();

            bool normalized = NormalizeCompletedLevelScenes();
            if (normalized) Save();
        }
        else
        {
            _payload = new SavePayload();
        }
    }

    private bool NormalizeCompletedLevelScenes()
    {
        if (_payload == null || _payload.completedLevelScenes == null) return false;
        var normalized = _payload.completedLevelScenes
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList();

        if (normalized.Count == _payload.completedLevelScenes.Count)
        {
            for (int i = 0; i < normalized.Count; i++)
            {
                if (normalized[i] != _payload.completedLevelScenes[i]) goto changed;
            }
            return false;
        }

    changed:
        _payload.completedLevelScenes = normalized;
        return true;
    }

    [System.Serializable]
    private class SavePayload
    {
        // New format: stable scene-name identifiers.
        public List<string> completedLevelScenes = new List<string>();
        // Legacy format for migration only.
        public List<int> completedLevels = new List<int>();
    }
}

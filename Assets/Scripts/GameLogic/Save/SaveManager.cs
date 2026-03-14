using System.Collections.Generic;
using System.IO;
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

    public bool IsLevelCompleted(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= GameConfig.Instance.TotalLevelNum) return false;
        string sceneName = GameConfig.Instance.GetLevelSceneName(levelIndex);
        return _payload.completedLevelScenes.Contains(sceneName);
    }

    public int GetUnlockedCount()
    {
        int count = _payload.completedLevelScenes.Count + GameConfig.Instance.InitialUnlockedLevelNum;
        return Mathf.Min(count, GameConfig.Instance.TotalLevelNum);
    }

    public bool IsLevelUnlocked(int levelIndex)
    {
        return levelIndex < GetUnlockedCount();
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

            // Migrate old index-based save data into scene-name-based save data.
            bool migrated = false;
            for (int i = 0; i < _payload.completedLevels.Count; i++)
            {
                int levelIndex = _payload.completedLevels[i];
                if (levelIndex < 0 || levelIndex >= GameConfig.Instance.TotalLevelNum) continue;
                string sceneName = GameConfig.Instance.GetLevelSceneName(levelIndex);
                if (string.IsNullOrEmpty(sceneName)) continue;
                if (_payload.completedLevelScenes.Contains(sceneName)) continue;
                _payload.completedLevelScenes.Add(sceneName);
                migrated = true;
            }
            if (migrated) Save();
        }
        else
        {
            _payload = new SavePayload();
        }
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

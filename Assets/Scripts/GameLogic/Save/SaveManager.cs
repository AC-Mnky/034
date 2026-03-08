using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance ??= new SaveManager();

    private const string SaveFileName = "save.json";
    private SavePayload _payload;

    public List<int> CompletedLevelList => _payload.completedLevels;

    private SaveManager()
    {
        Load();
    }

    public void CompleteLevel(int levelIndex)
    {
        if (!_payload.completedLevels.Contains(levelIndex))
        {
            _payload.completedLevels.Add(levelIndex);
            Save();
        }
    }

    public bool IsLevelCompleted(int levelIndex)
    {
        return _payload.completedLevels.Contains(levelIndex);
    }

    public int GetUnlockedCount()
    {
        int count = _payload.completedLevels.Count + GameConstants.InitialUnlockedLevelNum;
        return Mathf.Min(count, GameConstants.TotalLevelNum);
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
        }
        else
        {
            _payload = new SavePayload();
        }
    }

    [System.Serializable]
    private class SavePayload
    {
        public List<int> completedLevels = new List<int>();
    }
}

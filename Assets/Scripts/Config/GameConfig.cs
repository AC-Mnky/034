using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Config")]
public class GameConfig : ScriptableObject
{
    private static GameConfig _instance;

    public static GameConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameConfig>("GameConfig");
                if (_instance == null)
                    Debug.LogError("GameConfig asset not found in Resources folder. " +
                                   "Right-click in Assets/Resources and select Create > Game > Game Config.");
            }
            return _instance;
        }
    }

    [Header("Scene Names")]
    public string LevelSelectSceneName = "LevelSelect";

    [Header("Camera")]
    public float MaxHorizontalSpan = 0.4f;
    public float MaxVerticalSpan = 0.4f;
}

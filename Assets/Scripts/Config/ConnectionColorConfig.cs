using UnityEngine;

[CreateAssetMenu(fileName = "ConnectionColorConfig", menuName = "Game/Connection Color Config")]
public class ConnectionColorConfig : ScriptableObject
{
    private static ConnectionColorConfig _instance;

    public static ConnectionColorConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ConnectionColorConfig>("ConnectionColorConfig");
                if (_instance == null)
                    Debug.LogError("ConnectionColorConfig asset not found in Resources folder. " +
                                   "Right-click in Assets/Resources and select Create > Game > Connection Color Config.");
            }
            return _instance;
        }
    }

    [Header("Normal Connection")]
    public Color ConnectionColor = Color.white;
    public float ConnectionWidth = 0.05f;

    [Header("Charged Connection")]
    public Color ChargedConnectionColor = Color.green;
    public float ChargedConnectionWidth = 0.05f;

    [Header("Balloon Connection")]
    public Color BalloonConnectionColor = new Color(1f, 0.8f, 0.2f, 1f);
    public float BalloonConnectionWidth = 0.05f;

    [Header("Level Select Connections")]
    public Color LevelConnectionLockedColor = new Color(1f, 1f, 1f, 0.35f);
    public float LevelConnectionLockedWidth = 0.06f;
    public Color LevelConnectionUnlockedColor = Color.white;
    public float LevelConnectionUnlockedWidth = 0.1f;
    public float LevelConnectionZ = 5f;

    [Header("Preview")]
    public Color PreviewColor = new Color(1f, 1f, 1f, 0.4f);
}

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

    [Header("Connection Colors")]
    public Color ConnectionColor = Color.white;
    public Color ChargedConnectionColor = Color.green;
    public Color BalloonConnectionColor = new Color(1f, 0.8f, 0.2f, 1f);
    public Color PreviewColor = new Color(1f, 1f, 1f, 0.4f);
}

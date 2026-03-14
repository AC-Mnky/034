using UnityEngine;

[CreateAssetMenu(fileName = "BuildAreaConfig", menuName = "Game/Build Area Config")]
public class BuildAreaConfig : ScriptableObject
{
    private static BuildAreaConfig _instance;

    public static BuildAreaConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<BuildAreaConfig>("BuildAreaConfig");
                if (_instance == null)
                    Debug.LogError("BuildAreaConfig asset not found in Resources folder. " +
                                   "Right-click in Assets/Resources and select Create > Game > Build Area Config.");
            }
            return _instance;
        }
    }

    [Header("Build Area Visual")]
    public Color BuildAreaFillColor = new Color(0f, 0f, 0f, 0.15f);
    public Color BuildAreaBorderColor = Color.white;
    [Min(0.005f)] public float BuildAreaBorderWidth = 0.06f;

    [Header("Inventory Area Visual")]
    public Color InventoryAreaFillColor = new Color(0f, 0f, 0f, 0.15f);
    public Color InventoryAreaBorderColor = Color.white;
    [Min(0.005f)] public float InventoryAreaBorderWidth = 0.06f;
}

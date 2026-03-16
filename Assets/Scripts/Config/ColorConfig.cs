using UnityEngine;

[CreateAssetMenu(fileName = "ColorConfig", menuName = "Game/Color Config")]
public class ColorConfig : ScriptableObject
{
    private static ColorConfig _instance;

    public static ColorConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ColorConfig>("ColorConfig");
                if (_instance == null)
                    Debug.LogError("ColorConfig asset not found in Resources folder. " +
                                   "Right-click in Assets/Resources and select Create > Game > Color Config.");
            }
            return _instance;
        }
    }

    [Header("Level - Area Backgrounds")]
    public Color InventoryBackgroundColor = new Color(0f, 0f, 0f, 0.3f);
    public Color BuildAreaBackgroundColor = new Color(0f, 0f, 0f, 0.15f);

    [Header("Level - Buttons")]
    public Color ExitButtonColor = Color.white;
    public Color StartButtonColor = Color.green;
    public Color StopButtonColor = Color.red;
    public Color NextButtonColor = Color.blue;
    public Color DisabledButtonColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Level Select - Level Buttons")]
    public Color CompletedLevelColor = Color.blue;
    public Color UnlockedLevelColor = Color.green;
    public Color LockedLevelColor = Color.gray;

    [Header("Level Select - Other")]
    public Color QuitButtonColor = Color.red;

    [Header("Node - Materials")]
    public Material UnchargedMaterial;
    public Material ChargedMaterial;

    [Header("Goal - Materials")]
    public Material GoalDefaultMaterial;
    public Material GoalReachedMaterial;
}

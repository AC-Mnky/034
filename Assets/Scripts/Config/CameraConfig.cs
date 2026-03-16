using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfig", menuName = "Game/Camera Config")]
public class CameraConfig : ScriptableObject
{
    private static CameraConfig _instance;

    public static CameraConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<CameraConfig>("CameraConfig");
                if (_instance == null)
                    Debug.LogError("CameraConfig asset not found in Resources folder. " +
                                   "Right-click in Assets/Resources and select Create > Game > Camera Config.");
            }
            return _instance;
        }
    }

    [Header("Level Intro Sequence")]
    [Min(0f)] public float IntroHoldSeconds = 1f;
    [Min(0f)] public float IntroMoveSeconds = 3f;
    [Min(1f)] public float IntroBoundsScaleMultiplier = 1.2f;
    [Min(0.01f)] public float HoverToIntroSeconds = 1.0f;
    [Min(0.01f)] public float HoverReturnSeconds = 0.5f;
    [Min(0.01f)] public float RuntimeHoverToIntroSeconds = 0.5f;

    [Header("Part Appearance")]
    [Min(0.01f)] public float PartAppearSeconds = 0.5f;
    [Min(0f)] public float PartAppearStartScale = 0.65f;
    [Range(0f, 1f)] public float PartAppearStartAlpha = 0f;
}

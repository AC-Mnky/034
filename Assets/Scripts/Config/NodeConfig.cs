using UnityEngine;

[CreateAssetMenu(fileName = "NodeConfig", menuName = "Game/Node Config")]
public class NodeConfig : ScriptableObject
{
    private static NodeConfig _instance;

    public static NodeConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<NodeConfig>("NodeConfig");
                if (_instance == null)
                    Debug.LogError("NodeConfig asset not found in Resources folder. " +
                                   "Right-click in Assets/Resources and select Create > Game > Node Config.");
            }
            return _instance;
        }
    }

    [Header("Connection Settings")]
    public float MaxConnectRadius = 3f;
    public float MinConnectRadius = 2f;
    public int MaxConnectNumber = 10;

    [Header("Physics")]
    public float SpringK = 125f;
    public float SpringDamping = 5f;
    public float SpringBreakLength = 1f;
    public float AngularSpringK = 200f;
    public float AngularSpringDamping = 1f;
}

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class AutoGround : MonoBehaviour
{
    [Header("Endpoints")]
    public Transform PointA;
    public Transform PointB;

    [Header("Ground Hierarchy")]
    public Transform GroundRoot;
    public GameObject GroundPrefab;
    public Transform GroundInstance;

    [Header("Length Mapping")]
    [Min(0.0001f)] public float LengthPerScaleX = 1f;

    private void Reset()
    {
        EnsureStructure();
        EnsureGroundInstance();
        UpdateGroundTransform();
    }

    private void Awake()
    {
        EnsureStructure();
        EnsureGroundInstance();
        UpdateGroundTransform();
    }

    private void OnValidate()
    {
        EnsureStructure();
        EnsureGroundInstance();
        UpdateGroundTransform();
    }

    private void LateUpdate()
    {
        UpdateGroundTransform();
    }

    private void EnsureStructure()
    {
        if (GroundRoot == null)
        {
            Transform existing = transform.Find("GroundRoot");
            if (existing != null)
            {
                GroundRoot = existing;
            }
            else
            {
                var rootGo = new GameObject("GroundRoot");
                GroundRoot = rootGo.transform;
                GroundRoot.SetParent(transform, false);
            }
        }

        if (PointA == null)
        {
            Transform existing = transform.Find("PointA");
            if (existing != null)
            {
                PointA = existing;
            }
            else
            {
                var pointGo = new GameObject("PointA");
                PointA = pointGo.transform;
                PointA.SetParent(transform, false);
                PointA.localPosition = new Vector3(-1f, 0f, 0f);
            }
        }

        if (PointB == null)
        {
            Transform existing = transform.Find("PointB");
            if (existing != null)
            {
                PointB = existing;
            }
            else
            {
                var pointGo = new GameObject("PointB");
                PointB = pointGo.transform;
                PointB.SetParent(transform, false);
                PointB.localPosition = new Vector3(1f, 0f, 0f);
            }
        }
    }

    private void EnsureGroundInstance()
    {
        if (GroundRoot == null) return;

        if (GroundInstance == null && GroundRoot.childCount > 0)
            GroundInstance = GroundRoot.GetChild(0);

        if (GroundInstance != null || GroundPrefab == null) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var instance = PrefabUtility.InstantiatePrefab(GroundPrefab, GroundRoot) as GameObject;
            if (instance != null)
            {
                GroundInstance = instance.transform;
                return;
            }
        }
#endif
        var runtimeInstance = Instantiate(GroundPrefab, GroundRoot);
        GroundInstance = runtimeInstance.transform;
    }

    private void UpdateGroundTransform()
    {
        if (PointA == null || PointB == null || GroundInstance == null) return;

        Vector2 a = PointA.position;
        Vector2 b = PointB.position;
        Vector2 delta = b - a;
        float length = delta.magnitude;
        if (length <= 1e-6f) return;

        Vector3 center = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, GroundInstance.position.z);
        float angleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        GroundInstance.position = center;
        GroundInstance.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        Vector3 scale = GroundInstance.localScale;
        scale.x = length / Mathf.Max(LengthPerScaleX, 0.0001f);
        GroundInstance.localScale = scale;
    }
}

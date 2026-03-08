using UnityEngine;

public class RuntimeCameraController : MonoBehaviour
{
    [Header("Smoothing")]
    public float SmoothTime = 0.3f;

    [Header("Zoom")]
    public float DefaultOrthoSize = 5f;
    public float MinOrthoSize = 3f;

    private Camera _cam;
    private ConnectionManager _connMgr;

    private Vector3 _velocityPos;
    private float _velocityZoom;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
        DefaultOrthoSize = _cam.orthographicSize;
    }

    private void OnEnable()
    {
        _connMgr = ConnectionManager.Instance;
        _velocityPos = Vector3.zero;
        _velocityZoom = 0f;
    }

    private void LateUpdate()
    {
        if (_connMgr == null || _connMgr.AllNodes.Count == 0) return;

        Vector2 centerOfMass;
        float targetSize;
        ComputeTargets(out centerOfMass, out targetSize);

        Vector3 targetPos = new Vector3(centerOfMass.x, centerOfMass.y, transform.position.z);

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocityPos, SmoothTime);
        _cam.orthographicSize = Mathf.SmoothDamp(_cam.orthographicSize, targetSize, ref _velocityZoom, SmoothTime);
    }

    private void ComputeTargets(out Vector2 centerOfMass, out float targetSize)
    {
        float totalMass = 0f;
        Vector2 weightedSum = Vector2.zero;

        int activeCount = 0;
        foreach (var node in _connMgr.AllNodes)
        {
            if (node == null || node.IsInInventory) continue;
            float m = node.Mass;
            weightedSum += (Vector2)node.transform.position * m;
            totalMass += m;
            activeCount++;
        }

        if (activeCount == 0 || totalMass < 1e-6f)
        {
            centerOfMass = transform.position;
            targetSize = DefaultOrthoSize;
            return;
        }

        centerOfMass = weightedSum / totalMass;

        float aspect = _cam.aspect;
        float requiredHalf = DefaultOrthoSize;

        foreach (var node in _connMgr.AllNodes)
        {
            if (node == null || node.IsInInventory) continue;
            Vector2 offset = (Vector2)node.transform.position - centerOfMass;

            float neededHoriz = Mathf.Abs(offset.x) / (GameConstants.MaxHorizontalSpan * aspect);
            float neededVert = Mathf.Abs(offset.y) / GameConstants.MaxVerticalSpan;

            float needed = Mathf.Max(neededHoriz, neededVert);
            if (needed > requiredHalf)
                requiredHalf = needed;
        }

        targetSize = Mathf.Max(requiredHalf, MinOrthoSize);
    }
}

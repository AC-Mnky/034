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

    private bool _returning;
    private Vector3 _returnPos;

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
        _returning = false;
    }

    public void ReturnToDefault(Vector3 targetPosition)
    {
        _returning = true;
        _returnPos = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
    }

    public void SnapToDefault(Vector3 targetPosition)
    {
        transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        _cam.orthographicSize = DefaultOrthoSize;
        _returning = false;
        _velocityPos = Vector3.zero;
        _velocityZoom = 0f;
        enabled = false;
    }

    public void StartFollowing()
    {
        _returning = false;
        _velocityPos = Vector3.zero;
        _velocityZoom = 0f;
        enabled = true;
    }

    private void LateUpdate()
    {
        if (_returning)
        {
            transform.position = Vector3.SmoothDamp(transform.position, _returnPos, ref _velocityPos, SmoothTime);
            _cam.orthographicSize = Mathf.SmoothDamp(_cam.orthographicSize, DefaultOrthoSize, ref _velocityZoom, SmoothTime);

            float posDist = Vector3.Distance(transform.position, _returnPos);
            float zoomDist = Mathf.Abs(_cam.orthographicSize - DefaultOrthoSize);
            if (posDist < 0.01f && zoomDist < 0.01f)
            {
                transform.position = _returnPos;
                _cam.orthographicSize = DefaultOrthoSize;
                _returning = false;
                enabled = false;
            }
            return;
        }

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

        Vector2 currentCamPos = transform.position;
        float aspect = _cam.aspect;
        float requiredHalf = DefaultOrthoSize;

        foreach (var node in _connMgr.AllNodes)
        {
            if (node == null || node.IsInInventory) continue;

            Vector2 fromTarget = (Vector2)node.transform.position - centerOfMass;
            Vector2 fromCamera = (Vector2)node.transform.position - currentCamPos;

            float neededFromTarget = Mathf.Max(
                Mathf.Abs(fromTarget.x) / (GameConstants.MaxHorizontalSpan * aspect),
                Mathf.Abs(fromTarget.y) / GameConstants.MaxVerticalSpan);

            float neededFromCamera = Mathf.Max(
                Mathf.Abs(fromCamera.x) / (GameConstants.MaxHorizontalSpan * aspect),
                Mathf.Abs(fromCamera.y) / GameConstants.MaxVerticalSpan);

            float needed = Mathf.Max(neededFromTarget, neededFromCamera);
            if (needed > requiredHalf)
                requiredHalf = needed;
        }

        targetSize = Mathf.Max(requiredHalf, MinOrthoSize);
    }
}

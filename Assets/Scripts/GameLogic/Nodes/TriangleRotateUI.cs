using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Node))]
[RequireComponent(typeof(Collider2D))]
public class TriangleRotateUI : MonoBehaviour
{
    public static bool IsAnyHandleDragging { get; private set; }

    private static TriangleRotateUI _active;

    [Header("Rotate UI")]
    [Min(0.1f)] public float HandleDistance = 1.2f;
    [Min(0.02f)] public float HandleRadius = 0.15f;
    [Min(0.01f)] public float LineWidth = 0.05f;
    public Color LineColor = Color.white;
    public Color HandleColor = Color.white;

    private Node _node;
    private NodeDragHandler _dragHandler;
    private Camera _mainCamera;
    private Collider2D _nodeCollider;
    private LineRenderer _line;
    private GameObject _handle;
    private SpriteRenderer _handleRenderer;
    private CircleCollider2D _handleCollider;
    private bool _isVisible;
    private bool _isHandleDragging;

    private void Awake()
    {
        _node = GetComponent<Node>();
        _dragHandler = GetComponent<NodeDragHandler>();
        _nodeCollider = GetComponent<Collider2D>();
        _mainCamera = Camera.main;
        EnsureVisuals();
        Hide();
    }

    private void OnDisable()
    {
        if (_active == this) _active = null;
        IsAnyHandleDragging = false;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (_mainCamera == null) _mainCamera = Camera.main;
        if (ConnectionManager.Instance == null || ConnectionManager.Instance.CurrentState != LevelState.Build)
        {
            Hide();
            return;
        }

        if (_isHandleDragging)
        {
            UpdateRotationFromMouse();
            if (Input.GetMouseButtonUp(0)) EndHandleDrag();
        }
        else if (_isVisible && Input.GetMouseButtonDown(0) && !IsNodeDragging() && IsPointerOverHandle())
        {
            BeginHandleDrag();
        }

        if (_isVisible)
        {
            UpdateVisualPositions();

            if (!_isHandleDragging && Input.GetMouseButtonDown(0) && !IsPointerOverSelfOrHandle())
            {
                Hide();
            }
        }
    }

    public static void OnNodePointerDown(Node node)
    {
        if (node == null) return;
        var ui = node.GetComponent<TriangleRotateUI>();
        if (ui != null) ui.Show();
        else if (_active != null) _active.Hide();
    }

    public void Show()
    {
        if (_active != null && _active != this)
            _active.Hide();

        _active = this;
        _isVisible = true;
        EnsureVisuals();
        _line.enabled = true;
        _handle.SetActive(true);
        UpdateVisualStyle();
        UpdateVisualPositions();
    }

    public void Hide()
    {
        _isVisible = false;
        _isHandleDragging = false;
        if (_line != null) _line.enabled = false;
        if (_handle != null) _handle.SetActive(false);
        if (_active == this) _active = null;
        IsAnyHandleDragging = false;
    }

    public void BeginHandleDrag()
    {
        Show();
        _isHandleDragging = true;
        IsAnyHandleDragging = true;
        UpdateRotationFromMouse();
    }

    public void UpdateRotationFromMouse()
    {
        if (!_isVisible || _mainCamera == null) return;

        Vector3 screenPos = Input.mousePosition;
        screenPos.z = -_mainCamera.transform.position.z;
        Vector2 mouseWorld = _mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 dir = mouseWorld - (Vector2)transform.position;
        if (dir.sqrMagnitude < 1e-6f) return;

        float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
        UpdateVisualPositions();
    }

    public void EndHandleDrag()
    {
        _isHandleDragging = false;
        IsAnyHandleDragging = false;
    }

    private void EnsureVisuals()
    {
        if (_line == null)
        {
            _line = GetComponent<LineRenderer>();
            if (_line == null) _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.sortingOrder = 200;
        }

        if (_handle == null)
        {
            _handle = new GameObject("RotateHandle");
            _handle.transform.SetParent(transform, false);
            _handleRenderer = _handle.AddComponent<SpriteRenderer>();
            _handleRenderer.sortingOrder = 210;
            _handleRenderer.sprite = CreateCircleSprite();
            _handleCollider = _handle.AddComponent<CircleCollider2D>();
            _handleCollider.isTrigger = false;
            _handle.AddComponent<TriangleRotateHandle>().Owner = this;
        }
    }

    private void UpdateVisualStyle()
    {
        _line.startWidth = LineWidth;
        _line.endWidth = LineWidth;
        _line.startColor = LineColor;
        _line.endColor = LineColor;
        _handleRenderer.color = HandleColor;
        _handle.transform.localScale = Vector3.one * (HandleRadius * 2f);
        _handleCollider.radius = 0.5f;
    }

    private void UpdateVisualPositions()
    {
        Vector3 center = transform.position;
        Vector3 handlePos = center + transform.up * HandleDistance;
        _line.SetPosition(0, center);
        _line.SetPosition(1, handlePos);
        _handle.transform.position = handlePos;
    }

    private bool IsPointerOverSelfOrHandle()
    {
        if (_mainCamera == null) return false;
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = -_mainCamera.transform.position.z;
        Vector2 mouseWorld = _mainCamera.ScreenToWorldPoint(screenPos);

        if (_nodeCollider != null && _nodeCollider.OverlapPoint(mouseWorld))
            return true;
        if (_handleCollider != null && _handleCollider.OverlapPoint(mouseWorld))
            return true;
        return false;
    }

    private bool IsPointerOverHandle()
    {
        if (_mainCamera == null || _handleCollider == null) return false;
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = -_mainCamera.transform.position.z;
        Vector2 mouseWorld = _mainCamera.ScreenToWorldPoint(screenPos);
        return _handleCollider.OverlapPoint(mouseWorld);
    }

    private bool IsNodeDragging()
    {
        return _dragHandler != null && _dragHandler.IsDragging;
    }

    private static Sprite CreateCircleSprite()
    {
        const int diameter = 32;
        var tex = new Texture2D(diameter, diameter);
        var pixels = new Color[diameter * diameter];
        float center = diameter * 0.5f;
        float rSq = center * center;
        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                pixels[y * diameter + x] = (dx * dx + dy * dy <= rSq) ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, diameter, diameter), Vector2.one * 0.5f, diameter);
    }
}

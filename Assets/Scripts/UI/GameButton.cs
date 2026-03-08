using UnityEngine;

public enum ButtonShape
{
    Circle,
    Square,
    TriangleLeft,
    TriangleRight
}

public class GameButton : MonoBehaviour
{
    public string ButtonName;
    public MonoBehaviour ButtonReceiver;
    public ButtonShape Shape;
    public Color ButtonColor = Color.white;
    public Vector2 ButtonSize = Vector2.one;

    private IButtonReceiver _receiver;
    private Camera _mainCamera;
    private Collider2D _collider;

    private void Start()
    {
        _mainCamera = Camera.main;
        _collider = GetComponent<Collider2D>();
        if (ButtonReceiver != null)
            _receiver = ButtonReceiver as IButtonReceiver;

        EnsureSprite();
    }

    private void EnsureSprite()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (sr.sprite != null) return;

        if (_collider is BoxCollider2D box)
        {
            sr.sprite = GenerateRectSprite(box.size);
        }
        else if (_collider is CircleCollider2D circle)
        {
            sr.sprite = GenerateCircleSprite(circle.radius);
        }
        else if (_collider is PolygonCollider2D poly)
        {
            sr.sprite = GeneratePolygonSprite(poly);
        }

        sr.color = ButtonColor;
    }

    private static Sprite GenerateRectSprite(Vector2 size)
    {
        int w = Mathf.Max(2, Mathf.RoundToInt(size.x * 32f));
        int h = Mathf.Max(2, Mathf.RoundToInt(size.y * 32f));
        var tex = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 32f);
    }

    private static Sprite GenerateCircleSprite(float radius)
    {
        int diameter = Mathf.Max(4, Mathf.RoundToInt(radius * 2f * 32f));
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
        return Sprite.Create(tex, new Rect(0, 0, diameter, diameter), Vector2.one * 0.5f, 32f);
    }

    private static Sprite GeneratePolygonSprite(PolygonCollider2D poly)
    {
        var points = poly.points;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var p in points)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        float ppu = 32f;
        int w = Mathf.Max(2, Mathf.RoundToInt((maxX - minX) * ppu));
        int h = Mathf.Max(2, Mathf.RoundToInt((maxY - minY) * ppu));
        var tex = new Texture2D(w, h);
        var pixels = new Color[w * h];

        for (int py = 0; py < h; py++)
        {
            for (int px = 0; px < w; px++)
            {
                Vector2 worldPt = new Vector2(
                    minX + (px + 0.5f) / ppu,
                    minY + (py + 0.5f) / ppu);
                pixels[py * w + px] = PointInPolygon(worldPt, points) ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();

        Vector2 pivot = new Vector2(
            (-minX) / (maxX - minX),
            (-minY) / (maxY - minY));
        return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu);
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; j = i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y)
                    / (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    public void SetReceiver(MonoBehaviour receiver)
    {
        ButtonReceiver = receiver;
        _receiver = receiver as IButtonReceiver;
    }

    private void Update()
    {
        if (_receiver == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 screenPos = Input.mousePosition;
        screenPos.z = -_mainCamera.transform.position.z;
        Vector2 mouseWorld = _mainCamera.ScreenToWorldPoint(screenPos);
        if (_collider != null && _collider.OverlapPoint(mouseWorld))
        {
            Debug.Log($"Button {ButtonName} clicked");
            _receiver.OnButtonDown(ButtonName);
        }
    }
}

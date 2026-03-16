using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum ButtonShape
{
    Circle,
    Square,
    TriangleLeft,
    TriangleRight
}

[RequireComponent(typeof(Image))]
public class GameButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public string ButtonName;
    public MonoBehaviour ButtonReceiver;
    public ButtonShape Shape;
    public Color ButtonColor = Color.white;
    public Vector2 ButtonSize = Vector2.one;

    private IButtonReceiver _receiver;
    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        if (ButtonReceiver != null)
            _receiver = ButtonReceiver as IButtonReceiver;
    }

    private void Start()
    {
        ApplyVisual();
    }

    public void SetReceiver(MonoBehaviour receiver)
    {
        ButtonReceiver = receiver;
        _receiver = receiver as IButtonReceiver;
    }

    public void ApplyVisual()
    {
        if (_image == null) _image = GetComponent<Image>();
        _image.sprite = GenerateShapeSprite(Shape, ButtonSize);
        _image.color = ButtonColor;
        _image.type = Image.Type.Simple;
        _image.preserveAspect = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_receiver == null) return;
        _receiver.OnButtonDown(ButtonName);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_receiver is IButtonHoverReceiver hoverReceiver)
            hoverReceiver.OnButtonHoverEnter(ButtonName);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_receiver is IButtonHoverReceiver hoverReceiver)
            hoverReceiver.OnButtonHoverExit(ButtonName);
    }

    private static Sprite GenerateShapeSprite(ButtonShape shape, Vector2 size)
    {
        switch (shape)
        {
            case ButtonShape.Circle:
                return GenerateCircleSprite(size);
            case ButtonShape.Square:
                return GenerateRectSprite(size);
            case ButtonShape.TriangleLeft:
                return GenerateTriangleSprite(size, true);
            case ButtonShape.TriangleRight:
                return GenerateTriangleSprite(size, false);
            default:
                return GenerateRectSprite(size);
        }
    }

    private static Sprite GenerateRectSprite(Vector2 size)
    {
        int w = Mathf.Max(2, Mathf.RoundToInt(size.x * 96f));
        int h = Mathf.Max(2, Mathf.RoundToInt(size.y * 96f));
        var tex = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 96f);
    }

    private static Sprite GenerateCircleSprite(Vector2 size)
    {
        int diameter = Mathf.Max(4, Mathf.RoundToInt(Mathf.Min(size.x, size.y) * 96f));
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
        return Sprite.Create(tex, new Rect(0, 0, diameter, diameter), Vector2.one * 0.5f, 96f);
    }

    private static Sprite GenerateTriangleSprite(Vector2 size, bool pointLeft)
    {
        int w = Mathf.Max(4, Mathf.RoundToInt(size.x * 96f));
        int h = Mathf.Max(4, Mathf.RoundToInt(size.y * 96f));
        var tex = new Texture2D(w, h);
        var pixels = new Color[w * h];
        float cx = w * 0.5f;
        float cy = h * 0.5f;

        Vector2 a, b, c;
        if (pointLeft)
        {
            a = new Vector2(0, cy);
            b = new Vector2(w, 0);
            c = new Vector2(w, h);
        }
        else
        {
            a = new Vector2(w, cy);
            b = new Vector2(0, 0);
            c = new Vector2(0, h);
        }

        for (int py = 0; py < h; py++)
        {
            for (int px = 0; px < w; px++)
            {
                Vector2 pt = new Vector2(px + 0.5f, py + 0.5f);
                pixels[py * w + px] = PointInTriangle(pt, a, b, c) ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 96f);
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
}

public interface IButtonHoverReceiver
{
    void OnButtonHoverEnter(string buttonName);
    void OnButtonHoverExit(string buttonName);
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelAreaVisualController : MonoBehaviour
{
    private GameObject _inventoryBg;
    private GameObject _buildAreaBg;
    private readonly List<AreaFadeState> _activeFadeStates = new List<AreaFadeState>();

    public Transform InventoryVisualRoot => _inventoryBg != null ? _inventoryBg.transform : null;
    public Transform BuildAreaVisualRoot => _buildAreaBg != null ? _buildAreaBg.transform : null;

    public void BuildVisuals(Vector2[] buildPoly, Rect inventoryArea)
    {
        ClearVisuals();
        CreateInventoryBackground(inventoryArea);
        CreateBuildAreaBackground(buildPoly);
    }

    public void SetVisible(bool visible)
    {
        if (_inventoryBg != null) _inventoryBg.SetActive(visible);
        if (_buildAreaBg != null) _buildAreaBg.SetActive(visible);
    }

    public IEnumerator FadeIn(float duration)
    {
        CancelFadeAndRestoreColors();
        _activeFadeStates.Clear();
        CollectFadeStatesFromRoot(_inventoryBg, _activeFadeStates);
        CollectFadeStatesFromRoot(_buildAreaBg, _activeFadeStates);
        if (_activeFadeStates.Count == 0) yield break;

        duration = Mathf.Max(0.01f, duration);
        SetAlpha(_activeFadeStates, 0f);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p * (3f - 2f * p);
            SetAlpha(_activeFadeStates, eased);
            yield return null;
        }

        RestoreFadeStates(_activeFadeStates);
        _activeFadeStates.Clear();
    }

    public void CancelFadeAndRestoreColors()
    {
        RestoreFadeStates(_activeFadeStates);
        _activeFadeStates.Clear();
    }

    public static void DrawAreaGizmos(Vector2[] buildPoly, Rect inventoryArea)
    {
        var cfg = BuildAreaConfig.Instance;
        if (buildPoly != null && buildPoly.Length >= 3)
        {
            Gizmos.color = cfg != null ? cfg.BuildAreaBorderColor : Color.white;
            int j = buildPoly.Length - 1;
            for (int i = 0; i < buildPoly.Length; j = i++)
            {
                Gizmos.DrawLine(buildPoly[j], buildPoly[i]);
            }
        }

        Gizmos.color = cfg != null ? cfg.InventoryAreaBorderColor : Color.white;
        Vector3 invCenter = new Vector3(
            inventoryArea.x + inventoryArea.width * 0.5f,
            inventoryArea.y + inventoryArea.height * 0.5f, 0f);
        Vector3 invSize = new Vector3(inventoryArea.width, inventoryArea.height, 0f);
        Gizmos.DrawWireCube(invCenter, invSize);
    }

    private void ClearVisuals()
    {
        if (_inventoryBg != null) Destroy(_inventoryBg);
        if (_buildAreaBg != null) Destroy(_buildAreaBg);
        _inventoryBg = null;
        _buildAreaBg = null;
    }

    private void CreateInventoryBackground(Rect area)
    {
        var cfg = BuildAreaConfig.Instance;
        Color fillColor = cfg != null ? cfg.InventoryAreaFillColor : new Color(0f, 0f, 0f, 0.15f);
        Color borderColor = cfg != null ? cfg.InventoryAreaBorderColor : Color.white;
        float borderWidth = cfg != null ? cfg.InventoryAreaBorderWidth : 0.06f;
        _inventoryBg = CreateAreaBackground("InventoryBackground", area, fillColor, borderColor, borderWidth);
    }

    private void CreateBuildAreaBackground(Vector2[] poly)
    {
        if (poly == null || poly.Length < 3) return;
        var cfg = BuildAreaConfig.Instance;

        _buildAreaBg = new GameObject("BuildAreaBackground");
        _buildAreaBg.transform.position = new Vector3(0f, 0f, 1f);

        var mf = _buildAreaBg.AddComponent<MeshFilter>();
        var mr = _buildAreaBg.AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default"));
        mr.material.color = cfg != null ? cfg.BuildAreaFillColor : new Color(0f, 0f, 0f, 0.15f);
        mr.sortingOrder = -100;
        mf.mesh = CreatePolygonMesh(poly);

        Color borderColor = cfg != null ? cfg.BuildAreaBorderColor : Color.white;
        float width = cfg != null ? cfg.BuildAreaBorderWidth : 0.06f;
        var points = new Vector3[poly.Length];
        for (int i = 0; i < poly.Length; i++)
            points[i] = new Vector3(poly[i].x, poly[i].y, 0f);
        CreateLoopBorderLine(_buildAreaBg.transform, "BuildAreaBorder", points, borderColor, width);
    }

    private static Mesh CreatePolygonMesh(Vector2[] poly)
    {
        var mesh = new Mesh();
        var verts = new Vector3[poly.Length];
        for (int i = 0; i < poly.Length; i++) verts[i] = new Vector3(poly[i].x, poly[i].y, 0f);
        var tris = new int[(poly.Length - 2) * 3];
        for (int i = 0; i < poly.Length - 2; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    private static GameObject CreateAreaBackground(string name, Rect area, Color fillColor, Color borderColor, float borderWidth)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(area.x + area.width * 0.5f, area.y + area.height * 0.5f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = fillColor;
        sr.sortingOrder = -100;

        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        go.transform.localScale = new Vector3(area.width, area.height, 1f);

        var points = new[]
        {
            new Vector3(area.xMin, area.yMin, 0f),
            new Vector3(area.xMax, area.yMin, 0f),
            new Vector3(area.xMax, area.yMax, 0f),
            new Vector3(area.xMin, area.yMax, 0f)
        };
        CreateLoopBorderLine(go.transform, $"{name}_Border", points, borderColor, borderWidth);
        return go;
    }

    private static void CreateLoopBorderLine(Transform parent, string name, Vector3[] points, Color borderColor, float borderWidth)
    {
        var borderGo = new GameObject(name);
        borderGo.transform.SetParent(parent, false);
        var lr = borderGo.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = points.Length;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = borderColor;
        lr.endColor = borderColor;
        lr.startWidth = borderWidth;
        lr.endWidth = borderWidth;
        lr.sortingOrder = -99;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        for (int i = 0; i < points.Length; i++) lr.SetPosition(i, points[i]);
    }

    private class AreaFadeState
    {
        public readonly SpriteRenderer SpriteRenderer;
        public readonly MeshRenderer MeshRenderer;
        public readonly LineRenderer LineRenderer;
        public readonly Color ColorA;
        public readonly Color ColorB;

        public AreaFadeState(SpriteRenderer spriteRenderer, Color color)
        {
            SpriteRenderer = spriteRenderer;
            MeshRenderer = null;
            LineRenderer = null;
            ColorA = color;
            ColorB = color;
        }

        public AreaFadeState(MeshRenderer meshRenderer, Color color)
        {
            SpriteRenderer = null;
            MeshRenderer = meshRenderer;
            LineRenderer = null;
            ColorA = color;
            ColorB = color;
        }

        public AreaFadeState(LineRenderer lineRenderer, Color startColor, Color endColor)
        {
            SpriteRenderer = null;
            MeshRenderer = null;
            LineRenderer = lineRenderer;
            ColorA = startColor;
            ColorB = endColor;
        }
    }

    private static void CollectFadeStatesFromRoot(GameObject root, List<AreaFadeState> states)
    {
        if (root == null) return;
        root.SetActive(true);

        var sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sprites.Length; i++)
        {
            var sr = sprites[i];
            if (sr == null) continue;
            states.Add(new AreaFadeState(sr, sr.color));
        }

        var meshes = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshes.Length; i++)
        {
            var mr = meshes[i];
            if (mr == null || mr.material == null) continue;
            states.Add(new AreaFadeState(mr, mr.material.color));
        }

        var lines = root.GetComponentsInChildren<LineRenderer>(true);
        for (int i = 0; i < lines.Length; i++)
        {
            var lr = lines[i];
            if (lr == null) continue;
            states.Add(new AreaFadeState(lr, lr.startColor, lr.endColor));
        }
    }

    private static void SetAlpha(List<AreaFadeState> states, float normalized)
    {
        float p = Mathf.Clamp01(normalized);
        for (int i = 0; i < states.Count; i++)
        {
            var s = states[i];
            if (s.SpriteRenderer != null)
            {
                var c = s.ColorA;
                s.SpriteRenderer.color = new Color(c.r, c.g, c.b, c.a * p);
                continue;
            }

            if (s.MeshRenderer != null && s.MeshRenderer.material != null)
            {
                var c = s.ColorA;
                s.MeshRenderer.material.color = new Color(c.r, c.g, c.b, c.a * p);
                continue;
            }

            if (s.LineRenderer != null)
            {
                var c0 = s.ColorA;
                var c1 = s.ColorB;
                s.LineRenderer.startColor = new Color(c0.r, c0.g, c0.b, c0.a * p);
                s.LineRenderer.endColor = new Color(c1.r, c1.g, c1.b, c1.a * p);
            }
        }
    }

    private static void RestoreFadeStates(List<AreaFadeState> states)
    {
        for (int i = 0; i < states.Count; i++)
        {
            var s = states[i];
            if (s.SpriteRenderer != null)
            {
                s.SpriteRenderer.color = s.ColorA;
                continue;
            }

            if (s.MeshRenderer != null && s.MeshRenderer.material != null)
            {
                s.MeshRenderer.material.color = s.ColorA;
                continue;
            }

            if (s.LineRenderer != null)
            {
                s.LineRenderer.startColor = s.ColorA;
                s.LineRenderer.endColor = s.ColorB;
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

public class LevelSelectTopologyRenderer : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField] private float _fallbackLineWidth = 0.08f;
    [SerializeField] private float _fallbackLineZ = 1000f;
    public Material LineMaterial;
    [SerializeField] private Color _fallbackLineColor = Color.white;

    private readonly List<LineRenderer> _linePool = new List<LineRenderer>();
    private readonly List<AutoLevelButton> _buttons = new List<AutoLevelButton>();
    private int _activeCount;

    private void LateUpdate()
    {
        RefreshButtons();
        _activeCount = 0;
        var cfg = ConnectionColorConfig.Instance;
        Color lockedColor = cfg != null ? cfg.LevelConnectionLockedColor : _fallbackLineColor;
        float lockedWidth = cfg != null ? cfg.LevelConnectionLockedWidth : _fallbackLineWidth;
        Color unlockedColor = cfg != null ? cfg.LevelConnectionUnlockedColor : _fallbackLineColor;
        float unlockedWidth = cfg != null ? cfg.LevelConnectionUnlockedWidth : _fallbackLineWidth;
        float lineZ = cfg != null ? cfg.LevelConnectionZ : _fallbackLineZ;

        for (int i = 0; i < _buttons.Count; i++)
        {
            var from = _buttons[i];
            if (from == null || from.UnlockLevels == null) continue;
            bool prerequisitePassed = SaveManager.Instance.IsLevelCompleted(from.SceneName);
            Color lineColor = prerequisitePassed ? unlockedColor : lockedColor;
            float lineWidth = prerequisitePassed ? unlockedWidth : lockedWidth;

            for (int j = 0; j < from.UnlockLevels.Count; j++)
            {
                var to = from.UnlockLevels[j];
                if (to == null) continue;
                DrawLine(from.transform.position, to.transform.position, lineColor, lineWidth, lineZ);
            }
        }

        for (int i = _activeCount; i < _linePool.Count; i++)
        {
            _linePool[i].enabled = false;
        }
    }

    private void RefreshButtons()
    {
        _buttons.Clear();
        var found = FindObjectsOfType<AutoLevelButton>(true);
        for (int i = 0; i < found.Length; i++)
        {
            var button = found[i];
            if (button == null) continue;
            _buttons.Add(button);
        }
    }

    private void DrawLine(Vector3 from, Vector3 to, Color color, float lineWidth, float lineZ)
    {
        LineRenderer lr;
        if (_activeCount < _linePool.Count)
        {
            lr = _linePool[_activeCount];
            lr.enabled = true;
        }
        else
        {
            var go = new GameObject($"TopologyLine_{_activeCount}");
            go.transform.SetParent(transform);
            lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.numCapVertices = 4;
            if (LineMaterial != null)
                lr.material = LineMaterial;
            else
                lr.material = new Material(Shader.Find("Sprites/Default"));
            _linePool.Add(lr);
        }

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = color;
        lr.endColor = color;
        lr.SetPosition(0, new Vector3(from.x, from.y, lineZ));
        lr.SetPosition(1, new Vector3(to.x, to.y, lineZ));
        lr.sortingOrder = 5;

        _activeCount++;
    }
}

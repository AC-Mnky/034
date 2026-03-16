using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class AutoLevelButton : MonoBehaviour
{
    [Header("Level")]
    public bool IsAlwaysUnlocked;
    public List<AutoLevelButton> UnlockLevels = new List<AutoLevelButton>();
    public string SceneName => gameObject != null ? gameObject.name : string.Empty;

    private GameButton _gameButton;
    private IButtonReceiver _receiver;
    private Image _image;
    private SpriteRenderer _spriteRenderer;
    private Renderer _renderer;
#if UNITY_EDITOR
    private static GUIStyle _nameLabelStyle;
#endif

    private void Awake()
    {
        _gameButton = GetComponent<GameButton>();
        _image = GetComponent<Image>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _renderer = _spriteRenderer == null ? GetComponent<Renderer>() : null;
        SyncButtonName();
    }

    private void OnValidate()
    {
        _gameButton = GetComponent<GameButton>();
        SyncButtonName();
    }

    public void SetReceiver(MonoBehaviour receiver)
    {
        _receiver = receiver as IButtonReceiver;
        EnsureCachedComponents();
        if (_gameButton != null)
            _gameButton.SetReceiver(receiver);
    }

    public void ApplyState(bool isCompleted, bool isUnlocked)
    {
        EnsureCachedComponents();
        var cfg = ColorConfig.Instance;
        if (cfg == null) return;

        Material targetMaterial;
        Color fallbackColor;
        if (isCompleted)
        {
            targetMaterial = cfg.CompletedLevelMaterial;
            fallbackColor = cfg.CompletedLevelColor;
        }
        else if (isUnlocked)
        {
            targetMaterial = cfg.UnlockedLevelMaterial;
            fallbackColor = cfg.UnlockedLevelColor;
        }
        else
        {
            targetMaterial = cfg.LockedLevelMaterial;
            fallbackColor = cfg.LockedLevelColor;
        }

        if (_gameButton != null)
        {
            _gameButton.ButtonColor = fallbackColor;
            _gameButton.ApplyVisual();
            if (_image != null && targetMaterial != null)
                _image.material = targetMaterial;
            return;
        }

        if (_spriteRenderer != null)
        {
            if (targetMaterial != null)
                _spriteRenderer.sharedMaterial = targetMaterial;
            else
                _spriteRenderer.color = fallbackColor;
            return;
        }

        if (_renderer != null)
        {
            if (targetMaterial != null)
                _renderer.sharedMaterial = targetMaterial;
        }
    }

    private void OnMouseDown()
    {
        if (!Application.isPlaying) return;
        if (_receiver == null) return;
        if (string.IsNullOrWhiteSpace(SceneName)) return;
        _receiver.OnButtonDown(SceneName);
    }

    private void EnsureCachedComponents()
    {
        if (_gameButton == null)
            _gameButton = GetComponent<GameButton>();
        if (_image == null)
            _image = GetComponent<Image>();
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_renderer == null && _spriteRenderer == null)
            _renderer = GetComponent<Renderer>();
        SyncButtonName();
    }

    private void SyncButtonName()
    {
        if (_gameButton == null) return;
        _gameButton.ButtonName = SceneName;
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (_nameLabelStyle == null)
        {
            _nameLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            _nameLabelStyle.fontSize = 14;
            _nameLabelStyle.alignment = TextAnchor.MiddleCenter;
            _nameLabelStyle.normal.textColor = Color.red;
        }
        Handles.Label(transform.position + Vector3.up * 0.25f, gameObject.name, _nameLabelStyle);
#endif

        var cfg = ConnectionColorConfig.Instance;
        bool prerequisitePassed = Application.isPlaying && SaveManager.Instance.IsLevelCompleted(SceneName);
        Color gizmoColor = prerequisitePassed
            ? (cfg != null ? cfg.LevelConnectionUnlockedColor : Color.white)
            : (cfg != null ? cfg.LevelConnectionLockedColor : new Color(1f, 1f, 1f, 0.35f));
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.8f);

        if (UnlockLevels == null) return;
        Vector3 from = transform.position;
        for (int i = 0; i < UnlockLevels.Count; i++)
        {
            var toButton = UnlockLevels[i];
            if (toButton == null) continue;
            Vector3 to = toButton.transform.position;
            Gizmos.DrawLine(from, to);
        }
    }
}

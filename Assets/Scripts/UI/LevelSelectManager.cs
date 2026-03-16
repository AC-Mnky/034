using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class LevelSelectManager : MonoBehaviour, IButtonReceiver
{
    [Header("Quit Button")]
    public float QuitButtonSize = 1f;

    private Color QuitButtonColor => ColorConfig.Instance.QuitButtonColor;

    private Canvas _canvas;
    private readonly List<AutoLevelButton> _levelButtons = new List<AutoLevelButton>();
    private readonly HashSet<string> _registeredNames = new HashSet<string>();

    private void Start()
    {
        SaveManager.Instance.Load();
        EnsureTopologyRenderer();
        RebuildTopologyFromScene();
        CreateCanvas();
        RefreshAutoButtons();
        GenerateQuitButton();
    }

    private void EnsureTopologyRenderer()
    {
        var existing = FindObjectOfType<LevelSelectTopologyRenderer>(true);
        if (existing != null) return;

        var go = new GameObject("LevelSelectTopologyRenderer");
        go.AddComponent<LevelSelectTopologyRenderer>();
    }

    private void Update()
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (ctrl && Input.GetKeyDown(KeyCode.T))
        {
            SaveManager.Instance.DebugCompleteAllLevelsOnce();
            RefreshButtons();
            return;
        }

        if (!ctrl && Input.GetKeyDown(KeyCode.R))
        {
            SaveManager.Instance.ClearAll();
            RefreshButtons();
        }
        if (ctrl && Input.GetKeyDown(KeyCode.R))
        {
            SaveManager.Instance.ClearAll();
            BlueprintData.DeleteAllBlueprints();
            RefreshButtons();
        }
    }

    private void RefreshButtons()
    {
        foreach (Transform child in _canvas.transform)
            Destroy(child.gameObject);

        RebuildTopologyFromScene();
        RefreshAutoButtons();
        GenerateQuitButton();
    }

    private void CreateCanvas()
    {
        var canvasGo = new GameObject("UICanvas");
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }
    }

    private void RebuildTopologyFromScene()
    {
        _levelButtons.Clear();
        _registeredNames.Clear();
        var found = FindObjectsOfType<AutoLevelButton>(true);
        for (int i = 0; i < found.Length; i++)
        {
            var button = found[i];
            string sceneName = button != null ? button.SceneName : null;
            if (button == null || string.IsNullOrWhiteSpace(sceneName)) continue;
            if (_registeredNames.Contains(sceneName))
            {
                Debug.LogWarning($"Duplicate AutoLevelButton scene name '{sceneName}'.");
                continue;
            }
            _registeredNames.Add(sceneName);
            _levelButtons.Add(button);
        }
        LevelTopologyRuntime.Rebuild(_levelButtons);
    }

    private void RefreshAutoButtons()
    {
        for (int i = 0; i < _levelButtons.Count; i++)
        {
            var button = _levelButtons[i];
            if (button == null) continue;

            button.SetReceiver(this);
            string levelName = button.SceneName;
            bool completed = SaveManager.Instance.IsLevelCompleted(levelName);
            bool unlocked = SaveManager.Instance.IsLevelUnlocked(levelName);
            button.ApplyState(completed, unlocked);
        }
    }

    private void GenerateQuitButton()
    {
        CreateButton("Quit", ButtonShape.Square, QuitButtonColor,
            new Vector2(0f, 0f), new Vector2(60f, 60f));
    }

    private void CreateButton(string buttonName, ButtonShape shape, Color color,
        Vector2 anchor, Vector2 anchoredPos)
    {
        var go = new GameObject(buttonName, typeof(RectTransform));
        go.transform.SetParent(_canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        float pixelSize = QuitButtonSize * 100f;
        rt.sizeDelta = new Vector2(pixelSize, pixelSize);

        go.AddComponent<Image>().raycastTarget = true;

        var btn = go.AddComponent<GameButton>();
        btn.ButtonName = buttonName;
        btn.Shape = shape;
        btn.ButtonColor = color;
        btn.ButtonSize = Vector2.one * QuitButtonSize;
        btn.SetReceiver(this);
        btn.ApplyVisual();
    }

    public bool OnButtonDown(string buttonName)
    {
        if (buttonName == "Quit")
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return true;
        }

        if (!string.IsNullOrWhiteSpace(buttonName))
        {
            if (SaveManager.Instance.IsLevelUnlocked(buttonName))
            {
                SceneManager.LoadScene(buttonName);
                return true;
            }
        }

        return false;
    }
}

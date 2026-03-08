using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour, IButtonReceiver
{
    [Header("Layout")]
    public int Columns = 5;
    public float ButtonSpacing = 120f;
    public Vector2 GridOrigin = new Vector2(-240f, 100f);
    public float ButtonSize = 1f;

    [Header("Colors")]
    public Color CompletedColor = Color.green;
    public Color UnlockedColor = Color.blue;
    public Color LockedColor = Color.gray;
    public Color QuitButtonColor = Color.red;

    private Canvas _canvas;

    private void Start()
    {
        SaveManager.Instance.Load();
        CreateCanvas();
        GenerateLevelButtons();
        GenerateQuitButton();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SaveManager.Instance.ClearAll();
            RefreshButtons();
        }
        if (Input.GetKeyDown(KeyCode.R) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            SaveManager.Instance.ClearAll();
            for (int i = 0; i < GameConstants.TotalLevelNum; i++)
                BlueprintData.DeleteBlueprint(i);
            RefreshButtons();
        }
    }

    private void RefreshButtons()
    {
        foreach (Transform child in _canvas.transform)
            Destroy(child.gameObject);

        GenerateLevelButtons();
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

    private void GenerateLevelButtons()
    {
        for (int i = 0; i < GameConstants.TotalLevelNum; i++)
        {
            int row = i / Columns;
            int col = i % Columns;
            Vector2 pos = new Vector2(
                GridOrigin.x + col * ButtonSpacing,
                GridOrigin.y - row * ButtonSpacing);

            Color color;
            if (SaveManager.Instance.IsLevelCompleted(i))
                color = CompletedColor;
            else if (SaveManager.Instance.IsLevelUnlocked(i))
                color = UnlockedColor;
            else
                color = LockedColor;

            CreateButton($"Level_{i}", ButtonShape.Square, color,
                new Vector2(0.5f, 0.5f), pos);
        }
    }

    private void GenerateQuitButton()
    {
        CreateButton("Quit", ButtonShape.Square, QuitButtonColor,
            new Vector2(1f, 0f), new Vector2(-60f, 60f));
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
        float pixelSize = ButtonSize * 100f;
        rt.sizeDelta = new Vector2(pixelSize, pixelSize);

        go.AddComponent<Image>().raycastTarget = true;

        var btn = go.AddComponent<GameButton>();
        btn.ButtonName = buttonName;
        btn.Shape = shape;
        btn.ButtonColor = color;
        btn.ButtonSize = Vector2.one * ButtonSize;
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

        if (buttonName.StartsWith("Level_"))
        {
            string indexStr = buttonName.Substring("Level_".Length);
            if (int.TryParse(indexStr, out int levelIndex))
            {
                if (SaveManager.Instance.IsLevelUnlocked(levelIndex))
                {
                    SceneManager.LoadScene(GameConstants.GetLevelSceneName(levelIndex));
                    return true;
                }
            }
        }

        return false;
    }
}

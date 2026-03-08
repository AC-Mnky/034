using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectManager : MonoBehaviour, IButtonReceiver
{
    [Header("Layout")]
    public int Columns = 5;
    public float ButtonSpacing = 1.5f;
    public Vector2 GridOrigin = new Vector2(-3f, 1.5f);
    public float ButtonSize = 1f;

    [Header("Colors")]
    public Color CompletedColor = Color.green;
    public Color UnlockedColor = Color.blue;
    public Color LockedColor = Color.gray;
    public Color QuitButtonColor = Color.red;

    private void Start()
    {
        SaveManager.Instance.Load();
        GenerateLevelButtons();
        GenerateQuitButton();
    }

    private void GenerateLevelButtons()
    {
        for (int i = 0; i < GameConstants.TotalLevelNum; i++)
        {
            int row = i / Columns;
            int col = i % Columns;
            Vector3 pos = new Vector3(
                GridOrigin.x + col * ButtonSpacing,
                GridOrigin.y - row * ButtonSpacing,
                0f);

            Color color;
            if (SaveManager.Instance.IsLevelCompleted(i))
                color = CompletedColor;
            else if (SaveManager.Instance.IsLevelUnlocked(i))
                color = UnlockedColor;
            else
                color = LockedColor;

            CreateButton($"Level_{i}", ButtonShape.Square, color, pos);
        }
    }

    private void GenerateQuitButton()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 pos = cam.ScreenToWorldPoint(new Vector3(Screen.width - 60f, 60f, 10f));
        CreateButton("Quit", ButtonShape.Square, QuitButtonColor, pos);
    }

    private void CreateButton(string buttonName, ButtonShape shape, Color color, Vector3 pos)
    {
        var go = new GameObject(buttonName);
        go.transform.position = pos;

        var btn = go.AddComponent<GameButton>();
        btn.ButtonName = buttonName;
        btn.Shape = shape;
        btn.ButtonColor = color;
        btn.ButtonSize = Vector2.one * ButtonSize;
        btn.SetReceiver(this);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one * ButtonSize;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = color;
        sr.sortingOrder = 10;
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

public static class GameConstants
{
    public const int TotalLevelNum = 3;
    public const int InitialUnlockedLevelNum = 1;
    public const int MaxConnectNumber = 3;
    public const float MaxHorizontalSpan = 0.4f;
    public const float MaxVerticalSpan = 0.4f;

    public const string LevelSelectSceneName = "LevelSelect";
    public static string GetLevelSceneName(int index) => $"Level_{index}";
}

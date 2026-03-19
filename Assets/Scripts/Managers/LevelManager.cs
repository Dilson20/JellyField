using UnityEngine;
using UnityEngine.Events;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Current Level")]
    public LevelData levelData;

    [Header("Events — hook up UI here")]
    public UnityEvent onLevelComplete;
    public UnityEvent onGameOver;
    public UnityEvent<int> onScoreChanged;
    public UnityEvent<int, int> onColorProgress;

    private int[] mergeCount = new int[6];
    private int score;
    public int Score => score;

    public int GetMergeCount(int colorIndex) => mergeCount[colorIndex];

    public int GetRequirement(int colorIndex)
    {
        if (levelData == null) return 0;
        int[] reqs = {
            levelData.requireRed, levelData.requireBlue, levelData.requireGreen,
            levelData.requireYellow, levelData.requirePurple, levelData.requirePink
        };
        return colorIndex >= 0 && colorIndex < reqs.Length ? reqs[colorIndex] : 0;
    }

    void Awake() { Instance = this; }

    public void RegisterMerge(int colorIndex, int quadrantCount = 1)
    {
        if (colorIndex < 0 || colorIndex >= 6) return;

        mergeCount[colorIndex] += quadrantCount;
        score += (levelData?.pointsPerMerge ?? 10) * quadrantCount;
        onScoreChanged?.Invoke(score);

        int requirement = GetRequirement(colorIndex);
        int remaining = Mathf.Max(0, requirement - mergeCount[colorIndex]);
        onColorProgress?.Invoke(colorIndex, remaining);

        if (CheckWin())
        {
            score += levelData?.bonusPerLevel ?? 100;
            onScoreChanged?.Invoke(score);
            onLevelComplete?.Invoke();
            Debug.Log($"Level Complete! Score: {score}");
        }
    }

    bool CheckWin()
    {
        if (levelData == null || levelData.IsSandbox()) return false;
        return mergeCount[0] >= levelData.requireRed
            && mergeCount[1] >= levelData.requireBlue
            && mergeCount[2] >= levelData.requireGreen
            && mergeCount[3] >= levelData.requireYellow
            && mergeCount[4] >= levelData.requirePurple
            && mergeCount[5] >= levelData.requirePink;
    }

    public void CheckGameOver()
    {
        var gm = GridManager.Instance;
        if (gm == null) return;
        if (gm.IsGridFull() && !gm.HasAnyMergePossible())
            onGameOver?.Invoke();
    }

    public void ResetLevel()
    {
        mergeCount = new int[6];
        score = 0;
        onScoreChanged?.Invoke(score);
    }
}
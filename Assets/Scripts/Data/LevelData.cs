using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "JellyField/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName = "Level 1";

    [Header("Color Requirements — how many merges needed per color to win")]
    public int requireRed = 0;
    public int requireBlue = 0;
    public int requireGreen = 0;
    public int requireYellow = 0;
    public int requirePurple = 0;

    [Header("Color Spawn Weights — higher = spawns more often (0 = never spawns)")]
    [Range(0, 10)] public int weightRed = 1;
    [Range(0, 10)] public int weightBlue = 1;
    [Range(0, 10)] public int weightGreen = 1;
    [Range(0, 10)] public int weightYellow = 1;
    [Range(0, 10)] public int weightPurple = 1;

    [Header("Score")]
    public int pointsPerMerge = 10;
    public int bonusPerLevel = 100;

    // Returns a weighted random color index based on weights
    public int GetWeightedRandomColor()
    {
        int[] weights = { weightRed, weightBlue, weightGreen, weightYellow, weightPurple };
        int total = 0;
        foreach (int w in weights) total += w;
        if (total == 0) return Random.Range(0, 5);

        int roll = Random.Range(0, total);
        int cumulative = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) return i;
        }
        return 0;
    }

    // Returns true if ALL requirements are zero (no goals = instant win, treated as sandbox)
    public bool IsSandbox()
    {
        return requireRed == 0 && requireBlue == 0 && requireGreen == 0
            && requireYellow == 0 && requirePurple == 0;
    }
}
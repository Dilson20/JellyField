using UnityEngine;

public class JellyTile : MonoBehaviour
{
    public int colorID;        // 0=Red, 1=Blue, 2=Green, 3=Yellow, 4=Purple
    public int gridX;
    public int gridY;

    private SpriteRenderer sr;

    public static readonly Color[] JellyColors = new Color[]
    {
        new Color(0.95f, 0.30f, 0.30f), // Red
        new Color(0.30f, 0.60f, 0.95f), // Blue
        new Color(0.30f, 0.85f, 0.45f), // Green
        new Color(0.98f, 0.85f, 0.25f), // Yellow
        new Color(0.75f, 0.35f, 0.95f)  // Purple
    };

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(int x, int y, int id)
    {
        gridX = x;
        gridY = y;
        colorID = id;
        sr.color = JellyColors[id];
    }
}
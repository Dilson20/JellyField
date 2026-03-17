using UnityEngine;

public class JellyTile : MonoBehaviour
{
    public int gridX;
    public int gridY;

    // Each tile has 4 colored quadrants
    public int[] quadrantColors = new int[4]; // 0=Red,1=Blue,2=Green,3=Yellow,4=Purple

    private SpriteRenderer[] quadrantRenderers = new SpriteRenderer[4];

    public static readonly Color[] JellyColors = new Color[]
    {
        new Color(0.95f, 0.30f, 0.30f), // Red
        new Color(0.30f, 0.60f, 0.95f), // Blue
        new Color(0.30f, 0.85f, 0.45f), // Green
        new Color(0.98f, 0.85f, 0.25f), // Yellow
        new Color(0.75f, 0.35f, 0.95f)  // Purple
    };

    // Quadrant positions (top-left, top-right, bottom-left, bottom-right)
    private static readonly Vector2[] QuadrantOffsets = new Vector2[]
    {
        new Vector2(-0.25f,  0.25f), // top-left
        new Vector2( 0.25f,  0.25f), // top-right
        new Vector2(-0.25f, -0.25f), // bottom-left
        new Vector2( 0.25f, -0.25f)  // bottom-right
    };

    void Awake()
    {
        // Get all 4 child SpriteRenderers in order
        for (int i = 0; i < 4; i++)
        {
            quadrantRenderers[i] = transform.GetChild(i).GetComponent<SpriteRenderer>();
        }
    }

    public void Init(int x, int y)
    {
        gridX = x;
        gridY = y;

        // Randomly decide how many quadrants are filled (2, 3, or 4)
        int filledCount = Random.Range(2, 5);

        // Shuffle indices so filled positions are random
        int[] indices = { 0, 1, 2, 3 };
        Shuffle(indices);

        for (int i = 0; i < 4; i++)
        {
            if (i < filledCount)
            {
                // Filled quadrant � random color
                quadrantColors[indices[i]] = Random.Range(0, JellyColors.Length);
                quadrantRenderers[indices[i]].color = JellyColors[quadrantColors[indices[i]]];
                quadrantRenderers[indices[i]].gameObject.SetActive(true);
            }
            else
            {
                // Empty quadrant � hide it
                quadrantRenderers[indices[i]].gameObject.SetActive(false);
                quadrantColors[indices[i]] = -1; // -1 = empty
            }
        }
    }

    void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

    public void SetQuadrantColor(int quadrant, int colorID)
    {
        quadrantColors[quadrant] = colorID;
        quadrantRenderers[quadrant].color = JellyColors[colorID];
    }
}
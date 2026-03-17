using UnityEngine;

public class JellyTile : MonoBehaviour
{
    public int gridX;
    public int gridY;
    public int[] quadrantColors = new int[4];
    private JiggleEffect jiggle;
    private int[] displayedBy = new int[] { 0, 1, 2, 3 };
    private int[] mergePartner = { -1, -1, -1, -1 };


    private SpriteRenderer[] quadrantRenderers = new SpriteRenderer[4];

    public static readonly Color[] JellyColors = new Color[]
    {
        new Color(0.95f, 0.30f, 0.30f), // Red
        new Color(0.30f, 0.60f, 0.95f), // Blue
        new Color(0.30f, 0.85f, 0.45f), // Green
        new Color(0.98f, 0.85f, 0.25f), // Yellow
        new Color(0.75f, 0.35f, 0.95f)  // Purple
    };

    // Adjacent pairs that can merge: 0=top row, 1=bottom row, 2=left col, 3=right col
    private static readonly int[][] MergePairs = new int[][]
    {
        new int[] { 0, 1 }, // top horizontal
        new int[] { 2, 3 }, // bottom horizontal
        new int[] { 0, 2 }, // left vertical
        new int[] { 1, 3 }  // right vertical
    };

    void Awake()
    {
        for (int i = 0; i < 4; i++)
            quadrantRenderers[i] = transform.GetChild(i).GetComponent<SpriteRenderer>();

        // Add jiggle component automatically
        jiggle = gameObject.AddComponent<JiggleEffect>();
    }

    public void OnPickup() => jiggle?.PlayPickup();
    public void OnDrop() => jiggle?.PlayDrop();
    public void OnSwap() => jiggle?.PlaySwap();
    public void OnDrag() => jiggle?.PlayDrag();
    public void OnIdle() => jiggle?.PlayIdle();

    public void Init(int x, int y)
    {
        gridX = x;
        gridY = y;
        ResetAllQuadrants();

        int layout = Random.Range(0, 3);

        if (layout == 0)
        {
            // 4 individual squares, no same color cross-adjacent
            AssignAllUnique();
        }
        else if (layout == 1)
        {
            // Top pair merged + Bottom pair merged
            int topColor = Random.Range(0, JellyColors.Length);
            int botColor = PickDifferentFrom(topColor);

            quadrantColors[0] = topColor;
            quadrantColors[1] = topColor;
            quadrantColors[2] = botColor;
            quadrantColors[3] = botColor;

            ApplyMerge(0, topColor); // merge Q0+Q1 top horizontal
            ApplyMerge(1, botColor); // merge Q2+Q3 bottom horizontal
        }
        else
        {
            // Left pair merged + Right pair merged
            int leftColor = Random.Range(0, JellyColors.Length);
            int rightColor = PickDifferentFrom(leftColor);

            quadrantColors[0] = leftColor;
            quadrantColors[2] = leftColor;
            quadrantColors[1] = rightColor;
            quadrantColors[3] = rightColor;

            ApplyMerge(2, leftColor);  // merge Q0+Q2 left vertical
            ApplyMerge(3, rightColor); // merge Q1+Q3 right vertical
        }

        //OnIdle();
    }

    void ResetAllQuadrants()
    {
        mergePartner = new int[] { -1, -1, -1, -1 };
        displayedBy = new int[] { 0, 1, 2, 3 };

        // Q0 top-left
        quadrantRenderers[0].transform.localPosition = new Vector3(-0.25f, 0.25f, 0);
        quadrantRenderers[0].transform.localScale = new Vector3(0.48f, 0.48f, 1);
        // Q1 top-right
        quadrantRenderers[1].transform.localPosition = new Vector3(0.25f, 0.25f, 0);
        quadrantRenderers[1].transform.localScale = new Vector3(0.48f, 0.48f, 1);
        // Q2 bottom-left
        quadrantRenderers[2].transform.localPosition = new Vector3(-0.25f, -0.25f, 0);
        quadrantRenderers[2].transform.localScale = new Vector3(0.48f, 0.48f, 1);
        // Q3 bottom-right
        quadrantRenderers[3].transform.localPosition = new Vector3(0.25f, -0.25f, 0);
        quadrantRenderers[3].transform.localScale = new Vector3(0.48f, 0.48f, 1);

        for (int i = 0; i < 4; i++)
            quadrantRenderers[i].gameObject.SetActive(true);
    }

    void ApplyMerge(int pairIndex, int color)
    {
        int[] pair = MergePairs[pairIndex];
        int a = pair[0];
        int b = pair[1];

        mergePartner[a] = b; // track partners
        mergePartner[b] = a;

        quadrantRenderers[b].gameObject.SetActive(false);
        quadrantColors[b] = color;
        displayedBy[b] = a;

        Transform t = quadrantRenderers[a].transform;
        switch (pairIndex)
        {
            case 0: t.localPosition = new Vector3(0f, 0.25f, 0); t.localScale = new Vector3(0.96f, 0.48f, 1); break;
            case 1: t.localPosition = new Vector3(0f, -0.25f, 0); t.localScale = new Vector3(0.96f, 0.48f, 1); break;
            case 2: t.localPosition = new Vector3(-0.25f, 0f, 0); t.localScale = new Vector3(0.48f, 0.96f, 1); break;
            case 3: t.localPosition = new Vector3(0.25f, 0f, 0); t.localScale = new Vector3(0.48f, 0.96f, 1); break;
        }
        quadrantRenderers[a].color = JellyColors[color];
        quadrantRenderers[a].gameObject.SetActive(true);
    }

    void AssignAllUnique()
    {
        // Pick 4 completely different colors
        int[] available = { 0, 1, 2, 3, 4 };
        Shuffle(available);

        for (int i = 0; i < 4; i++)
        {
            quadrantColors[i] = available[i];
            quadrantRenderers[i].color = JellyColors[available[i]];
            quadrantRenderers[i].gameObject.SetActive(true);
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

    int PickColorAvoidingNeighbors(int quadrant, int forbidden)
    {
        int c;
        int attempts = 0;
        do
        {
            c = Random.Range(0, JellyColors.Length);
            attempts++;
        } while (c == forbidden && attempts < 20);
        return c;
    }

    int PickDifferentFrom(int a)
    {
        int c;
        do { c = Random.Range(0, JellyColors.Length); } while (c == a);
        return c;
    }

    int PickDifferentFrom2(int a, int b)
    {
        int c;
        int attempts = 0;
        do
        {
            c = Random.Range(0, JellyColors.Length);
            attempts++;
        } while ((c == a || c == b) && attempts < 20);
        return c;
    }

    int[] GetOtherQuadrants(int[] pair)
    {
        System.Collections.Generic.List<int> others = new();
        for (int i = 0; i < 4; i++)
            if (i != pair[0] && i != pair[1])
                others.Add(i);
        return others.ToArray();
    }

    public void SetQuadrantColor(int quadrant, int colorID)
    {
        quadrantColors[quadrant] = colorID;
        quadrantRenderers[quadrant].color = JellyColors[colorID];
    }

    public void ClearQuadrant(int index)
    {
        int partner = mergePartner[index];

        // Clear this quadrant
        quadrantColors[index] = -1;
        mergePartner[index] = -1;
        int displayIdx = displayedBy[index];
        RestoreQuadrantVisual(displayIdx);
        quadrantRenderers[displayIdx].gameObject.SetActive(false);
        displayedBy[index] = index;

        if (partner >= 0 && quadrantColors[partner] >= 0)
        {
            // Feature 1 — 2-size block disappears entirely
            quadrantColors[partner] = -1;
            mergePartner[partner] = -1;
            int partnerDisplay = displayedBy[partner];
            RestoreQuadrantVisual(partnerDisplay);
            quadrantRenderers[partnerDisplay].gameObject.SetActive(false);
            displayedBy[partner] = partner;
        }
        else
        {
            // Feature 2 — expand adjacent quadrant into cleared space
            ExpandNeighborIntoCleared(index);
        }
    }

    void ExpandNeighborIntoCleared(int clearedIndex)
    {
        for (int p = 0; p < MergePairs.Length; p++)
        {
            int[] pair = MergePairs[p];
            int other = -1;
            if (pair[0] == clearedIndex) other = pair[1];
            else if (pair[1] == clearedIndex) other = pair[0];

            if (other < 0) continue;
            if (quadrantColors[other] < 0) continue;  // other is empty
            if (mergePartner[other] >= 0) continue;   // already merged

            // Expand 'other' to fill both spots
            mergePartner[other] = clearedIndex;
            displayedBy[clearedIndex] = other;
            quadrantColors[clearedIndex] = quadrantColors[other];

            Transform t = quadrantRenderers[other].transform;
            switch (p)
            {
                case 0: t.localPosition = new Vector3(0f, 0.25f, 0); t.localScale = new Vector3(0.96f, 0.48f, 1); break;
                case 1: t.localPosition = new Vector3(0f, -0.25f, 0); t.localScale = new Vector3(0.96f, 0.48f, 1); break;
                case 2: t.localPosition = new Vector3(-0.25f, 0f, 0); t.localScale = new Vector3(0.48f, 0.96f, 1); break;
                case 3: t.localPosition = new Vector3(0.25f, 0f, 0); t.localScale = new Vector3(0.48f, 0.96f, 1); break;
            }
            quadrantRenderers[other].color = JellyColors[quadrantColors[other]];
            quadrantRenderers[other].gameObject.SetActive(true);
            break; // only expand one neighbor
        }
    }

    void RestoreQuadrantVisual(int index)
    {
        Vector3[] positions = {
        new Vector3(-0.25f,  0.25f, 0),
        new Vector3( 0.25f,  0.25f, 0),
        new Vector3(-0.25f, -0.25f, 0),
        new Vector3( 0.25f, -0.25f, 0)
    };
        quadrantRenderers[index].transform.localPosition = positions[index];
        quadrantRenderers[index].transform.localScale = new Vector3(0.48f, 0.48f, 1);
    }
}
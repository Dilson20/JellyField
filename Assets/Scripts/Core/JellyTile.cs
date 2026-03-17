using System.Collections;
using UnityEngine;

public class JellyTile : MonoBehaviour
{
    public int gridX;
    public int gridY;
    public int[] quadrantColors = new int[4];
    private JiggleEffect jiggle;
    private SpriteRenderer[] quadrantRenderers = new SpriteRenderer[4];
    private int[] displayedBy = new int[] { 0, 1, 2, 3 };
    private int[] mergePartner = { -1, -1, -1, -1 };

    public static readonly Color[] JellyColors = new Color[]
    {
        new Color(0.95f, 0.30f, 0.30f),
        new Color(0.30f, 0.60f, 0.95f),
        new Color(0.30f, 0.85f, 0.45f),
        new Color(0.98f, 0.85f, 0.25f),
        new Color(0.75f, 0.35f, 0.95f)
    };

    private static readonly int[][] MergePairs = new int[][]
    {
        new int[] { 0, 1 },
        new int[] { 2, 3 },
        new int[] { 0, 2 },
        new int[] { 1, 3 }
    };

    void Awake()
    {
        for (int i = 0; i < 4; i++)
            quadrantRenderers[i] = transform.GetChild(i).GetComponent<SpriteRenderer>();
        jiggle = gameObject.AddComponent<JiggleEffect>();
    }

    public void OnPickup() => jiggle?.PlayPickup();
    public void OnDrop() => jiggle?.PlayDrop();
    public void OnSwap() => jiggle?.PlaySwap();
    public void OnDrag() => jiggle?.PlayDrag();
    public void OnIdle() => jiggle?.PlayIdle();
    public int GetMergePartner(int index) => mergePartner[index];

    public void Init(int x, int y)
    {
        gridX = x;
        gridY = y;
        ResetAllQuadrants();

        int layout = Random.Range(0, 3);

        if (layout == 0)
        {
            AssignAllUnique();
        }
        else if (layout == 1)
        {
            int topColor = Random.Range(0, JellyColors.Length);
            int botColor = PickDifferentFrom(topColor);
            quadrantColors[0] = topColor; quadrantColors[1] = topColor;
            quadrantColors[2] = botColor; quadrantColors[3] = botColor;
            ApplyMerge(0, topColor);
            ApplyMerge(1, botColor);
        }
        else
        {
            int leftColor = Random.Range(0, JellyColors.Length);
            int rightColor = PickDifferentFrom(leftColor);
            quadrantColors[0] = leftColor; quadrantColors[2] = leftColor;
            quadrantColors[1] = rightColor; quadrantColors[3] = rightColor;
            ApplyMerge(2, leftColor);
            ApplyMerge(3, rightColor);
        }

        //OnIdle();
    }

    void ResetAllQuadrants()
    {
        displayedBy = new int[] { 0, 1, 2, 3 };
        mergePartner = new int[] { -1, -1, -1, -1 };

        quadrantRenderers[0].transform.localPosition = new Vector3(-0.25f, 0.25f, 0);
        quadrantRenderers[0].transform.localScale = new Vector3(0.48f, 0.48f, 1);
        quadrantRenderers[1].transform.localPosition = new Vector3(0.25f, 0.25f, 0);
        quadrantRenderers[1].transform.localScale = new Vector3(0.48f, 0.48f, 1);
        quadrantRenderers[2].transform.localPosition = new Vector3(-0.25f, -0.25f, 0);
        quadrantRenderers[2].transform.localScale = new Vector3(0.48f, 0.48f, 1);
        quadrantRenderers[3].transform.localPosition = new Vector3(0.25f, -0.25f, 0);
        quadrantRenderers[3].transform.localScale = new Vector3(0.48f, 0.48f, 1);

        for (int i = 0; i < 4; i++)
            quadrantRenderers[i].gameObject.SetActive(true);
    }

    void ApplyMerge(int pairIndex, int color)
    {
        int[] pair = MergePairs[pairIndex];
        int a = pair[0], b = pair[1];

        mergePartner[a] = b;
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

    public void ClearQuadrant(int index)
    {
        if (IsFullTile())
        {
            for (int i = 0; i < 4; i++)
            {
                quadrantColors[i] = -1;
                mergePartner[i] = -1;
                displayedBy[i] = i;
                RestoreQuadrantVisual(i);
                quadrantRenderers[i].gameObject.SetActive(false);
            }
            return;
        }

        int partner = mergePartner[index];

        // Clear this quadrant visually
        quadrantColors[index] = -1;
        mergePartner[index] = -1;
        int displayIdx = displayedBy[index];
        RestoreQuadrantVisual(displayIdx);
        quadrantRenderers[displayIdx].gameObject.SetActive(false);
        displayedBy[index] = index;

        if (partner >= 0 && quadrantColors[partner] >= 0)
        {
            // Both were merged — clear partner too (2-size block disappears entirely)
            quadrantColors[partner] = -1;
            mergePartner[partner] = -1;
            int partnerDisplay = displayedBy[partner];
            RestoreQuadrantVisual(partnerDisplay);
            quadrantRenderers[partnerDisplay].gameObject.SetActive(false);
            displayedBy[partner] = partner;

            // Give remaining quadrants a chance to expand into both freed slots
            ExpandNeighborIntoCleared(index);
            ExpandNeighborIntoCleared(partner);
        }
        else
        {
            // Solo quadrant cleared — expand an adjacent partner into the space
            ExpandNeighborIntoCleared(index);
        }
        TryExpandToFullTile();
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
            if (quadrantColors[other] < 0) continue;
            if (mergePartner[other] >= 0) continue;
            if (displayedBy[other] != other) continue;

            // Expand other to cover the cleared slot
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
            break;
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

    void AssignAllUnique()
    {
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
            int temp = array[i]; array[i] = array[j]; array[j] = temp;
        }
    }

    int PickDifferentFrom(int a)
    {
        int c;
        do { c = Random.Range(0, JellyColors.Length); } while (c == a);
        return c;
    }

    public void SetQuadrantColor(int quadrant, int colorID)
    {
        quadrantColors[quadrant] = colorID;
        quadrantRenderers[quadrant].color = JellyColors[colorID];
    }

    public bool IsFullyEmpty()
    {
        for (int i = 0; i < 4; i++)
            if (quadrantColors[i] >= 0) return false;
        return true;
    }

    bool IsFullTile()
    {
        // Full tile: Q0 is display for all, all quadrants have same color
        for (int i = 1; i < 4; i++)
            if (displayedBy[i] != 0) return false;
        return quadrantColors[0] >= 0;
    }

    public void TryExpandToFullTile()
    {
        // Path 1: all 4 quadrants explicitly the same color
        int color = quadrantColors[0];
        if (color >= 0)
        {
            bool allSame = true;
            for (int i = 1; i < 4; i++)
                if (quadrantColors[i] != color) { allSame = false; break; }
            if (allSame) { PromoteToFull(color); return; }
        }

        // Path 2: exactly 2 active quadrants, same color, other 2 empty
        int p1 = -1, p2 = -1;
        color = -1;
        for (int i = 0; i < 4; i++)
        {
            if (quadrantColors[i] < 0) continue;
            if (color == -1) { color = quadrantColors[i]; p1 = i; }
            else if (quadrantColors[i] == color && p2 == -1) p2 = i;
            else return; // 3+ active or mixed colors → abort
        }

        if (color == -1 || p1 == -1 || p2 == -1) return; // 0 or 1 active quad

        // The other 2 must be empty (-1)
        for (int i = 0; i < 4; i++)
            if (i != p1 && i != p2 && quadrantColors[i] != -1) return;

        // ✅ Removed partner check — 2 same-color quads + 2 empty always = valid 4-size
        PromoteToFull(color);
    }

    void PromoteToFull(int color)
    {
        for (int i = 1; i < 4; i++)
        {
            quadrantRenderers[i].gameObject.SetActive(false);
            displayedBy[i] = 0;
            mergePartner[i] = 0;
            quadrantColors[i] = color;
        }
        mergePartner[0] = -1;
        quadrantRenderers[0].transform.localPosition = Vector3.zero;
        quadrantRenderers[0].transform.localScale = new Vector3(0.96f, 0.96f, 1);
        quadrantRenderers[0].color = JellyColors[color];
        quadrantRenderers[0].gameObject.SetActive(true);
        quadrantColors[0] = color;
    }

    // ── Add this helper coroutine anywhere in JellyTile ──────────────────────
    IEnumerator AnimateScale(Transform tr, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        tr.localScale = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Cubic ease-out with slight overshoot (feels springy)
            float ease = 1f - Mathf.Pow(1f - Mathf.Min(t, 1f), 3f);
            tr.localScale = Vector3.LerpUnclamped(from, to, ease);
            yield return null;
        }
        tr.localScale = to;
    }

    // ── Replace SetActive(false) in ClearQuadrant with this ───────────────────
    // Instead of: quadrantRenderers[displayIdx].gameObject.SetActive(false);
    // Use:
    void AnimateOut(int displayIdx)
    {
        var tr = quadrantRenderers[displayIdx].transform;
        Vector3 current = tr.localScale;
        StartCoroutine(AnimateAndHide(tr, current, Vector3.zero, 0.12f,
            quadrantRenderers[displayIdx].gameObject));
    }

    IEnumerator AnimateAndHide(Transform tr, Vector3 from, Vector3 to,
                                float duration, GameObject hideAfter)
    {
        yield return AnimateScale(tr, from, to, duration);
        hideAfter.SetActive(false);
        tr.localScale = from; // restore scale for reuse
    }

    // ── Replace direct scale sets in ExpandNeighborIntoCleared ───────────────
    // Instead of: quadrantRenderers[displayIdx].transform.localScale = newScale;
    // Use:
    void AnimateExpandIn(int displayIdx, Vector3 targetScale)
    {
        var tr = quadrantRenderers[displayIdx].transform;
        quadrantRenderers[displayIdx].gameObject.SetActive(true);
        StartCoroutine(AnimateScale(tr, tr.localScale * 0.7f, targetScale, 0.18f));
    }

    // ── Replace the scale set in TryExpandToFullTile ──────────────────────────
    // Instead of: quadrantRenderers[0].transform.localScale = new Vector3(0.96f, 0.96f, 1f);
    // Use:
    void AnimateFullExpand()
    {
        var tr = quadrantRenderers[0].transform;
        Vector3 current = tr.localScale;
        StartCoroutine(AnimateScale(tr, current, new Vector3(0.96f, 0.96f, 1f), 0.2f));
    }
}
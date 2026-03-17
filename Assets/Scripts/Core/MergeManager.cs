using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance;
    void Awake() { Instance = this; }

    private static readonly int[] RightOurs = { 1, 3 };
    private static readonly int[] RightTheirs = { 0, 2 };
    private static readonly int[] LeftOurs = { 0, 2 };
    private static readonly int[] LeftTheirs = { 1, 3 };
    private static readonly int[] UpOurs = { 0, 1 };
    private static readonly int[] UpTheirs = { 2, 3 };
    private static readonly int[] DownOurs = { 2, 3 };
    private static readonly int[] DownTheirs = { 0, 1 };

    public void CheckMerges(JellyTile placedTile)
    {
        StartCoroutine(DoMerge(placedTile));
    }

    IEnumerator DoMerge(JellyTile tile)
    {
        yield return new WaitForSeconds(0.15f);
        if (tile == null) yield break;

        GridManager gm = GridManager.Instance;

        // Store (tile, quadrant, SNAPSHOT COLOR) so we never double-clear after expansion refills a slot
        var merges = new List<(JellyTile ourTile, int ourQ, int ourColor, JellyTile theirTile, int theirQ, int theirColor)>();
        CollectMerges(tile, gm, 1, 0, RightOurs, RightTheirs, merges);
        CollectMerges(tile, gm, -1, 0, LeftOurs, LeftTheirs, merges);
        CollectMerges(tile, gm, 0, 1, UpOurs, UpTheirs, merges);
        CollectMerges(tile, gm, 0, -1, DownOurs, DownTheirs, merges);

        if (merges.Count == 0) yield break;

        var affected = new HashSet<JellyTile>();
        foreach (var (ourTile, ourQ, ourColor, theirTile, theirQ, theirColor) in merges)
        {
            // ✅ Only clear if the quadrant STILL holds the exact color that was matched
            // This prevents double-clearing when ExpandNeighborIntoCleared refills a slot
            if (ourTile != null && ourTile.quadrantColors[ourQ] == ourColor)
                ourTile.ClearQuadrant(ourQ);
            if (theirTile != null && theirTile.quadrantColors[theirQ] == theirColor)
                theirTile.ClearQuadrant(theirQ);

            ourTile.OnSwap();
            theirTile.OnSwap();
            affected.Add(ourTile);
            affected.Add(theirTile);
        }

        foreach (var t in affected)
            if (t != null && !t.IsFullyEmpty())
                t.TryExpandToFullTile();

        foreach (var t in affected)
            CheckAndDestroyEmpty(t, gm);
    }

    void CollectMerges(JellyTile tile, GridManager gm, int dx, int dy,
                   int[] ourQuads, int[] theirQuads,
                   List<(JellyTile, int, int, JellyTile, int, int)> merges)
    {
        JellyTile neighbor = gm.GetTile(tile.gridX + dx, tile.gridY + dy);
        if (neighbor == null) return;

        for (int i = 0; i < ourQuads.Length; i++)
        {
            int ourColor = tile.quadrantColors[ourQuads[i]];
            int theirColor = neighbor.quadrantColors[theirQuads[i]];
            if (ourColor >= 0 && theirColor >= 0 && ourColor == theirColor)
                merges.Add((tile, ourQuads[i], ourColor, neighbor, theirQuads[i], theirColor));
        }
    }

    void CheckAndDestroyEmpty(JellyTile tile, GridManager gm)
    {
        if (tile == null || !tile.IsFullyEmpty()) return;
        gm.RemoveTile(tile.gridX, tile.gridY);
        Destroy(tile.gameObject);
    }
}
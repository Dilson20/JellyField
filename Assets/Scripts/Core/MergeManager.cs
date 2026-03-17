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

        // STEP 1: Snapshot all merge pairs across ALL directions before touching anything
        var merges = new List<(JellyTile ourTile, int ourQ, JellyTile theirTile, int theirQ)>();
        CollectMerges(tile, gm, 1, 0, RightOurs, RightTheirs, merges);
        CollectMerges(tile, gm, -1, 0, LeftOurs, LeftTheirs, merges);
        CollectMerges(tile, gm, 0, 1, UpOurs, UpTheirs, merges);
        CollectMerges(tile, gm, 0, -1, DownOurs, DownTheirs, merges);

        if (merges.Count == 0) yield break;

        // STEP 2: Execute all clears
        var affected = new HashSet<JellyTile>();
        foreach (var (ourTile, ourQ, theirTile, theirQ) in merges)
        {
            if (ourTile.quadrantColors[ourQ] >= 0)
                ourTile.ClearQuadrant(ourQ);
            if (theirTile.quadrantColors[theirQ] >= 0)
                theirTile.ClearQuadrant(theirQ);

            ourTile.OnSwap();
            theirTile.OnSwap();
            affected.Add(ourTile);
            affected.Add(theirTile);
        }

        // STEP 3: Now that ALL clears + neighbor expansions are done,
        //         run TryExpandToFullTile on every affected tile
        //         This is what was missing — full-tile promotion was checked
        //         mid-batch before all expansions completed
        foreach (var t in affected)
            if (t != null && !t.IsFullyEmpty())
                t.TryExpandToFullTile();

        // STEP 4: Destroy empties
        foreach (var t in affected)
            CheckAndDestroyEmpty(t, gm);
    }

    void CollectMerges(JellyTile tile, GridManager gm, int dx, int dy,
                       int[] ourQuads, int[] theirQuads,
                       List<(JellyTile, int, JellyTile, int)> merges)
    {
        JellyTile neighbor = gm.GetTile(tile.gridX + dx, tile.gridY + dy);
        if (neighbor == null) return;

        for (int i = 0; i < ourQuads.Length; i++)
        {
            int ourColor = tile.quadrantColors[ourQuads[i]];
            int theirColor = neighbor.quadrantColors[theirQuads[i]];
            if (ourColor >= 0 && theirColor >= 0 && ourColor == theirColor)
                merges.Add((tile, ourQuads[i], neighbor, theirQuads[i]));
        }
    }

    void CheckAndDestroyEmpty(JellyTile tile, GridManager gm)
    {
        if (tile == null || !tile.IsFullyEmpty()) return;
        gm.RemoveTile(tile.gridX, tile.gridY);
        Destroy(tile.gameObject);
    }
}
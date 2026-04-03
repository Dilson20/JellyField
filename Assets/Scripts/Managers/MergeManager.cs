using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance;
    void Awake() { Instance = this; }

    public bool enableChainMerge = true;

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
        StartCoroutine(DoMerge(placedTile, false));
    }

    IEnumerator DoMerge(JellyTile tile, bool isChain)
    {
        // Chain merges use a shorter delay for snappier feel
        yield return new WaitForSeconds(isChain ? (0.1f / JellyTile.AnimSpeed) : (0.15f / JellyTile.AnimSpeed));
        if (tile == null) yield break;

        GridManager gm = GridManager.Instance;

        var merges = new List<(JellyTile ourTile, int ourQ, int ourColor, JellyTile theirTile, int theirQ, int theirColor)>();
        CollectMerges(tile, gm, 1, 0, RightOurs, RightTheirs, merges);
        CollectMerges(tile, gm, -1, 0, LeftOurs, LeftTheirs, merges);
        CollectMerges(tile, gm, 0, 1, UpOurs, UpTheirs, merges);
        CollectMerges(tile, gm, 0, -1, DownOurs, DownTheirs, merges);

        if (merges.Count == 0) yield break;

        var affected = new HashSet<JellyTile>();
        foreach (var (ourTile, ourQ, ourColor, theirTile, theirQ, theirColor) in merges)
        {
            bool cleared = false;
            if (ourTile != null && ourTile.quadrantColors[ourQ] == ourColor)
            {
                ourTile.ClearQuadrant(ourQ);
                cleared = true;
            }
            if (theirTile != null && theirTile.quadrantColors[theirQ] == theirColor)
            {
                theirTile.ClearQuadrant(theirQ);
                cleared = true;
            }

            if (!cleared) continue;

            ourTile.OnSwap();
            theirTile.OnSwap();
            affected.Add(ourTile);
            affected.Add(theirTile);
            LevelManager.Instance?.RegisterMerge(ourColor);
        }

        SoundManager.Instance?.PlayMerge();

        foreach (var t in affected)
            if (t != null && !t.IsFullyEmpty())
                t.TryExpandToFullTile();

        // ── Collect survivors before destroying ───────────────────────────
        var survivors = new HashSet<JellyTile>();
        foreach (var t in affected)
        {
            if (t == null) continue;
            if (t.IsFullyEmpty())
                CheckAndDestroyEmpty(t, gm);
            else
                survivors.Add(t);
        }

        // ── Chain merge: re-check all surviving tiles and their neighbors ─
        if (enableChainMerge)
        {
            var chainTargets = new HashSet<JellyTile>();
            foreach (var t in survivors)
            {
                if (t == null) continue;
                chainTargets.Add(t);
                // Also check neighbors so they can react to newly-freed slots
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (dx != 0 && dy != 0) continue; // cardinal only
                        var nb = gm.GetTile(t.gridX + dx, t.gridY + dy);
                        if (nb != null) chainTargets.Add(nb);
                    }
            }
            foreach (var t in chainTargets)
                if (t != null && !t.IsFullyEmpty())
                    StartCoroutine(DoMerge(t, true));
        }
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
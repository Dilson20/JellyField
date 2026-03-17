using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance;
    public int GetMergePartner(int index) => mergePartner[index];

    void Awake() { Instance = this; }

    // Quadrants that touch when tiles are adjacent
    // Our tile's quadrants that face Right: Q1(1), Q3(3)
    // Neighbor's quadrants that face Left:  Q0(0), Q2(2)
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
        yield return new WaitForSeconds(0.15f); // small delay for drop anim

        GridManager gm = GridManager.Instance;
        bool anyMerge = false;

        // Check all 4 directions
        anyMerge |= TryMergeDirection(tile, gm, 1, 0, RightOurs, RightTheirs);
        anyMerge |= TryMergeDirection(tile, gm, -1, 0, LeftOurs, LeftTheirs);
        anyMerge |= TryMergeDirection(tile, gm, 0, 1, UpOurs, UpTheirs);
        anyMerge |= TryMergeDirection(tile, gm, 0, -1, DownOurs, DownTheirs);

        if (anyMerge)
        {
            // Check if tile is now empty after merging
            CheckAndDestroyEmpty(tile, gm);

            // Chain merge — check neighbors of neighbors
            yield return new WaitForSeconds(0.1f);
            // Could add chain logic here later
        }
    }

    bool TryMergeDirection(JellyTile tile, GridManager gm,
                           int dx, int dy,
                           int[] ourQuads, int[] theirQuads)
    {
        int nx = tile.gridX + dx;
        int ny = tile.gridY + dy;

        JellyTile neighbor = gm.GetTile(nx, ny);
        if (neighbor == null) return false;

        bool merged = false;

        for (int i = 0; i < ourQuads.Length; i++)
        {
            int oq = ourQuads[i];
            int tq = theirQuads[i];

            int ourColor = tile.quadrantColors[oq];
            int theirColor = neighbor.quadrantColors[tq];

            // Both active and same color
            if (ourColor >= 0 && theirColor >= 0 && ourColor == theirColor)
            {
                // Remove both matching quadrants
                tile.ClearQuadrant(oq);
                neighbor.ClearQuadrant(tq);
                merged = true;

                // Play effects
                tile.OnSwap();
                neighbor.OnSwap();
            }
        }

        if (merged)
            CheckAndDestroyEmpty(neighbor, gm);

        return merged;
    }

    void CheckAndDestroyEmpty(JellyTile tile, GridManager gm)
    {
        // If all quadrants are empty, remove the tile
        bool allEmpty = true;
        for (int i = 0; i < 4; i++)
        {
            if (tile.quadrantColors[i] >= 0)
            {
                allEmpty = false;
                break;
            }
        }

        if (allEmpty)
        {
            gm.RemoveTile(tile.gridX, tile.gridY);
            Destroy(tile.gameObject);
        }
    }
}
using UnityEngine;
using System.Collections;

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

        TryMergeDirection(tile, gm, 1, 0, RightOurs, RightTheirs);
        TryMergeDirection(tile, gm, -1, 0, LeftOurs, LeftTheirs);
        TryMergeDirection(tile, gm, 0, 1, UpOurs, UpTheirs);
        TryMergeDirection(tile, gm, 0, -1, DownOurs, DownTheirs);

        CheckAndDestroyEmpty(tile, gm);
    }

    void TryMergeDirection(JellyTile tile, GridManager gm,
                           int dx, int dy,
                           int[] ourQuads, int[] theirQuads)
    {
        int nx = tile.gridX + dx;
        int ny = tile.gridY + dy;

        JellyTile neighbor = gm.GetTile(nx, ny);
        if (neighbor == null) return;

        for (int i = 0; i < ourQuads.Length; i++)
        {
            int oq = ourQuads[i];
            int tq = theirQuads[i];

            int ourColor = tile.quadrantColors[oq];
            int theirColor = neighbor.quadrantColors[tq];

            if (ourColor >= 0 && theirColor >= 0 && ourColor == theirColor)
            {
                tile.ClearQuadrant(oq);
                neighbor.ClearQuadrant(tq);

                tile.OnSwap();
                neighbor.OnSwap();

                CheckAndDestroyEmpty(neighbor, gm);
            }
        }
    }

    void CheckAndDestroyEmpty(JellyTile tile, GridManager gm)
    {
        if (tile == null) return;
        if (!tile.IsFullyEmpty()) return;

        gm.RemoveTile(tile.gridX, tile.gridY);
        Destroy(tile.gameObject);
    }
}
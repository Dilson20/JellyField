using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Density")]
    [Range(0, 42)] public int minBlankTiles = 5;       // guaranteed empty slots
    [Range(0f, 1f)] public float jellyDensity = 0.8f;  // 1.0 = full, 0.0 = all blank

    [Header("Grid Settings")]
    public int columns = 6;
    public int rows = 7;
    public float tileSize = 0.9f;
    public float tileSpacing = 0.05f;
    public GameObject tilePrefab;
    public GameObject blankTilePrefab;

    private JellyTile[,] grid;
    private bool[,] isBlank;

    void Awake() { Instance = this; }

    void Start()
    {
        GenerateGrid();
        CenterCamera();
    }

    void GenerateGrid()
    {
        SpawnBackgroundTiles();

        int total = columns * rows;

        // Build a list of all cell positions and shuffle it
        var positions = new List<Vector2Int>();
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                positions.Add(new Vector2Int(x, y));

        Shuffle(positions);

        // Calculate how many tiles to actually fill
        int maxFromDensity = Mathf.RoundToInt(total * jellyDensity);
        int forcedBlanks = Mathf.Clamp(minBlankTiles, 0, total);
        int fillCount = Mathf.Clamp(maxFromDensity, 0, total - forcedBlanks);

        for (int i = 0; i < positions.Count; i++)
        {
            int x = positions[i].x;
            int y = positions[i].y;

            if (i < fillCount)
            {
                // Spawn a jelly tile
                Vector3 worldPos = GridToWorld(x, y);
                var go = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                var tile = go.GetComponent<JellyTile>();
                tile.Init(x, y);
                tile.isInteractable = false;
                tiles[x, y] = tile;
            }
            // else: leave the cell empty (blank background already placed)
        }
    }

    void Shuffle(List<Vector2Int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public bool WorldToGrid(Vector2 worldPos, out int gx, out int gy)
    {
        float totalWidth = columns * (tileSize + tileSpacing) - tileSpacing;
        float totalHeight = rows * (tileSize + tileSpacing) - tileSpacing;
        float startX = -totalWidth / 2f;
        float startY = -totalHeight / 2f;

        gx = Mathf.FloorToInt((worldPos.x - startX + (tileSize + tileSpacing) / 2f)
             / (tileSize + tileSpacing));
        gy = Mathf.FloorToInt((worldPos.y - startY + (tileSize + tileSpacing) / 2f)
             / (tileSize + tileSpacing));

        return gx >= 0 && gx < columns && gy >= 0 && gy < rows;
    }

    public Vector3 GridToWorld(int gx, int gy)
    {
        float totalWidth = columns * (tileSize + tileSpacing) - tileSpacing;
        float totalHeight = rows * (tileSize + tileSpacing) - tileSpacing;
        float startX = -totalWidth / 2f + tileSize / 2f;
        float startY = -totalHeight / 2f + tileSize / 2f;

        return new Vector3(
            startX + gx * (tileSize + tileSpacing),
            startY + gy * (tileSize + tileSpacing),
            0);
    }

    public void TryMoveToPosition(JellyTile tile, Vector2 releaseWorldPos)
    {
        int ox = tile.gridX;
        int oy = tile.gridY;
        bool fromHand = (ox < 0 || oy < 0);

        int bestX = -1, bestY = -1;
        float bestDist = float.MaxValue;
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                if (!isBlank[x, y]) continue;
                Vector3 cw = GridToWorld(x, y);
                float d = Vector2.Distance(releaseWorldPos, new Vector2(cw.x, cw.y));
                if (d < bestDist) { bestDist = d; bestX = x; bestY = y; }
            }

        if (bestX != -1 && bestDist < tileSize + tileSpacing)
        {
            if (!fromHand) { grid[ox, oy] = null; isBlank[ox, oy] = true; }

            isBlank[bestX, bestY] = false;
            grid[bestX, bestY] = tile;
            tile.gridX = bestX;
            tile.gridY = bestY;
            tile.transform.position = GridToWorld(bestX, bestY);

            if (fromHand)
            {
                int prevSlot = tile.handSlotIndex;
                tile.isInteractable = false;
                tile.handSlotIndex = -1;
                HandManager.Instance?.OnHandTilePlaced(prevSlot);
            }

            tile.OnDrop();
            MergeManager.Instance.CheckMerges(tile);
        }
        else
        {
            // Snap back — hand tiles return to tray, grid tiles return to grid
            tile.transform.position = (fromHand && HandManager.Instance != null)
                ? HandManager.Instance.GetSlotPosition(tile.handSlotIndex)
                : GridToWorld(ox, oy);
            tile.OnDrop();
            if (!fromHand) MergeManager.Instance.CheckMerges(tile);
        }
    }

    void SpawnBackgroundTiles()
    {
        if (blankTilePrefab == null) return;
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 pos = GridToWorld(x, y);
                pos.z = 0.5f; // render behind jelly tiles
                var bg = Instantiate(blankTilePrefab, pos, Quaternion.identity, transform);
                bg.name = $"BG_{x}_{y}";
                bg.transform.localScale = new Vector3(tileSize, tileSize, 1f);
            }
        }
    }

    public JellyTile GetTile(int x, int y)
    {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return null;
        return grid[x, y];
    }

    public void RemoveTile(int x, int y)
    {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return;
        grid[x, y] = null;
        isBlank[x, y] = true;
    }

    void CenterCamera()
    {
        float totalHeight = rows * (tileSize + tileSpacing) - tileSpacing;
        Camera.main.orthographicSize = (totalHeight / 2f) + 1.2f;
    }
}
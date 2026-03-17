using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

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
        grid = new JellyTile[columns, rows];
        isBlank = new bool[columns, rows];

        float totalWidth = columns * (tileSize + tileSpacing) - tileSpacing;
        float totalHeight = rows * (tileSize + tileSpacing) - tileSpacing;
        float startX = -totalWidth / 2f + tileSize / 2f;
        float startY = -totalHeight / 2f + tileSize / 2f;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                float posX = startX + x * (tileSize + tileSpacing);
                float posY = startY + y * (tileSize + tileSpacing);

                bool blank = Random.value < 0.15f;
                isBlank[x, y] = blank;

                if (blank)
                {
                    if (blankTilePrefab != null)
                    {
                        GameObject b = Instantiate(blankTilePrefab,
                            new Vector3(posX, posY, 0), Quaternion.identity);
                        b.transform.SetParent(this.transform);
                        b.name = $"Blank_{x}_{y}";
                    }
                    grid[x, y] = null;
                }
                else
                {
                    GameObject obj = Instantiate(tilePrefab,
                        new Vector3(posX, posY, 0), Quaternion.identity);
                    obj.transform.SetParent(this.transform);
                    obj.name = $"Tile_{x}_{y}";

                    JellyTile tile = obj.GetComponent<JellyTile>();
                    tile.Init(x, y);
                    grid[x, y] = tile;
                }
            }
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

        // Find the nearest blank cell to drop position
        int bestX = -1, bestY = -1;
        float bestDist = float.MaxValue;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (!isBlank[x, y]) continue;

                Vector3 cellWorld = GridToWorld(x, y);
                float dist = Vector2.Distance(releaseWorldPos,
                             new Vector2(cellWorld.x, cellWorld.y));

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestX = x;
                    bestY = y;
                }
            }
        }

        // Only snap if close enough (within 1 tile distance)
        float snapThreshold = tileSize + tileSpacing;

        if (bestX != -1 && bestDist < snapThreshold)
        {
            grid[ox, oy] = null;
            isBlank[ox, oy] = true;
            isBlank[bestX, bestY] = false;
            grid[bestX, bestY] = tile;

            tile.gridX = bestX;
            tile.gridY = bestY;
            tile.transform.position = GridToWorld(bestX, bestY);
            tile.OnDrop();
            MergeManager.Instance.CheckMerges(tile);
        }
        else
        {
            // Snap back
            tile.transform.position = GridToWorld(ox, oy);
            tile.OnDrop();
            MergeManager.Instance.CheckMerges(tile);
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
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

        gx = Mathf.RoundToInt((worldPos.x - startX) / (tileSize + tileSpacing));
        gy = Mathf.RoundToInt((worldPos.y - startY) / (tileSize + tileSpacing));

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
        if (!WorldToGrid(releaseWorldPos, out int tx, out int ty))
        {
            tile.transform.position = GridToWorld(tile.gridX, tile.gridY);
            tile.OnDrop();
            return;
        }

        int ox = tile.gridX;
        int oy = tile.gridY;

        if (tx == ox && ty == oy)
        {
            tile.transform.position = GridToWorld(ox, oy);
            tile.OnDrop();
            return;
        }

        JellyTile target = grid[tx, ty];

        if (isBlank[tx, ty])
        {
            grid[ox, oy] = null;
            isBlank[ox, oy] = true;
            isBlank[tx, ty] = false;
            grid[tx, ty] = tile;

            tile.gridX = tx;
            tile.gridY = ty;
            tile.transform.position = GridToWorld(tx, ty);
            tile.OnDrop();
        }
        else if (target != null)
        {
            grid[ox, oy] = target;
            grid[tx, ty] = tile;

            target.gridX = ox; target.gridY = oy;
            tile.gridX = tx; tile.gridY = ty;

            target.transform.position = GridToWorld(ox, oy);
            tile.transform.position = GridToWorld(tx, ty);

            tile.OnDrop();
            target.OnSwap();
        }
        else
        {
            tile.transform.position = GridToWorld(ox, oy);
            tile.OnDrop();
        }
    }

    void CenterCamera()
    {
        float totalHeight = rows * (tileSize + tileSpacing) - tileSpacing;
        Camera.main.orthographicSize = (totalHeight / 2f) + 1.2f;
    }
}
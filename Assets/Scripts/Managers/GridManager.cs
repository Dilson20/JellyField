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

    private JellyTile[,] grid;

    void Awake() { Instance = this; }

    void Start()
    {
        GenerateGrid();
        CenterCamera();
    }

    void GenerateGrid()
    {
        grid = new JellyTile[columns, rows];

        float totalWidth = columns * (tileSize + tileSpacing) - tileSpacing;
        float totalHeight = rows * (tileSize + tileSpacing) - tileSpacing;
        float startX = -totalWidth / 2f + tileSize / 2f;
        float startY = -totalHeight / 2f + tileSize / 2f;

        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                float posX = startX + x * (tileSize + tileSpacing);
                float posY = startY + y * (tileSize + tileSpacing);

                GameObject obj = Instantiate(tilePrefab,
                    new Vector3(posX, posY, 0), Quaternion.identity);
                obj.transform.SetParent(this.transform);
                obj.name = $"Tile_{x}_{y}";

                JellyTile tile = obj.GetComponent<JellyTile>();
                tile.Init(x, y);
                grid[x, y] = tile;
            }
    }

    public void TrySwap(JellyTile tile, Direction dir)
    {
        int nx = tile.gridX + (dir == Direction.Right ? 1 : dir == Direction.Left ? -1 : 0);
        int ny = tile.gridY + (dir == Direction.Up ? 1 : dir == Direction.Down ? -1 : 0);

        if (nx < 0 || nx >= columns || ny < 0 || ny >= rows) return;

        JellyTile neighbor = grid[nx, ny];
        if (neighbor == null) return;

        SwapTiles(tile, neighbor);
    }

    void SwapTiles(JellyTile a, JellyTile b)
    {
        grid[a.gridX, a.gridY] = b;
        grid[b.gridX, b.gridY] = a;

        Vector3 tempPos = a.transform.position;
        a.transform.position = b.transform.position;
        b.transform.position = tempPos;

        int tempX = a.gridX; int tempY = a.gridY;
        a.gridX = b.gridX; a.gridY = b.gridY;
        b.gridX = tempX; b.gridY = tempY;
    }

    void CenterCamera()
    {
        float totalHeight = rows * (tileSize + tileSpacing) - tileSpacing;
        Camera.main.orthographicSize = (totalHeight / 2f) + 1.2f;
    }
}
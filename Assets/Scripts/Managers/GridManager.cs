using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int columns = 6;
    public int rows = 7;
    public float tileSize = 0.9f;
    public float tileSpacing = 0.05f;
    public GameObject tilePrefab;

    private JellyTile[,] grid;

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
        {
            for (int y = 0; y < rows; y++)
            {
                float posX = startX + x * (tileSize + tileSpacing);
                float posY = startY + y * (tileSize + tileSpacing);

                GameObject obj = Instantiate(tilePrefab,
                    new Vector3(posX, posY, 0), Quaternion.identity);
                obj.transform.SetParent(this.transform);
                obj.name = $"Tile_{x}_{y}";

                int randomColor = Random.Range(0, JellyTile.JellyColors.Length);
                JellyTile tile = obj.GetComponent<JellyTile>();
                tile.Init(x, y, randomColor);

                grid[x, y] = tile;
            }
        }
    }

    void CenterCamera()
    {
        float totalHeight = rows * (tileSize + tileSpacing) - tileSpacing;
        // Adjust camera size to fit grid nicely with padding
        Camera.main.orthographicSize = (totalHeight / 2f) + 1.2f;
    }
}
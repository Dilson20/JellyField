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

    [Header("Grid Position")]
    [Tooltip("Offset the entire grid vertically. Positive = up, Negative = down.")]
    public float gridOffsetY = 0f;

    [Header("Side Extensions")]
    public bool extendLeft;
    public bool extendRight;
    public bool extendTop;
    public bool extendBottom;

    [Header("Middle-Only Sides")]
    [Tooltip("Left column only spawns tile(s) in the middle row(s).")]
    public bool middleLeft = false;
    [Tooltip("Right column only spawns tile(s) in the middle row(s).")]
    public bool middleRight = false;
    [Tooltip("Top row only spawns tile(s) in the middle column(s).")]
    public bool middleTop = false;
    [Tooltip("Bottom row only spawns tile(s) in the middle column(s).")]
    public bool middleBottom = false;

    private JellyTile[,] grid;
    private bool[,] isBlank;
    private HashSet<Vector2Int> restrictedCells = new HashSet<Vector2Int>();

    // Extension cells live outside [0,columns) x [0,rows) bounds
    private HashSet<Vector2Int> blankExtensions = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, JellyTile> extensionTiles = new Dictionary<Vector2Int, JellyTile>();

    void Awake() { Instance = this; }

    void Start()
    {
        FitTileSizeToScreen();
        GenerateGrid();
    }

    // Tự tính tileSize để grid vừa khít màn hình theo aspect ratio
    void FitTileSizeToScreen()
    {
        float aspect = Screen.width / (float)Screen.height;
        float fill = 0.92f;

        // Ước tính số row tương đương mà camera cần hiển thị
        // (grid rows + ~2 rows cho hand area + padding)
        float totalRows = rows + 2f;

        // Từ HandManager.AdjustCamera: orthoSize ≈ totalRows*step/2 + C
        // worldWidth = 2 * orthoSize * aspect
        // Giải ra step để columns*step = worldWidth * fill:
        // columns * step = (totalRows * step + C) * aspect * fill
        // step * (columns - totalRows * aspect * fill) = C * aspect * fill
        // step = C * aspect * fill / (columns - totalRows * aspect * fill)
        const float C = 3.3f; // ≈ topUIPadding + extraPadding + margins quy đổi sang world units

        float denom = columns - totalRows * aspect * fill;
        if (denom <= 0.1f)
            return; // màn hình quá rộng (landscape) — giữ nguyên giá trị Inspector

        float step = C * aspect * fill / denom;
        tileSize = Mathf.Clamp(step - tileSpacing, 0.3f, 3f);

        // Scale tileSpacing theo tileSize để giữ tỉ lệ gap nhất quán
        tileSpacing = tileSize * 0.055f;
    }

    IEnumerable<int> MiddleIndices(int count)
    {
        if (count % 2 == 1) { yield return count / 2; }
        else { yield return count / 2 - 1; yield return count / 2; }
    }

    void BuildRestrictedCells()
    {
        restrictedCells.Clear();
        if (middleLeft && columns >= 1)
        {
            var mid = new HashSet<int>(MiddleIndices(rows));
            for (int y = 0; y < rows; y++)
                if (!mid.Contains(y)) restrictedCells.Add(new Vector2Int(0, y));
        }
        if (middleRight && columns >= 2)
        {
            var mid = new HashSet<int>(MiddleIndices(rows));
            for (int y = 0; y < rows; y++)
                if (!mid.Contains(y)) restrictedCells.Add(new Vector2Int(columns - 1, y));
        }
        if (middleTop && rows >= 2)
        {
            var mid = new HashSet<int>(MiddleIndices(columns));
            for (int x = 0; x < columns; x++)
                if (!mid.Contains(x)) restrictedCells.Add(new Vector2Int(x, rows - 1));
        }
        if (middleBottom && rows >= 1)
        {
            var mid = new HashSet<int>(MiddleIndices(columns));
            for (int x = 0; x < columns; x++)
                if (!mid.Contains(x)) restrictedCells.Add(new Vector2Int(x, 0));
        }
    }

    List<Vector2Int> GetExtensionCells()
    {
        var cells = new List<Vector2Int>();
        if (extendLeft)   foreach (int y in MiddleIndices(rows))    cells.Add(new Vector2Int(-1, y));
        if (extendRight)  foreach (int y in MiddleIndices(rows))    cells.Add(new Vector2Int(columns, y));
        if (extendBottom) foreach (int x in MiddleIndices(columns)) cells.Add(new Vector2Int(x, -1));
        if (extendTop)    foreach (int x in MiddleIndices(columns)) cells.Add(new Vector2Int(x, rows));
        return cells;
    }

    bool IsExtension(int x, int y) => x < 0 || x >= columns || y < 0 || y >= rows;

    void GenerateGrid()
    {
        grid = new JellyTile[columns, rows];
        isBlank = new bool[columns, rows];
        blankExtensions.Clear();
        extensionTiles.Clear();

        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                isBlank[x, y] = true;

        foreach (var ext in GetExtensionCells())
            blankExtensions.Add(ext);

        BuildRestrictedCells();
        SpawnBackgroundTiles();

        // Build spawnable positions (exclude restricted cells)
        var positions = new List<Vector2Int>();
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                if (!restrictedCells.Contains(new Vector2Int(x, y)))
                    positions.Add(new Vector2Int(x, y));

        Shuffle(positions);

        int spawnableCount = positions.Count;
        int maxFromDensity = Mathf.RoundToInt(spawnableCount * jellyDensity);
        int forcedBlanks = Mathf.Clamp(minBlankTiles, 0, spawnableCount);
        int fillCount = Mathf.Clamp(maxFromDensity, 0, spawnableCount - forcedBlanks);

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
                tile.SetTileScale(tileSize);
                tile.Init(x, y);
                tile.isInteractable = false;
                grid[x, y] = tile;
                isBlank[x, y] = false;
            }
            // else: leave the cell empty (blank background already placed)
        }

        BreakInitialMatches();
    }

    void BreakInitialMatches()
    {
        bool anyMatch = true;
        int passes = 0;
        while (anyMatch && passes < 10)
        {
            anyMatch = false;
            passes++;
            for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y] == null) continue;
                if (TileHasMatchWithNeighbor(x, y))
                {
                    grid[x, y].Init(x, y);
                    anyMatch = true;
                }
            }
        }
    }

    bool TileHasMatchWithNeighbor(int x, int y)
    {
        var tile = grid[x, y];
        var right = (x + 1 < columns) ? grid[x + 1, y] : null;
        if (right != null)
        {
            if (ColorMatch(tile.quadrantColors[1], right.quadrantColors[0])) return true;
            if (ColorMatch(tile.quadrantColors[3], right.quadrantColors[2])) return true;
        }
        var left = (x > 0) ? grid[x - 1, y] : null;
        if (left != null)
        {
            if (ColorMatch(tile.quadrantColors[0], left.quadrantColors[1])) return true;
            if (ColorMatch(tile.quadrantColors[2], left.quadrantColors[3])) return true;
        }
        var up = (y + 1 < rows) ? grid[x, y + 1] : null;
        if (up != null)
        {
            if (ColorMatch(tile.quadrantColors[0], up.quadrantColors[2])) return true;
            if (ColorMatch(tile.quadrantColors[1], up.quadrantColors[3])) return true;
        }
        var down = (y > 0) ? grid[x, y - 1] : null;
        if (down != null)
        {
            if (ColorMatch(tile.quadrantColors[2], down.quadrantColors[0])) return true;
            if (ColorMatch(tile.quadrantColors[3], down.quadrantColors[1])) return true;
        }
        return false;
    }

    bool ColorMatch(int a, int b) => a >= 0 && b >= 0 && a == b;

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
        gy = Mathf.FloorToInt((worldPos.y - gridOffsetY - startY + (tileSize + tileSpacing) / 2f)
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
            startY + gy * (tileSize + tileSpacing) + gridOffsetY,
            0);
    }

    public void TryMoveToPosition(JellyTile tile, Vector2 releaseWorldPos)
    {
        int ox = tile.gridX;
        int oy = tile.gridY;
        bool fromHand = (ox < 0 || oy < 0);

        int bestX = int.MinValue, bestY = int.MinValue;
        float bestDist = float.MaxValue;

        // Check main grid cells
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                if (!isBlank[x, y]) continue;
                if (restrictedCells.Contains(new Vector2Int(x, y))) continue;
                Vector3 cw = GridToWorld(x, y);
                float d = Vector2.Distance(releaseWorldPos, new Vector2(cw.x, cw.y));
                if (d < bestDist) { bestDist = d; bestX = x; bestY = y; }
            }

        // Check extension cells
        foreach (var ext in blankExtensions)
        {
            Vector3 cw = GridToWorld(ext.x, ext.y);
            float d = Vector2.Distance(releaseWorldPos, new Vector2(cw.x, cw.y));
            if (d < bestDist) { bestDist = d; bestX = ext.x; bestY = ext.y; }
        }

        if (bestX != int.MinValue && bestDist < tileSize + tileSpacing)
        {
            // Clear old position
            if (!fromHand)
            {
                if (IsExtension(ox, oy)) { extensionTiles.Remove(new Vector2Int(ox, oy)); blankExtensions.Add(new Vector2Int(ox, oy)); }
                else { grid[ox, oy] = null; isBlank[ox, oy] = true; }
            }

            // Place at new position
            if (IsExtension(bestX, bestY))
            {
                blankExtensions.Remove(new Vector2Int(bestX, bestY));
                extensionTiles[new Vector2Int(bestX, bestY)] = tile;
            }
            else
            {
                isBlank[bestX, bestY] = false;
                grid[bestX, bestY] = tile;
            }

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
                if (restrictedCells.Contains(new Vector2Int(x, y))) continue;
                Vector3 pos = GridToWorld(x, y);
                pos.z = 0.5f;
                var bg = Instantiate(blankTilePrefab, pos, Quaternion.identity, transform);
                bg.name = $"BG_{x}_{y}";
                bg.transform.localScale = new Vector3(tileSize, tileSize, 1f);
            }
        }

        // Spawn background tiles for extension cells
        foreach (var ext in GetExtensionCells())
        {
            Vector3 pos = GridToWorld(ext.x, ext.y);
            pos.z = 0.5f;
            var bg = Instantiate(blankTilePrefab, pos, Quaternion.identity, transform);
            bg.name = $"BG_EXT_{ext.x}_{ext.y}";
            bg.transform.localScale = new Vector3(tileSize, tileSize, 1f);
        }
    }

    public JellyTile GetTile(int x, int y)
    {
        if (IsExtension(x, y))
        {
            extensionTiles.TryGetValue(new Vector2Int(x, y), out var ext);
            return ext;
        }
        return grid[x, y];
    }

    public bool IsGridFull()
    {
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                if (restrictedCells.Contains(new Vector2Int(x, y))) continue;
                if (isBlank[x, y]) return false;
            }
        if (blankExtensions.Count > 0) return false;
        return true;
    }

    public bool HasAnyMergePossible()
    {
        // Horizontal pairs
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                var a = GetTile(x, y);
                var b = GetTile(x + 1, y);
                if (a != null && b != null)
                {
                    if (a.quadrantColors[1] >= 0 && a.quadrantColors[1] == b.quadrantColors[0]) return true;
                    if (a.quadrantColors[3] >= 0 && a.quadrantColors[3] == b.quadrantColors[2]) return true;
                }
                // Vertical pairs
                var c = GetTile(x, y + 1);
                if (a != null && c != null)
                {
                    if (a.quadrantColors[0] >= 0 && a.quadrantColors[0] == c.quadrantColors[2]) return true;
                    if (a.quadrantColors[1] >= 0 && a.quadrantColors[1] == c.quadrantColors[3]) return true;
                }
            }
        return false;
    }

    public void RemoveTile(int x, int y)
    {
        if (IsExtension(x, y))
        {
            var key = new Vector2Int(x, y);
            extensionTiles.Remove(key);
            if (blankExtensions.Contains(key) == false && GetExtensionCells().Contains(key))
                blankExtensions.Add(key);
            return;
        }
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
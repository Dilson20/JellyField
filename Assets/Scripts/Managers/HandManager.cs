using System.Collections;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance;

    [Header("Setup")]
    public int handSize = 5;
    public GameObject tilePrefab;

    [Header("Layout")]
    public float extraPaddingBelow = 0.4f;

    private JellyTile[] slots;
    private Vector3[] slotPositions;

    void Awake() { Instance = this; }

    void Start()
    {
        BuildHand();
        AdjustCamera();
    }

    void BuildHand()
    {
        GridManager gm = GridManager.Instance;
        float step = gm.tileSize + gm.tileSpacing;

        float gridBottomY = -(gm.rows * step - gm.tileSpacing) / 2f;
        float handY = gridBottomY - step - extraPaddingBelow;

        float totalW = handSize * step - gm.tileSpacing;
        float startX = -totalW / 2f + gm.tileSize / 2f;

        slots = new JellyTile[handSize];
        slotPositions = new Vector3[handSize];

        for (int i = 0; i < handSize; i++)
        {
            slotPositions[i] = new Vector3(startX + i * step, handY, 0);
            SpawnSlot(i);
        }

        CreatePanel(new Vector3(0, handY, 0.1f), totalW + step * 0.4f, gm.tileSize + step * 0.35f);
    }

    void SpawnSlot(int i)
    {
        var go = Instantiate(tilePrefab, slotPositions[i], Quaternion.identity);
        go.transform.SetParent(transform);
        go.name = $"HandTile_{i}";
        var tile = go.GetComponent<JellyTile>();
        tile.Init(-1, -1);
        tile.isInteractable = true;
        tile.handSlotIndex = i;
        slots[i] = tile;
    }

    void CreatePanel(Vector3 center, float width, float height)
    {
        var panel = new GameObject("HandPanel");
        panel.transform.SetParent(transform);
        panel.transform.position = center;
        panel.transform.localScale = new Vector3(width, height, 1f);

        var sr = panel.AddComponent<SpriteRenderer>();
        sr.sprite = tilePrefab?.GetComponentInChildren<SpriteRenderer>()?.sprite;
        sr.color = new Color(0.10f, 0.11f, 0.20f, 1f);
        sr.sortingOrder = -2;
    }

    void AdjustCamera()
    {
        GridManager gm = GridManager.Instance;
        float step = gm.tileSize + gm.tileSpacing;
        float gridTop = (gm.rows * step - gm.tileSpacing) / 2f;
        float handBottom = slotPositions[0].y - gm.tileSize / 2f - 0.4f;

        float span = gridTop - handBottom;
        float centerY = (gridTop + handBottom) / 2f;

        var cam = Camera.main;
        cam.orthographicSize = span / 2f + 0.5f;
        cam.transform.position = new Vector3(0, centerY, -10f);
    }

    public Vector3 GetSlotPosition(int i) =>
        i >= 0 && i < slotPositions.Length ? slotPositions[i] : Vector3.zero;

    public void OnHandTilePlaced(int i)
    {
        if (i < 0 || i >= slots.Length) return;
        slots[i] = null;
        StartCoroutine(Refill(i));
    }

    IEnumerator Refill(int i)
    {
        yield return new WaitForSeconds(0.35f / JellyTile.AnimSpeed);
        if (this != null && gameObject != null) SpawnSlot(i);
    }
}
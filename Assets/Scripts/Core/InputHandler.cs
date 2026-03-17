using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    private Camera mainCam;
    private JellyTile selectedTile;
    private Vector2 dragStartPos;
    private Vector3 tileOriginalPos;
    private bool isDragging;

    private float dragLift = 0.1f; // scale up slightly when dragging
    private Vector3 originalScale;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Touchscreen touch = Touchscreen.current;

        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
                OnPointerDown(mouse.position.ReadValue());
            else if (mouse.leftButton.wasReleasedThisFrame)
                OnPointerUp(mouse.position.ReadValue());
            else if (mouse.leftButton.isPressed && isDragging)
                OnPointerDrag(mouse.position.ReadValue());
        }

        if (touch != null)
        {
            var t = touch.primaryTouch;
            if (t.press.wasPressedThisFrame)
                OnPointerDown(t.position.ReadValue());
            else if (t.press.wasReleasedThisFrame)
                OnPointerUp(t.position.ReadValue());
            else if (t.press.isPressed && isDragging)
                OnPointerDrag(t.position.ReadValue());
        }
    }

    void OnPointerDown(Vector2 screenPos)
    {
        Vector2 worldPos = mainCam.ScreenToWorldPoint(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider != null)
        {
            JellyTile tile = hit.collider.GetComponent<JellyTile>();
            if (tile != null)
            {
                selectedTile = tile;
                dragStartPos = worldPos;
                tileOriginalPos = tile.transform.position;
                originalScale = tile.transform.localScale;
                isDragging = true;

                // Lift tile above others
                selectedTile.transform.localScale = originalScale * 1.1f;
                SetSortingOrder(selectedTile, 10);
                selectedTile.OnPickup();
            }
        }
    }

    void OnPointerDrag(Vector2 screenPos)
    {
        if (selectedTile == null) return;
        Vector2 worldPos = mainCam.ScreenToWorldPoint(screenPos);

        // Smoothly follow finger
        selectedTile.transform.position = Vector3.Lerp(
            selectedTile.transform.position,
            new Vector3(worldPos.x, worldPos.y, 0),
            0.3f
        );
    }

    void OnPointerUp(Vector2 screenPos)
    {
        if (!isDragging || selectedTile == null) { ResetInput(); return; }

        Vector2 worldPos = mainCam.ScreenToWorldPoint(screenPos);

        selectedTile.transform.localScale = originalScale;
        SetSortingOrder(selectedTile, 0);

        GridManager.Instance.TryMoveToPosition(selectedTile, worldPos);

        StartCoroutine(ResumeIdle(selectedTile));
        ResetInput();
    }

    void AnimateToPosition(JellyTile tile, Vector3 target)
    {
        // Simple immediate snap — we'll add tweening in jiggle phase
        tile.transform.position = target;
    }

    void SetSortingOrder(JellyTile tile, int order)
    {
        foreach (var sr in tile.GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = order;
    }

    void ResetInput()
    {
        selectedTile = null;
        isDragging = false;
    }
}

public enum Direction { Up, Down, Left, Right }
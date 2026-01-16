using System.Collections.Generic;
using UnityEngine;
// Không cần using UnityEngine.EventSystems nữa

public class DragDropPlacedDecor : MonoBehaviour
{
    private Camera mainCam;
    private Vector3 originalPos;
    private SpriteRenderer sr;
    private GameObject highlight;
    private SpriteRenderer highlightSR;
    private Collider2D decorCollider;
    private bool isDragging = false;

    [Tooltip("Đánh dấu xem Decor này có phải là Decor gốc không (Không lưu, không xóa)")]
    public bool OriginalDecor;

    private void Start()
    {
        mainCam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
        decorCollider = GetComponent<Collider2D>();

        highlight = new GameObject("Highlight_Move");
        highlightSR = highlight.AddComponent<SpriteRenderer>();
        highlightSR.sprite = sr.sprite;
        highlightSR.sortingOrder = sr.sortingOrder + 1;
        highlightSR.color = new Color(1, 1, 1, 0);
    }

    private void OnMouseDown()
    {
        Debug.Log("🎯 Bắt đầu kéo Decor đã đặt (OnMouseDown).");
        originalPos = transform.position;
        sr.color = new Color(1, 1, 1, 0.5f);
        isDragging = true;
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;

        transform.position = worldPos;
        highlight.transform.position = worldPos;

        bool canMove = CheckCanPlace(worldPos);
        highlightSR.color = canMove ? new Color(0, 1, 0, 0.35f) : new Color(1, 0, 0, 0.35f);
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;
        bool canMove = CheckCanPlace(worldPos);

        if (!canMove)
        {
            transform.position = originalPos;
            Debug.Log("⚠️ Không thể di chuyển Decor tới vị trí này.");
        }
        else
        {
            Debug.Log("✅ Di chuyển Decor thành công.");
        }

        sr.color = Color.white;
        highlightSR.color = new Color(1, 1, 1, 0);
    }

    private bool CheckCanPlace(Vector3 pos)
    {
        if (decorCollider == null) return false;

        ContactFilter2D filter = new ContactFilter2D().NoFilter();
        List<Collider2D> hits = new List<Collider2D>();

        decorCollider.Overlap(filter, hits);

        if (hits.Count >= 2) return false;

        bool hasSoil = false;
        bool hasBackground = false;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Soil")) hasSoil = true;
            if (hit.CompareTag("Background")) hasBackground = true;
        }

        return hasBackground && !hasSoil;
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragDropItems : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public List<Canvas> allCanvasToHide;
    public BagSlotUI slotUI;

    private GameObject ghostItem;
    private GameObject highlight;
    private SpriteRenderer highlightSR;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
        allCanvasToHide = new List<Canvas>(FindObjectsByType<Canvas>(FindObjectsSortMode.None));

        highlight = new GameObject("Highlight");
        highlightSR = highlight.AddComponent<SpriteRenderer>();
        highlightSR.sortingOrder = 1000;
        highlightSR.color = new Color(1, 1, 1, 0);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slotUI == null || string.IsNullOrEmpty(slotUI.itemId)) return;

        foreach (var canvas in allCanvasToHide)
            if (canvas != null)
            {
                // [SỬA LỖI A] - Ngoại trừ Tutorial Canvas ra, đừng tắt nó!
                // Giả sử Canvas chứa tutorial của bạn tên là "TutorialCanvas"
                if (canvas.gameObject.name == "TutorialCanvas") continue;

                canvas.enabled = false;
            }

        // [THÊM MỚI] - Ngay khi nắm hạt giống, dời lỗ thủng ra ô đất để soi sáng chỗ trồng
        if (FarmingTutorialController.IsTutorialMode)
        {
            FarmingTutorialController.Instance.FocusOnSoilDuringDrag();
        }

        GameObject prefab = FindPrefabByItemId(slotUI.itemId);
        if (prefab == null)
        {
            ShowAllCanvas();
            return;
        }

        ghostItem = Instantiate(prefab);
        var col = ghostItem.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        var sr = ghostItem.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = new Color(1, 1, 1, 0.5f);

        highlightSR.sprite = sr != null ? sr.sprite : null;
        highlightSR.color = new Color(1, 1, 1, 0);

        UpdateGhostAndHighlight();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostItem != null)
            UpdateGhostAndHighlight();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghostItem == null)
        {
            ShowAllCanvas();
            highlightSR.color = new Color(1, 1, 1, 0);
            return;
        }

        Vector3 mouseWorldPos = GetMouseWorldPos();
        var (snapPos, targetParent, canPlace) = GetSnapPositionAndParent(mouseWorldPos, slotUI.itemType);

        if (canPlace)
        {
            PlaceItem(snapPos, targetParent);
        }

        Destroy(ghostItem);
        highlightSR.color = new Color(1, 1, 1, 0);
        ShowAllCanvas();
    }

    private void UpdateGhostAndHighlight()
    {
        Vector3 mouseWorldPos = GetMouseWorldPos();
        var (snapPos, _, canPlace) = GetSnapPositionAndParent(mouseWorldPos, slotUI.itemType);

        ghostItem.transform.position = snapPos;
        highlight.transform.position = snapPos;
        highlightSR.color = canPlace ? new Color(0, 1, 0, 0.35f) : new Color(1, 0, 0, 0.35f);
    }

    private (Vector3 position, Transform parent, bool canPlace) GetSnapPositionAndParent(Vector3 inputPos, string itemType)
    {
        if (itemType == "Seed")
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(inputPos, 0.5f);
            Collider2D closestSoil = null;
            float minDist = float.MaxValue;

            foreach (var hit in nearby)
            {
                if (hit != null && hit.CompareTag("Soil"))
                {
                    float dist = Vector2.Distance(inputPos, hit.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestSoil = hit;
                    }
                }
            }

            if (closestSoil != null)
            {
                // Kiểm tra xem Soil đã có cây chưa
                Collider2D[] onTopSoil = Physics2D.OverlapPointAll(closestSoil.transform.position);
                if (onTopSoil.Length > 2)
                    return (closestSoil.transform.position, closestSoil.transform, false);

                return (closestSoil.transform.position, closestSoil.transform, true);
            }
            return (inputPos, null, false);
        }
        else if (itemType == "Decor")
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(inputPos);
            bool hasSoil = false, hasBackground = false;

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Soil")) hasSoil = true;
                if (hit.CompareTag("Background")) hasBackground = true;
            }

            bool valid = hasBackground && !hasSoil && hits.Length < 2;
            return (inputPos, null, valid);
        }

        return (inputPos, null, false);
    }

    private void PlaceItem(Vector3 pos, Transform parent)
    {
        GameObject prefab = FindPrefabByItemId(slotUI.itemId);
        if (prefab == null) return;

        GameObject placedItem = Instantiate(prefab, pos, Quaternion.identity);

        if (parent != null)
        {
            placedItem.transform.SetParent(parent);
        }

        var sr = placedItem.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;

        if (slotUI.itemType == "Seed")
        {
            int plantLayer = LayerMask.NameToLayer("Plant");
            if (plantLayer != -1) placedItem.layer = plantLayer;
        }
        else
        {
            int placedItemLayer = LayerMask.NameToLayer("PlacedItem");
            if (placedItemLayer != -1) placedItem.layer = placedItemLayer;
        }

        if (slotUI.itemType == "Decor")
        {
            Collider2D col = placedItem.GetComponent<Collider2D>();
            if (col == null)
            {
                col = placedItem.AddComponent<BoxCollider2D>();
                if (sr != null && sr.sprite != null)
                    ((BoxCollider2D)col).size = sr.sprite.bounds.size;
            }
            col.isTrigger = false;
            col.enabled = true;

            if (placedItem.GetComponent<DragDropPlacedDecor>() == null)
                placedItem.AddComponent<DragDropPlacedDecor>();
        }

        //CHẶN VIỆC TRỪ ĐỒ NẾU ĐANG LÀ TUTORIAL
        if (FarmingTutorialController.IsTutorialMode)
        {
            Debug.Log("🌱 Gieo hạt trong Tutorial: Không trừ số lượng hạt giống!");
            FarmingTutorialController.isSeedPlanted = true;

            return; // Dừng hàm lại tại đây, không cho chạy xuống dòng ReduceQuantity
        }

        // Nếu không phải Tutorial thì trừ đồ bình thường
        slotUI.ReduceQuantity(1);
    }

    private void ShowAllCanvas()
    {
        foreach (var canvas in allCanvasToHide)
            if (canvas != null) canvas.enabled = true;
    }

    private GameObject FindPrefabByItemId(string id)
    {
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");
        foreach (var prefab in allPrefabs)
        {
            var item = prefab.GetComponent<ItemClass>();
            if (item != null && item.itemId == id)
                return prefab;
        }
        return null;
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 p = mainCam.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0;
        return p;
    }
}
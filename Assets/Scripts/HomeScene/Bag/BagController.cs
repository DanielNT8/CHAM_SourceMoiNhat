using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LoadBag;

public class BagController : MonoBehaviour
{
    [Header("Prefabs & UI References")]
    public GameObject slotPrefab;
    public Transform slotParent;
    public LoadBag bagService; // Gán script LoadBag trong Inspector

    [Header("Config")]
    public int slotCount = 50;

    private List<GameObject> slots = new List<GameObject>();
    private bool dataLoaded = false;
    private bool isReady = false;

    // 🕒 Danh sách item mua khi bag chưa load
    private List<(string itemId, int quantity)> pendingItems = new List<(string, int)>();

    void Start()
    {
        GenerateSlots();
        StartCoroutine(InitBag());
    }

    private IEnumerator InitBag()
    {
        if (!dataLoaded)
        {
            dataLoaded = true;
            yield return bagService.LoadBagData(OnBagDataLoaded, OnBagDataError);
        }

        isReady = true;

        // 🔁 Sau khi load xong, xử lý các item mua trước đó
        if (pendingItems.Count > 0)
        {
            Debug.Log($"🧩 Xử lý {pendingItems.Count} item pending...");
            foreach (var (id, qty) in pendingItems)
                AddItemToBag(id, qty);

            pendingItems.Clear();
        }
    }

    void GenerateSlots()
    {
        if (slots.Count > 0) return;

        for (int i = 0; i < slotCount; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotParent);
            newSlot.name = "Slot_" + i;
            slots.Add(newSlot);
        }
    }

    void OnBagDataLoaded(List<BagItemData> items)
    {
        for (int i = 0; i < items.Count && i < slots.Count; i++)
        {
            SetItemToSlot(items[i], slots[i].transform);
        }

        Debug.Log($"✅ Loaded {items.Count} items into bag.");
    }

    void OnBagDataError(string error)
    {
        Debug.LogError("❌ Failed to load bag data: " + error);
    }

    void SetItemToSlot(BagItemData data, Transform slot)
    {
        // Tìm prefab tương ứng trong toàn bộ Resources theo itemId
        ItemClass foundItem = FindItemPrefabById(data.itemId);

        // Lấy Image và TMP_Text trong prefab slot
        Image iconImage = slot.Find("ItemIcon")?.GetComponent<Image>();
        TextMeshProUGUI quantityText = slot.Find("Amount")?.GetComponent<TextMeshProUGUI>();

        slot.GetComponent<BagSlotUI>().SetData(data.itemId, data.quantity);

        if (foundItem != null && iconImage != null)
        {
            // Lấy sprite preview từ prefab (nếu có Renderer hoặc SpriteRenderer)
            Sprite icon = GetSpriteFromPrefab(foundItem.gameObject);
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        // Gán số lượng
        if (quantityText != null)
        {
            quantityText.text = data.quantity > 1 ? data.quantity.ToString() : "";
        }
    }

    // 🔍 Tìm prefab trong toàn bộ Resources theo itemId
    ItemClass FindItemPrefabById(string itemId)
    {
        ItemClass[] allItems = Resources.LoadAll<ItemClass>("");
        foreach (var item in allItems)
        {
            if (item.itemId == itemId)
            {
                return item;
            }
        }
        Debug.LogWarning($"⚠ Không tìm thấy item có ID: {itemId}");
        return null;
    }

    // 🖼️ Lấy sprite từ prefab (nếu có SpriteRenderer hoặc Image)
    Sprite GetSpriteFromPrefab(GameObject prefab)
    {
        SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) return sr.sprite;

        Image img = prefab.GetComponentInChildren<Image>();
        if (img != null) return img.sprite;

        return null;
    }

    public List<LoadBag.BagItemData> GetCurrentBagData()
    {
        List<LoadBag.BagItemData> result = new List<LoadBag.BagItemData>();

        foreach (var slot in slots)
        {
            BagSlotUI slotUI = slot.GetComponent<BagSlotUI>();
            if (slotUI != null && !string.IsNullOrEmpty(slotUI.itemId))
            {
                result.Add(new LoadBag.BagItemData
                {
                    itemId = slotUI.itemId,
                    quantity = slotUI.quantity
                });
            }
        }

        return result;
    }

    public List<BagSlotUI> GetCurrentBagSlotUIs()
    {
        List<BagSlotUI> result = new List<BagSlotUI>();
        foreach (var slot in slots)
        {
            BagSlotUI slotUI = slot.GetComponent<BagSlotUI>();
            if (slotUI != null && !string.IsNullOrEmpty(slotUI.itemId))
            {
                result.Add(slotUI);
            }
        }

        return result;
    }

    // ⚙️ Thêm vật phẩm vào túi (hỗ trợ pending nếu bag chưa load)
    public void AddItemToBag(string itemId, int quantityToAdd)
    {
        if (string.IsNullOrEmpty(itemId) || quantityToAdd <= 0)
            return;

        // Nếu bag chưa load → lưu tạm
        if (!isReady)
        {
            pendingItems.Add((itemId, quantityToAdd));
            Debug.Log($"🕒 Bag chưa sẵn sàng, lưu tạm item {itemId} (x{quantityToAdd})");
            return;
        }

        ItemClass[] AllItems = Resources.LoadAll<ItemClass>("");
        GameObject ItemAdd = null;
        foreach (var item in AllItems)
        {
            if (item.itemId == itemId)
            {
                ItemAdd = item.gameObject;
                break;
            }
        }

        Debug.Log($"🧾 AddItemToBag: {itemId}, +{quantityToAdd}");

        bool added = false;

        // 1️⃣ Nếu đã có → cộng số lượng
        foreach (var slot in slots)
        {
            BagSlotUI slotUI = slot.GetComponent<BagSlotUI>();
            if (slotUI == null) continue;

            if (slotUI.itemId == itemId)
            {
                slotUI.quantity += quantityToAdd;
                slotUI.UpdateQuantityUI();
                Debug.Log($"👜 Cộng thêm {quantityToAdd} {itemId}, tổng: {slotUI.quantity}");
                added = true;
                break;
            }
        }

        // 2️⃣ Nếu chưa có → thêm vào slot trống
        if (!added)
        {
            foreach (var slot in slots)
            {
                BagSlotUI slotUI = slot.GetComponent<BagSlotUI>();
                if (slotUI == null) continue;

                if (string.IsNullOrEmpty(slotUI.itemId))
                {
                    slotUI.SetData(itemId, quantityToAdd);
                    Debug.Log($"👜 Thêm mới {itemId} (x{quantityToAdd}) vào túi.");
                    added = true;
                    break;
                }
            }
        }

        // 3️⃣ Nếu vẫn chưa thêm được
        if (!added)
        {
            Debug.LogWarning("⚠️ Túi đã đầy, không thể thêm vật phẩm!");
        }
    }

    // 🔍 Cho phép các script khác kiểm tra bag đã sẵn sàng chưa
    public bool IsReady() => isReady;
}

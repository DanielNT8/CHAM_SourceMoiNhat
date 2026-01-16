using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI quantityText;

    [Header("Item Data")]
    public string itemId;
    public string itemType;
    public int quantity;

    public void SetData(string id, int qty)
    {
        itemId = id;
        quantity = qty;

        // Tự đọc type từ prefab Resources (tìm sâu trong các thư mục con)
        GameObject prefab = FindPrefabById(id);
        if (prefab != null)
        {
            ItemClass itemInfo = prefab.GetComponent<ItemClass>();
            itemType = itemInfo != null ? itemInfo.type : "Unknown";

            // Load icon (tùy bạn đặt lại đường dẫn icon)
            SpriteRenderer icon = prefab.GetComponent<SpriteRenderer>();
            iconImage.sprite = icon.sprite;
            iconImage.enabled = icon != null;
        }

        UpdateQuantityUI();
    }

    public void ReduceQuantity(int amount = 1)
    {
        quantity -= amount;
        if (quantity <= 0)
        {
            ClearSlot();
        }
        else
        {
            UpdateQuantityUI();
        }
    }

    public void UpdateQuantityUI()
    {
        if (quantityText != null)
            quantityText.text = quantity >= 1 ? quantity.ToString() : "";
    }

    void ClearSlot()
    {
        itemId = "";
        itemType = "";
        quantity = 0;
        iconImage.sprite = null;
        iconImage.enabled = false;
        quantityText.text = "";
    }

    GameObject FindPrefabById(string id)
    {
        // Tìm toàn bộ prefab trong Resources (mọi thư mục con)
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");
        foreach (var prefab in allPrefabs)
        {
            var item = prefab.GetComponent<ItemClass>();
            if (item != null && item.itemId == id)
                return prefab;
        }
        return null;
    }
}

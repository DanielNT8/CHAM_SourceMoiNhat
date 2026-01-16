using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ConvertedTreeManage : MonoBehaviour
{
    [System.Serializable]
    public class ConvertedTreeSlot
    {
        public string itemId;                // ID cây cần đổi
        public int requiredQuantity = 5;     // số lượng cần
        public Image iconTree;               // icon hiển thị
        public TextMeshProUGUI qtyText;      // hiển thị số lượng yêu cầu
        public Button claimButton;           // nút "Claim"
    }

    [Header("Danh sách Slot Converted Tree")]
    public List<ConvertedTreeSlot> treeSlots = new List<ConvertedTreeSlot>();

    [Header("Text thông báo (TMP_Text)")]
    public TextMeshProUGUI messageText; // hiển thị thông báo lên UI

    private void Start()
    {
        foreach (var slot in treeSlots)
        {
            // Load icon cây từ prefab
            Sprite icon = GetSpriteFromItemPrefab(slot.itemId);
            if (slot.iconTree != null && icon != null)
                slot.iconTree.sprite = icon;

            if (slot.qtyText != null)
                slot.qtyText.text = "x" + slot.requiredQuantity;

            if (slot.claimButton != null)
            {
                string capturedItem = slot.itemId;
                int capturedQty = slot.requiredQuantity;
                slot.claimButton.onClick.AddListener(() =>
                {
                    TryConvertTree(capturedItem, capturedQty, slot.claimButton);
                });
            }
        }
    }

    #region --- Kiểm tra điều kiện trước khi gửi API ---
    private void TryConvertTree(string itemId, int qty, Button claimBtn)
    {
        if (PlantLogTabManage.Instance == null)
        {
            ShowMessage("Không tìm thấy dữ liệu collection!");
            return;
        }

        var userCollection = GetUserCollection();
        int count = 0;
        foreach (var tree in userCollection)
        {
            if (tree.itemId == itemId)
                count++;
        }

        if (count < qty)
        {
            ShowMessage($"Không đủ cây ({count}/{qty}) để đổi {itemId}!");
            Debug.LogWarning($"[ConvertedTreeManage] ❌ Không đủ số lượng {itemId}: {count}/{qty}");
            return;
        }

        // Đủ số lượng => gửi API
        StartCoroutine(SendConvertedTree(itemId, qty, claimBtn));
    }

    private List<ClaimedTreeData> GetUserCollection()
    {
        return PlantLogTabManage.Instance != null
            ? PlantLogTabManage.Instance.GetType()
                .GetField("listClaimedTree", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(PlantLogTabManage.Instance) as List<ClaimedTreeData>
            : new List<ClaimedTreeData>();
    }
    #endregion

    #region --- Gửi API và trừ số lượng ---
    private IEnumerator SendConvertedTree(string itemId, int qty, Button claimBtn)
    {
        string userId = (UserSession.currentUser != null && !string.IsNullOrEmpty(UserSession.currentUser.userId))
            ? UserSession.currentUser.userId
            : "unknown";

        string url = $"https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/User/ConvertedTree?userId={userId}&qty={qty}&itemId={itemId}";
        Debug.Log($"[ConvertedTreeManage] 🌐 PUT {url}");

        using (UnityWebRequest req = UnityWebRequest.Put(url, ""))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[ConvertedTreeManage] ✅ Đổi {itemId} x{qty} thành công!");
                ShowMessage($"Đổi {itemId} x{qty} thành công!");

                // ✅ Trừ số lượng trong Collection
                RemoveItemsFromCollection(itemId, qty);

                // ✅ Disable nút claim
                if (claimBtn != null)
                {
                    claimBtn.interactable = false;
                    var btnText = claimBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                        btnText.text = "Đã đổi";
                }
            }
            else
            {
                Debug.LogWarning($"[ConvertedTreeManage] ❌ Lỗi gửi ConvertedTree: {req.error}");
                ShowMessage("Lỗi kết nối máy chủ!");
            }
        }
    }

    private void RemoveItemsFromCollection(string itemId, int qty)
    {
        if (PlantLogTabManage.Instance == null) return;

        var collection = GetUserCollection();
        int removed = 0;

        for (int i = collection.Count - 1; i >= 0 && removed < qty; i--)
        {
            if (collection[i].itemId == itemId)
            {
                collection.RemoveAt(i);
                removed++;
            }
        }

        Debug.Log($"[ConvertedTreeManage] Đã trừ {removed}/{qty} cây {itemId} khỏi Collection.");
    }
    #endregion

    #region --- Hiển thị thông báo ---
    private void ShowMessage(string msg)
    {
        if (messageText != null)
        {
            messageText.text = msg;
            if (!messageText.gameObject.activeInHierarchy)
                messageText.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(ClearMessageAfterDelay());
        }
    }

    private IEnumerator ClearMessageAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        if (messageText != null)
            messageText.text = "";
    }
    #endregion

    #region --- Lấy sprite từ prefab ---
    private Sprite GetSpriteFromItemPrefab(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        SplitItemId(itemId, out string reqBase, out int reqLevel);
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");

        foreach (var prefab in allPrefabs)
        {
            var ic = prefab.GetComponent<ItemClass>();
            if (ic != null && ic.itemId == itemId)
            {
                var sr = prefab.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                    return sr.sprite;
            }
        }

        foreach (var prefab in allPrefabs)
        {
            var ic = prefab.GetComponent<ItemClass>();
            if (ic == null) continue;
            SplitItemId(ic.itemId, out string pfBase, out int pfLevel);
            if (pfBase == reqBase && pfLevel == reqLevel)
            {
                var sr = prefab.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                    return sr.sprite;
            }
        }

        Debug.LogWarning($"[ConvertedTreeManage] ⚠️ Không tìm thấy sprite cho {itemId}");
        return null;
    }

    private void SplitItemId(string id, out string baseId, out int level)
    {
        baseId = id;
        level = -1;
        if (string.IsNullOrEmpty(id)) return;
        char last = id[id.Length - 1];
        if (char.IsDigit(last))
        {
            baseId = id.Substring(0, id.Length - 1);
            level = last - '0';
        }
    }
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlantLogTabManage : MonoBehaviour
{
    [Header("Prefab slot hiển thị cây / hạt giống")]
    public GameObject SlotPlantedLog;

    [Header("Parent chứa các slot")]
    public Transform slotParent;

    [Header("Nút tab trái / phải")]
    public Button leftTabButton;   // Planting Trees
    public Button rightTabButton;  // Collections

    private readonly List<GameObject> listPlantedTree = new List<GameObject>();
    private readonly List<ClaimedTreeData> listClaimedTree = new List<ClaimedTreeData>();

    public static PlantLogTabManage Instance;

    private enum TabType { Planting, Collection }
    private TabType currentTab = TabType.Planting;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PlantLogTabManage] ✅ Instance initialized!");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (leftTabButton != null)
            leftTabButton.onClick.AddListener(() => SwitchTab(TabType.Planting));

        if (rightTabButton != null)
            rightTabButton.onClick.AddListener(() => SwitchTab(TabType.Collection));

        GetAllPlantingTrees();

        // ✅ Gọi API khi start game
        LoadPlantLogFromAPI();
    }

    #region --- Tab Switching ---
    private void SwitchTab(TabType type)
    {
        currentTab = type;

        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        if (type == TabType.Planting)
        {
            Debug.Log("[PlantLogTabManage] 🪴 Hiển thị cây đang trồng");
            GetAllPlantingTrees();
        }
        else
        {
            Debug.Log("[PlantLogTabManage] 🌳 Hiển thị Collection");
            ShowCollection();
        }
    }
    #endregion

    #region --- Lấy cây đang trồng ---
    private void GetAllPlantingTrees()
    {
        listPlantedTree.Clear();

        ItemClass[] allItems = FindObjectsByType<ItemClass>(FindObjectsSortMode.None);

        foreach (ItemClass item in allItems)
        {
            if ((item.type == "Seed" || item.type == "Tree") && !item.itemId.Contains("3"))
            {
                listPlantedTree.Add(item.gameObject);
                CreateSlot(item);
            }
        }

        Debug.Log($"[PlantLogTabManage] ✅ Đã load {listPlantedTree.Count} cây đang trồng.");
    }
    #endregion

    #region --- Hiển thị Collection ---
    private void ShowCollection()
    {
        foreach (var tree in listClaimedTree)
        {
            CreateSlot(tree);
        }

        Debug.Log($"[PlantLogTabManage] ✅ Đã load {listClaimedTree.Count} cây trong Collection.");
    }
    #endregion

    #region --- Khi cây được Claim ---
    public void AddToCollection(ClaimedTreeData data)
    {
        StartCoroutine(AddToCollectionRoutine(data));
    }

    private IEnumerator AddToCollectionRoutine(ClaimedTreeData data)
    {
        yield return new WaitForEndOfFrame();

        listClaimedTree.Add(data);
        Debug.Log($"[PlantLogTabManage] 🌿 Cây {data.name} (Lv {data.level}, ID {data.itemId}) đã được thêm vào Collection.");

        if (currentTab == TabType.Collection)
        {
            CreateSlot(data);
        }

        if (currentTab == TabType.Planting)
        {
            foreach (Transform child in slotParent)
                Destroy(child.gameObject);
            GetAllPlantingTrees();
        }

        SendCollectionToAPI();
    }
    #endregion

    #region --- Tạo Slot ---
    private void CreateSlot(ItemClass item)
    {
        if (SlotPlantedLog == null || slotParent == null)
        {
            Debug.LogWarning("[PlantLogTabManage] ⚠️ Slot prefab hoặc slotParent chưa được gán!");
            return;
        }

        GameObject slot = Instantiate(SlotPlantedLog, slotParent);
        var icon = slot.transform.Find("PlantedIcon")?.GetComponent<Image>();
        var levelText = slot.transform.Find("StartIcon/Level")?.GetComponent<TextMeshProUGUI>();

        SpriteRenderer itemImg = item.GetComponent<SpriteRenderer>();
        if (icon != null && itemImg != null)
            icon.sprite = itemImg.sprite;

        SaveItemData data = item.GetComponent<SaveItemData>();
        if (levelText != null && data != null)
            levelText.text = data.level.ToString();
    }

    private void CreateSlot(ClaimedTreeData data)
    {
        if (SlotPlantedLog == null || slotParent == null)
        {
            Debug.LogWarning("[PlantLogTabManage] ⚠️ Slot prefab hoặc slotParent chưa được gán!");
            return;
        }

        GameObject slot = Instantiate(SlotPlantedLog, slotParent);
        var icon = slot.transform.Find("PlantedIcon")?.GetComponent<Image>();
        var levelText = slot.transform.Find("StartIcon/Level")?.GetComponent<TextMeshProUGUI>();
        var idText = slot.transform.Find("ItemIdText")?.GetComponent<TextMeshProUGUI>();

        // ✅ Lấy prefab gốc tương ứng
        GameObject prefab = GetPrefabByItemId(data.itemId);

        if (prefab != null)
        {
            ItemClass prefabItem = prefab.GetComponent<ItemClass>();
            if (prefabItem != null)
            {
                // ✅ Add ItemClass của prefab vào slot
                ItemClass newItem = slot.AddComponent<ItemClass>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(prefabItem), newItem);
                Debug.Log($"[PlantLogTabManage] 🔄 Đã thêm ItemClass từ prefab {prefab.name} vào slot {slot.name}");
            }

            SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
            if (sr != null)
                data.sprite = sr.sprite;
        }

        if (icon != null && data.sprite != null)
            icon.sprite = data.sprite;

        if (levelText != null)
            levelText.text = data.level.ToString();

        if (idText != null)
            idText.text = data.itemId;
    }
    #endregion

    #region --- Gửi dữ liệu Collection lên API ---
    public void SendCollectionToAPI()
    {
        StartCoroutine(SendCollectionRoutine());
    }

    private IEnumerator SendCollectionRoutine()
    {
        List<string> ids = new List<string>();
        foreach (var tree in listClaimedTree)
            ids.Add(tree.itemId);

        if (ids.Count == 0)
        {
            Debug.LogWarning("[PlantLogTabManage] ⚠️ Không có itemId nào để gửi lên server.");
            yield break;
        }

        string userId = (UserSession.currentUser != null && !string.IsNullOrEmpty(UserSession.currentUser.userId))
            ? UserSession.currentUser.userId
            : "unknown";

        string url = $"https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/User/SavePlantedLog?userId={userId}";
        Debug.Log($"[PlantLogTabManage] 🌐 PUT {url}");

        string jsonArray = JsonHelper.ToJson(ids.ToArray());
        Debug.Log("[PlantLogTabManage] 📦 Body JSON: " + jsonArray);

        using (UnityWebRequest req = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonArray);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[PlantLogTabManage] ✅ Gửi danh sách cây thành công!");
            }
            else
            {
                Debug.LogWarning($"[PlantLogTabManage] ❌ Lỗi gửi API: {req.error}");
            }
        }
    }
    #endregion

    #region --- Load dữ liệu Collection từ API ---
    public void LoadPlantLogFromAPI()
    {
        StartCoroutine(LoadPlantLogRoutine());
    }

    private IEnumerator LoadPlantLogRoutine()
    {
        string userId = (UserSession.currentUser != null && !string.IsNullOrEmpty(UserSession.currentUser.userId))
            ? UserSession.currentUser.userId
            : "unknown";

        string url = $"https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/User/GetPlantLogByUserId?userId={userId}";
        Debug.Log($"[PlantLogTabManage] 🌐 GET {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[PlantLogTabManage] ❌ Lỗi tải PlantLog: {req.error}");
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("[PlantLogTabManage] 📥 Response JSON: " + json);

            PlantLogResponse[] logs = JsonHelper.FromJson<PlantLogResponse>(json);
            listClaimedTree.Clear();

            foreach (var log in logs)
            {
                ClaimedTreeData data = new ClaimedTreeData
                {
                    name = log.itemId,
                    itemId = log.itemId,
                    level = 3,
                };

                listClaimedTree.Add(data);
            }

            if (currentTab == TabType.Collection)
            {
                foreach (Transform child in slotParent)
                    Destroy(child.gameObject);

                ShowCollection();
            }

            Debug.Log($"[PlantLogTabManage] ✅ Đã load {listClaimedTree.Count} cây từ API PlantLog.");
        }
    }
    #endregion

    #region --- Helper: Quét toàn bộ prefab trong Resources ---
    private GameObject GetPrefabByItemId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        SplitItemId(itemId, out string reqBase, out int reqLevel);
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");

        // Ưu tiên exact match trước
        foreach (var prefab in allPrefabs)
        {
            var ic = prefab.GetComponent<ItemClass>();
            if (ic != null && ic.itemId == itemId)
                return prefab;
        }

        // Nếu không có, thử base + cùng level
        foreach (var prefab in allPrefabs)
        {
            var ic = prefab.GetComponent<ItemClass>();
            if (ic == null || string.IsNullOrEmpty(ic.itemId)) continue;
            SplitItemId(ic.itemId, out string pfBase, out int pfLevel);

            if (pfBase == reqBase && pfLevel == reqLevel)
                return prefab;
        }

        // Cuối cùng, lấy base (generic)
        foreach (var prefab in allPrefabs)
        {
            var ic = prefab.GetComponent<ItemClass>();
            if (ic == null) continue;
            SplitItemId(ic.itemId, out string pfBase, out int pfLevel);
            if (pfBase == reqBase && pfLevel == -1)
                return prefab;
        }

        Debug.LogWarning($"[PlantLogTabManage] ⚠️ Không tìm thấy prefab cho itemId='{itemId}' trong Resources!");
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

    [System.Serializable]
    public class PlantLogResponse
    {
        public string userId;
        public string itemId;
        public string status;
        public string id;
    }
}

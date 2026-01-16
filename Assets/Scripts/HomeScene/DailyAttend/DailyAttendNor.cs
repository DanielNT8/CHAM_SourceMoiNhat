using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DailyAttendNorSpace
{
    public class DailyAttendNor : MonoBehaviour
    {
        [Header("UI References")]
        public Transform contentParent;
        public GameObject slotPrefab;
        public TMP_Text coins;

        public RectTransform bagUIElement;

        [Header("Data")]
        public List<RewardNorData> rewardList;
        private Dictionary<string, GameObject> resourcePrefabs;

        [Header("References")]
        public BagController bagController;
        private string currentUserId = "";

        private string saveFilePath;
        private AttendanceNorData attendanceData;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip coinCollectionSound; // Kéo tiếng nhận tiền (ding ding)
        [SerializeField] private AudioClip itemCollectionSound;


        void Start()
        {
            currentUserId = UserSession.currentUser != null ? UserSession.currentUser.userId : "";
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            Debug.Log(exeFolder);
            string userFolder = Path.Combine(exeFolder, currentUserId);
            if (!Directory.Exists(userFolder))
                Directory.CreateDirectory(userFolder);

            saveFilePath = Path.Combine(userFolder, "attendance_normal.json");

            LoadAllPrefabsFromResources();
            LoadRewardList();
            LoadAttendanceData();
            CreateRewardSlots();
        }

        void LoadAllPrefabsFromResources()
        {
            resourcePrefabs = new Dictionary<string, GameObject>();
            foreach (var prefab in Resources.LoadAll<GameObject>(""))
            {
                ItemClass item = prefab.GetComponent<ItemClass>();
                if (item != null && !resourcePrefabs.ContainsKey(item.itemId))
                    resourcePrefabs[item.itemId] = prefab;
            }
        }

        void LoadRewardList()
        {
            rewardList = new List<RewardNorData>
            {
                new RewardNorData { ItemId = "Coins", quantity = 50 },
                new RewardNorData { ItemId = "DG001", quantity = 1 },
                new RewardNorData { ItemId = "Coins", quantity = 100 },
                new RewardNorData { ItemId = "DF001", quantity = 1 },
                new RewardNorData { ItemId = "Coins", quantity = 200 },
                new RewardNorData { ItemId = "DC001", quantity = 2 },
                new RewardNorData { ItemId = "DFR01", quantity = 3 }
            };
        }

        void LoadAttendanceData()
        {
            if (File.Exists(saveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(saveFilePath);
                    attendanceData = JsonUtility.FromJson<AttendanceNorData>(json);
                }
                catch
                {
                    attendanceData = new AttendanceNorData();
                }
            }
            else
            {
                attendanceData = new AttendanceNorData();
            }

            DateTime today = DateTime.Now.Date;
            if (!string.IsNullOrEmpty(attendanceData.lastClaimDate))
            {
                DateTime lastDate = DateTime.Parse(attendanceData.lastClaimDate);
                if ((today - lastDate).Days >= 2)
                {
                    Debug.Log("⚠️ Bỏ qua 1 ngày -> reset chuỗi điểm danh Normal!");
                    attendanceData.claimedDays.Clear();
                    attendanceData.lastClaimDate = "";
                    SaveAttendanceData();
                }
            }
        }

        void CreateRewardSlots()
        {
            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

            for (int i = 0; i < rewardList.Count; i++)
            {
                RewardNorData reward = rewardList[i];
                GameObject slot = Instantiate(slotPrefab, contentParent);

                Image icon = slot.transform.Find("ItemIcon")?.GetComponent<Image>();
                TextMeshProUGUI qtyText = slot.transform.Find("ItemIcon/Quantity")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI dayNo = slot.transform.Find("Day/No")?.GetComponent<TextMeshProUGUI>();

                if (resourcePrefabs.TryGetValue(reward.ItemId, out GameObject prefab))
                {
                    Sprite iconSprite = prefab.GetComponent<SpriteRenderer>()?.sprite
                        ?? prefab.GetComponent<Image>()?.sprite;

                    if (icon != null)
                    {
                        icon.sprite = iconSprite;
                        icon.color = Color.white;
                    }
                }
                else if (icon != null)
                {
                    icon.sprite = null;
                    icon.color = Color.clear;
                }

                if (qtyText != null)
                    qtyText.text = "x" + reward.quantity;

                if (dayNo != null)
                    dayNo.text = (i + 1).ToString();

                CanvasGroup group = slot.GetComponent<CanvasGroup>() ?? slot.AddComponent<CanvasGroup>();
                bool isClaimed = attendanceData.claimedDays.Contains(i + 1);
                group.alpha = isClaimed ? 0.5f : 1f;

                Button btn = slot.GetComponent<Button>() ?? slot.AddComponent<Button>();
                int dayIndex = i + 1;
                btn.interactable = !isClaimed && CanClaimThisDay(dayIndex);

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnRewardClicked(reward, slot, dayIndex));
            }
        }

        bool CanClaimThisDay(int dayIndex)
        {
            int nextDay = attendanceData.claimedDays.Count + 1;
            if (dayIndex != nextDay)
                return false;

            if (!string.IsNullOrEmpty(attendanceData.lastClaimDate))
            {
                if (DateTime.Parse(attendanceData.lastClaimDate) == DateTime.Now.Date)
                    return false;
            }

            return true;
        }

        void OnRewardClicked(RewardNorData reward, GameObject slot, int dayIndex)
        {
            if (!CanClaimThisDay(dayIndex)) return;

            // 1. Xác định đích đến (Target)
            RectTransform targetRect;
            if (reward.ItemId == "Coins" && coins != null)
                targetRect = coins.rectTransform; // Bay vào thanh tiền
            else
                targetRect = bagUIElement; // Bay vào túi đồ

            // 2. Lấy hình ảnh icon từ slot hiện tại
            Image slotImage = slot.transform.Find("ItemIcon")?.GetComponent<Image>();
            Sprite iconSprite = slotImage != null ? slotImage.sprite : null;

            // 3. Gọi Manager để chạy hiệu ứng
            // Lưu ý: slot.GetComponent<RectTransform>() là điểm bắt đầu
            if (ResourceFlyManager.Instance != null)
            {
                ResourceFlyManager.Instance.PlayFromUI(
                    slot.GetComponent<RectTransform>(),
                    targetRect,
                    iconSprite,
                    reward.quantity, 
                    () =>
                    {
                        // 4. Callback: Code xử lý dữ liệu (Chỉ chạy khi bay xong)
                        HandleClaimData(reward, slot, dayIndex);
                    }
                );
            }
            else
            {
                // Fallback: Nếu quên chưa bỏ Manager vào scene thì vẫn nhận quà bình thường
                HandleClaimData(reward, slot, dayIndex);
            }
        }

        // Tách logic nhận quà ra hàm riêng để gọi trong Callback
        void HandleClaimData(RewardNorData reward, GameObject slot, int dayIndex)
        {
            Debug.Log($"🎯 Nhận reward Normal: {reward.ItemId} x{reward.quantity}");

            if (AudioManager.Instance != null)
            {
                if (reward.ItemId == "Coins")
                {
                    // Nếu là tiền -> Phát tiếng Coin
                    AudioManager.Instance.PlaySFX(coinCollectionSound);
                }
                else
                {
                    // Nếu là vật phẩm -> Phát tiếng Item vào túi
                    AudioManager.Instance.PlaySFX(itemCollectionSound);
                }
            }

            if (reward.ItemId == "Coins" && coins != null && int.TryParse(coins.text, out int current))
            {
                coins.text = (current + reward.quantity).ToString();
            }
            else if (bagController != null)
            {
                bagController.AddItemToBag(reward.ItemId, reward.quantity);
            }

            attendanceData.claimedDays.Add(dayIndex);
            attendanceData.lastClaimDate = DateTime.Now.ToString("yyyy-MM-dd");
            SaveAttendanceData();

            // Cập nhật UI Slot
            CanvasGroup group = slot.GetComponent<CanvasGroup>();
            if (group) group.alpha = 0.5f;

            Button btn = slot.GetComponent<Button>();
            if (btn) btn.interactable = false;

            Debug.Log($"✅ Đã nhận phần thưởng Normal ngày {dayIndex}");
        }


        void SaveAttendanceData()
        {
            File.WriteAllText(saveFilePath, JsonUtility.ToJson(attendanceData, true));
        }
    }

    [Serializable]
    public class RewardNorData
    {
        public string ItemId;
        public int quantity;
    }

    [Serializable]
    public class AttendanceNorData
    {
        public List<int> claimedDays = new();
        public string lastClaimDate = "";
    }
}

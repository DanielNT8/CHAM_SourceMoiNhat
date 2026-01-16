using DG.Tweening;
using System;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DailyTaskData
{
    public bool watered;
    public bool fertilized;
    public bool waterClaimed;
    public bool fertilizerClaimed;
    public int coins;
    public string lastResetDate;
}

public class DailyTaskManage : MonoBehaviour
{
    [Header("User Info")]
    public TMP_Text userCoins;

    [Header("Effect Settings")]
    public RectTransform coinTargetUI;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip coinCollectionSound; // Tiếng nhận tiền
    [SerializeField] private AudioClip itemCollectionSound;

    [Header("Task 1 - Water Plants")]
    public Button task1ClaimBtn;
    public TMP_Text task1QuantityText;
    [AllowsNull]
    public TMP_Text task1StatusText;
    public int task1Reward = 5;

    [Header("Task 2 - Apply Fertilizer")]
    public Button task2ClaimBtn;
    public TMP_Text task2QuantityText;
    [AllowsNull]
    public TMP_Text task2StatusText;
    public int task2Reward = 10;

    private DailyTaskData data;
    private string saveFilePath;

    private void Start()
    {
        // ✅ Lưu file riêng biệt theo userId
        if (UserSession.currentUser != null && !string.IsNullOrEmpty(UserSession.currentUser.userId))
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, $"DailyTaskData_{UserSession.currentUser.userId}.json");
        }
        else
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, "DailyTaskData_Guest.json");
        }

        Debug.Log($"📁 File nhiệm vụ: {saveFilePath}");

        LoadData();
        CheckDailyReset();

        // ✅ Đồng bộ coin từ server
        if (UserSession.currentUser != null)
        {
            Debug.Log($"🔄 Đồng bộ coin từ server: {UserSession.currentUser.coin}");
            data.coins = UserSession.currentUser.coin;
            SaveData();
        }

        // Gán sự kiện cho các nút
        task1ClaimBtn.onClick.AddListener(ClaimWaterTask);
        task2ClaimBtn.onClick.AddListener(ClaimFertilizerTask);

        task1QuantityText.text = $"{task1Reward}";
        task2QuantityText.text = $"{task2Reward}";

        UpdateUI();
    }

    private Sprite GetIconFromButton(Button btn)
    {
        // Tìm object con tên "Icon" bên trong nút để lấy ảnh
        Transform iconTrans = btn.transform.Find("Icon");
        if (iconTrans != null && iconTrans.TryGetComponent<Image>(out var img))
        {
            return img.sprite;
        }
        return null; // Không tìm thấy thì trả về null
    }

    // ================== LƯU / TẢI DỮ LIỆU ==================
    private void LoadData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            data = JsonUtility.FromJson<DailyTaskData>(json);
            Debug.Log("📥 Đã tải dữ liệu DailyTask cho user hiện tại.");
        }
        else
        {
            data = new DailyTaskData
            {
                watered = false,
                fertilized = false,
                waterClaimed = false,
                fertilizerClaimed = false,
                coins = 0,
                lastResetDate = DateTime.Now.ToString("yyyyMMdd")
            };
            SaveData();
            Debug.Log("🆕 Tạo file DailyTaskData mới cho user hiện tại.");
        }
    }

    private void SaveData()
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
    }

    // ================== RESET HÀNG NGÀY ==================
    private void CheckDailyReset()
    {
        string today = DateTime.Now.ToString("yyyyMMdd");

        if (data.lastResetDate != today)
        {
            Debug.Log("🕐 Reset nhiệm vụ hàng ngày!");
            data.lastResetDate = today;

            data.watered = false;
            data.fertilized = false;
            data.waterClaimed = false;
            data.fertilizerClaimed = false;

            SaveData();
        }
    }

    // ================== GỌI TỪ NGOÀI ==================
    public void MarkWatered()
    {
        if (!data.watered)
        {
            data.watered = true;
            SaveData();
            UpdateUI();
            PlayClaimEffect(task1ClaimBtn);
        }
    }

    public void MarkFertilized()
    {
        if (!data.fertilized)
        {
            data.fertilized = true;
            SaveData();
            UpdateUI();
            PlayClaimEffect(task2ClaimBtn);
        }
    }

    // ================== CLAIM ==================
    private void ClaimWaterTask()
    {
        if (!data.watered || data.waterClaimed) return;

        // 1. Cập nhật trạng thái "Đã nhận" ngay lập tức
        data.waterClaimed = true;

        // 2. Dừng hiệu ứng nút và cập nhật UI (Disable nút ngay)
        StopClaimEffect(task1ClaimBtn);
        UpdateUI();

        // 3. LƯU DỮ LIỆU LẦN 1
        SaveData();

        // 4. Chạy hiệu ứng bay
        if (ResourceFlyManager.Instance != null)
        {
            ResourceFlyManager.Instance.PlayFromUI(
                task1ClaimBtn.GetComponent<RectTransform>(),
                coinTargetUI,
                GetIconFromButton(task1ClaimBtn),
                task1Reward,
                () => {
                    // 5. Callback: Bay xong mới xử lý logic

                    // --- [MỚI] PHÁT ÂM THANH ---
                    // Vì Task này thưởng Coin nên phát tiếng Coin
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(coinCollectionSound);
                    // --------------------------

                    AddCoin(task1Reward);
                    Debug.Log($"✅ Nhận thưởng: +{task1Reward} coins (Water Task)");
                }
            );
        }
        else
        {
            // Fallback: Không có hiệu ứng thì vẫn cộng tiền + âm thanh
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(coinCollectionSound);
            AddCoin(task1Reward);
            Debug.Log($"✅ Nhận thưởng (No FX): +{task1Reward} coins (Water Task)");
        }
    }

    private void ClaimFertilizerTask()
    {
        if (!data.fertilized || data.fertilizerClaimed) return;

        // 1. Cập nhật trạng thái
        data.fertilizerClaimed = true;

        // 2. Cập nhật UI
        StopClaimEffect(task2ClaimBtn);
        UpdateUI();

        // 3. LƯU DỮ LIỆU LẦN 1
        SaveData();

        // 4. Chạy hiệu ứng bay
        if (ResourceFlyManager.Instance != null)
        {
            ResourceFlyManager.Instance.PlayFromUI(
                task2ClaimBtn.GetComponent<RectTransform>(),
                coinTargetUI,
                GetIconFromButton(task2ClaimBtn),
                task2Reward,
                () => {
                    // 5. Callback: Bay xong mới xử lý logic

                    // --- [MỚI] PHÁT ÂM THANH ---
                    // Task này cũng thưởng Coin
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(coinCollectionSound);
                    // --------------------------

                    AddCoin(task2Reward);
                    Debug.Log($"✅ Nhận thưởng: +{task2Reward} coins (Fertilizer Task)");
                }
            );
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(coinCollectionSound);
            AddCoin(task2Reward);
            Debug.Log($"✅ Nhận thưởng (No FX): +{task2Reward} coins (Fertilizer Task)");
        }
    }

    // ================== COIN ==================
    private void AddCoin(int amount)
    {
        data.coins += amount;

        // ✅ Đồng bộ coin sang UserSession (server)
        if (UserSession.currentUser != null)
        {
            UserSession.currentUser.coin = data.coins;
        }

        SaveData();
        UpdateCoinUI();
        Debug.Log($"💰 Tổng coin hiện tại: {data.coins}");
    }

    private void UpdateCoinUI()
    {
        if (userCoins != null)
            userCoins.text = data.coins.ToString();
    }

    // ================== UI ==================
    private void UpdateUI()
    {
        // Task 1
        UpdateTaskUI(
            data.watered,
            data.waterClaimed,
            task1StatusText,
            task1ClaimBtn,
            Color.gray,
            new Color(0.3f, 0.8f, 0.3f)
        );

        // Task 2
        UpdateTaskUI(
            data.fertilized,
            data.fertilizerClaimed,
            task2StatusText,
            task2ClaimBtn,
            Color.gray,
            new Color(0.3f, 0.8f, 0.3f)
        );

        UpdateCoinUI();
    }

    private void UpdateTaskUI(bool done, bool claimed, TMP_Text statusText, Button btn, Color disabledColor, Color completedColor)
    {
        Image btnImage = btn.GetComponent<Image>();
        TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();

        statusText.gameObject.SetActive(true);

        if (claimed)
        {
            statusText.text = "Đã nhận!";
            btn.interactable = false;

            if (btnImage != null) btnImage.color = disabledColor;
            if (btnText != null) btnText.color = Color.white;
        }
        else if (done)
        {
            statusText.text = "Đã hoàn thành!";
            btn.interactable = true;

            if (btnImage != null) btnImage.color = completedColor;
            if (btnText != null) btnText.color = Color.white;
        }
        else
        {
            statusText.text = "Chưa hoàn thành!";
            btn.interactable = false;

            if (btnImage != null) btnImage.color = disabledColor;
            if (btnText != null) btnText.color = Color.white;
        }

        DOVirtual.DelayedCall(1f, () =>
        {
            statusText.text = string.Empty;
        });
    }

    // ================== DOTWEEN HIỆU ỨNG ==================
    private void PlayClaimEffect(Button btn)
    {
        Image btnImage = btn.GetComponent<Image>();
        TMP_Text text = btn.GetComponentInChildren<TMP_Text>();

        btn.transform.DOShakeScale(1.2f, 0.15f, 5, 80f, false, ShakeRandomnessMode.Harmonic)
            .SetLoops(-1, LoopType.Restart);

        if (btnImage != null)
        {
            Color baseColor = btnImage.color;
            btnImage.DOColor(new Color(1f, 1f, 0.7f), 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .OnKill(() => btnImage.color = baseColor);
        }

        if (text != null)
        {
            Color baseColor = text.color;
            text.DOColor(new Color(1f, 0.9f, 0.5f), 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .OnKill(() => text.color = baseColor);
        }
    }

    private void StopClaimEffect(Button btn)
    {
        btn.transform.DOKill();
        btn.GetComponent<Image>()?.DOKill();
        btn.GetComponentInChildren<TMP_Text>()?.DOKill();
        btn.transform.localScale = Vector3.one;
    }
}

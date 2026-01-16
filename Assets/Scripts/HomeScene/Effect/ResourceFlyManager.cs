using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;

public class ResourceFlyManager : MonoBehaviour
{
    public static ResourceFlyManager Instance;

    [Header("Cấu hình Chung")]
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform fxCanvas;
    [SerializeField] private int maxItems = 20;

    [Header("Cấu hình Bay Về Đích")]
    [SerializeField] private float flyDuration = 0.6f;
    [SerializeField] private float jumpPowerTarget = 100f; // Độ cong khi bay về đích

    [Header("Cấu hình Vật Lý (Làm cho thật hơn)")]
    [SerializeField] private float initialScatterPower = 80f; // Lực bung ra ban đầu (Càng lớn bung càng xa)
    [SerializeField] private float bounceHeight = 50f;        // Độ cao cú nảy đầu tiên
    [SerializeField] private float deceleration = 0.5f;       // Độ suy giảm (0.5 = mỗi lần nảy giảm 50% lực)

    // --- POOLING & SCALING ---
    private List<GameObject> itemPool = new List<GameObject>();
    private Dictionary<int, Vector3> originalScales = new Dictionary<int, Vector3>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        for (int i = 0; i < 10; i++) CreateNewItemToPool();
    }

    private GameObject GetItemFromPool()
    {
        foreach (var item in itemPool)
        {
            if (!item.activeSelf) { item.SetActive(true); return item; }
        }
        return CreateNewItemToPool(true);
    }

    private GameObject CreateNewItemToPool(bool active = false)
    {
        GameObject item = Instantiate(itemPrefab, fxCanvas);
        item.SetActive(active);
        itemPool.Add(item);
        return item;
    }

    public void PlayFromUI(RectTransform startUI, RectTransform targetUI, Sprite icon, int amount, System.Action onComplete)
    {
        SpawnAndFly(startUI.position, targetUI, icon, amount, onComplete);
    }

    public void PlayFromWorld(Vector3 worldPos, RectTransform targetUI, Sprite icon, int amount, System.Action onComplete)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        SpawnAndFly(screenPos, targetUI, icon, amount, onComplete);
    }

    // --- LOGIC XỬ LÝ CHÍNH ---
    private void SpawnAndFly(Vector3 startPos, RectTransform targetUI, Sprite icon, int amount, System.Action onComplete)
    {
        if (targetUI == null || !targetUI.gameObject.activeInHierarchy)
        {
            onComplete?.Invoke();
            return;
        }

        int targetID = targetUI.gameObject.GetInstanceID();
        if (!originalScales.ContainsKey(targetID)) originalScales.Add(targetID, targetUI.localScale);

        int count = Mathf.Min(amount, maxItems);
        int finished = 0;

        for (int i = 0; i < count; i++)
        {
            GameObject item = GetItemFromPool();

            // Reset Item
            item.transform.position = startPos;
            item.transform.localScale = Vector3.zero;
            item.transform.rotation = Quaternion.identity;
            item.transform.DOKill();

            Image img = item.GetComponent<Image>();
            if (img) img.sprite = icon;

            Sequence seq = DOTween.Sequence();

            // =================================================================================
            // GIAI ĐOẠN 1: TÍNH TOÁN & APPEND CÁC BƯỚC NẢY (Jump) TRƯỚC
            // =================================================================================

            Vector3 randomDir = Random.insideUnitCircle.normalized;
            Vector3 currentPos = startPos;
            float currentPower = initialScatterPower;
            float currentHeight = bounceHeight;

            int bounceCount = Random.Range(2, 4);
            float stepDuration = 0.3f;

            // [SỬA LỖI] Vòng lặp Jump thực hiện Append trước để định hình Timeline
            for (int k = 0; k < bounceCount; k++)
            {
                float dist = currentPower * Random.Range(0.8f, 1.2f);
                Vector3 nextPos = currentPos + (randomDir * dist);

                // Append: Nối tiếp nhau tạo thành chuyển động nảy
                seq.Append(item.transform.DOJump(nextPos, currentHeight, 1, stepDuration).SetEase(Ease.Linear));

                currentPos = nextPos;
                currentPower *= deceleration;
                currentHeight *= deceleration;
                stepDuration *= 0.8f;
            }

            // =================================================================================
            // GIAI ĐOẠN 2: CHÈN (INSERT) HIỆU ỨNG XOAY & SCALE VÀO THỜI ĐIỂM 0
            // =================================================================================

            // [QUAN TRỌNG] Insert(0, ...) bắt buộc hiệu ứng chạy ngay từ đầu, song song với Jump

            // 1. Scale từ 0 lên 1
            seq.Insert(0, item.transform.DOScale(1f, 0.2f));

            // 2. Xoay: Tính tổng thời gian nảy để xoay cho khớp
            float totalBounceDuration = seq.Duration();
            float rotateAngle = Random.Range(-360f, 360f);

            // Xoay trong suốt quá trình nảy (totalBounceDuration)
            seq.Insert(0, item.transform.DORotate(new Vector3(0, 0, rotateAngle), totalBounceDuration, RotateMode.FastBeyond360));


            // =================================================================================
            // GIAI ĐOẠN 3: BAY VỀ ĐÍCH (Tiếp tục Append vào sau khi nảy xong)
            // =================================================================================

            seq.AppendInterval(Random.Range(0.05f, 0.15f)); // Nghỉ xíu

            // Xoay về 0 khi bay
            seq.Join(item.transform.DORotate(Vector3.zero, flyDuration * 0.5f));

            // Bay về đích
            seq.Append(item.transform.DOJump(targetUI.position, jumpPowerTarget, 1, flyDuration).SetEase(Ease.InQuad));

            // Thu nhỏ
            seq.Insert(seq.Duration() - 0.2f, item.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));

            seq.OnComplete(() =>
            {
                item.SetActive(false);
                finished++;

                if (targetUI != null)
                {
                    targetUI.DOKill(true);
                    if (originalScales.ContainsKey(targetID)) targetUI.localScale = originalScales[targetID];
                    targetUI.DOPunchScale(originalScales[targetID] * 0.15f, 0.15f, 1, 0.5f);
                }

                if (finished >= count) onComplete?.Invoke();
            });
        }
    }
}
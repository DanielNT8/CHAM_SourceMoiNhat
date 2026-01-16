using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpinWheelManage : MonoBehaviour
{
    [Header("References")]
    public Transform wheel;
    public Transform arrow;
    public BagController bagController;
    public Button spinButton;
    public TextMeshProUGUI messageText;

    [Header("Config")]
    public float spinDuration = 3f;
    public float maxSpeed = 800f;

    private bool isSpinning = false;
    private Coroutine messageCoroutine;

    void Start()
    {
        if (spinButton != null)
            spinButton.onClick.AddListener(OnSpinButtonClicked);
    }

    private void OnSpinButtonClicked()
    {
        if (isSpinning)
        {
            ShowMessage("Vòng quay đang chạy...");
            return;
        }

        if (!bagController.IsReady())
        {
            ShowMessage("Túi đồ chưa sẵn sàng, xin hãy chờ chút...");
            return;
        }

        bool hasTicket = false;

        var currentItems = bagController.GetCurrentBagSlotUIs();
        foreach (var item in currentItems)
        {
            if (item.itemId == "TKE01" && item.quantity > 0)
            {
                item.ReduceQuantity(1);
                hasTicket = true;
                break;
            }
        }

        if (!hasTicket)
        {
            ShowMessage("Bạn không có đủ vé quay!");
            return;
        }

        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        ShowMessage("Đang quay...");

        float time = 0f;
        float startRotation = wheel.eulerAngles.z;
        float totalRotation = 1440f + Random.Range(0f, 360f);

        while (time < spinDuration)
        {
            time += Time.deltaTime;
            float t = time / spinDuration;
            float easeT = t * (2 - t);

            float currentAngle = Mathf.Lerp(0, totalRotation, easeT);
            wheel.rotation = Quaternion.Euler(0, 0, startRotation + currentAngle);
            yield return null;
        }

        wheel.rotation = Quaternion.Euler(0, 0, startRotation + totalRotation);

        yield return new WaitForSeconds(0.5f);
        isSpinning = false;
        ShowMessage("");
        CheckHitGift();
    }

    private void CheckHitGift()
    {
        Collider2D arrowCollider = arrow.GetComponent<Collider2D>();
        if (arrowCollider == null)
        {
            Debug.LogError("❌ Arrow chưa có BoxCollider2D!");
            return;
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            arrowCollider.bounds.center,
            arrowCollider.bounds.size,
            0f
        );

        if (hits.Length == 0)
        {
            Debug.Log("⚠️ Không phát hiện phần thưởng nào va chạm với mũi tên!");
            return;
        }

        Debug.Log($"🔍 Arrow va chạm với {hits.Length} collider(s):");
        foreach (var hit in hits)
        {
            Debug.Log($"   👉 {hit.name}");

            ItemClass giftItem = hit.GetComponent<ItemClass>();
            if (giftItem != null)
            {
                Debug.Log($"🎁 Trúng phần thưởng: {giftItem.itemId} ({giftItem.itemName})");
                ShowMessage($" Bạn nhận được phần thưởng: {giftItem.itemId}");
                bagController.AddItemToBag(giftItem.itemId, 1);
                return; // chỉ lấy quà đầu tiên
            }
        }

        Debug.Log("❌ Không tìm thấy ItemClass hợp lệ trong các collider va chạm.");
    }

    private void ShowMessage(string msg)
    {
        if (messageText != null)
        {
            messageText.text = msg;
            if (!messageText.gameObject.activeInHierarchy)
                messageText.gameObject.SetActive(true);
            // ⚠️ KHÔNG dùng StopAllCoroutines() — nó sẽ dừng cả SpinRoutine!
            if (messageCoroutine != null)
                StopCoroutine(messageCoroutine);

            messageCoroutine = StartCoroutine(HideMessageAfterDelay(2f));
        }
        else
        {
            Debug.Log(msg);
        }
    }

    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (messageText != null)
            messageText.text = "";
    }
}

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("--- UI COMPONENTS ---")]
    public Image overlayImage;
    public RectTransform firefly;
    public Transform dialogueBox;
    public TextMeshProUGUI dialogueText;
    public Button skipButton;            // NÚT SKIP

    // Tọa độ lệch tối đa (X và Y). Code sẽ tự động đảo dấu âm/dương.
    public Vector2 textOffset = new Vector2(80, 80);

    [Header("--- SHADER SETTINGS ---")]
    public float holeSmallRadius = 0.04f;
    public float holeBigRadius = 0.1f;
    public float holeExpandSpeed = 0.5f;

    [System.Serializable]
    public struct TutorialStep
    {
        public RectTransform targetUI;
        [TextArea] public string message;
        public float customHoleSize;
    }

    public TutorialStep[] steps;

    private Material holeMat;
    private Coroutine currentFlow;
    public static bool isTutorialRunning = false;

    void Awake()
    {
        holeMat = Instantiate(overlayImage.material);
        overlayImage.material = holeMat;
    }

    void Start()
    {
        // Gán sự kiện cho nút Skip
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(SkipTutorial);
        }

        if (PlayerPrefs.GetInt(GetTutorialSaveKey(), 0) == 0)
        {
            ReplayTutorial();
        }
        else
        {
            DisableTutorialUI();
        }
    }

    private string GetTutorialSaveKey()
    {
        var user = UserSession.currentUser;
        if (user != null && !string.IsNullOrEmpty(user.userId)) return $"TutorialPlayed_{user.userId}";
        return "TutorialPlayed_Guest";
    }

    // --- HÀM BỎ QUA (SKIP) VÀ CHUYỂN SANG TRỒNG CÂY ---
    public void SkipTutorial()
    {
        if (!isTutorialRunning) return;

        // Dừng flow hiện tại
        StopAllCoroutines();

        // Dừng các hoạt ảnh đang chạy của UI này
        firefly.DOKill();
        dialogueBox.DOKill();

        // Tắt lỗ thủng ngay lập tức
        holeMat.SetFloat("_Radius", 0);

        DisableTutorialUI();

        // Lưu lại trạng thái
        PlayerPrefs.SetInt(GetTutorialSaveKey(), 1);
        PlayerPrefs.Save();

        // GỌI HÀM BẮT ĐẦU GIAI ĐOẠN 2 Ở ĐÂY
        StartFarmingPhase();
    }

    // --- GIAI ĐOẠN 2: THỰC HÀNH TRỒNG CÂY ---
    private void StartFarmingPhase()
    {
        Debug.Log("🚀 Chuyển sang Giai đoạn 2: Cầm tay chỉ việc trồng cây!");
        // Viết code gọi sang script FarmingTutorial của bạn tại đây
        if (FarmingTutorialController.Instance != null)
        {
            FarmingTutorialController.Instance.StartFarming();
        }
    }

    public void ReplayTutorial()
    {
        if (isTutorialRunning) return;

        StopAllCoroutines();
        firefly.DOKill();
        dialogueBox.DOKill();

        currentFlow = StartCoroutine(RunFlow());
    }

    void Update()
    {
        if (isTutorialRunning && firefly.gameObject.activeSelf)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, firefly.position);
            Vector2 viewportPos = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
            holeMat.SetVector("_Center", viewportPos);
        }
    }

    IEnumerator RunFlow()
    {
        isTutorialRunning = true;
        overlayImage.gameObject.SetActive(true);
        firefly.gameObject.SetActive(true);
        if (skipButton) skipButton.gameObject.SetActive(true); // Bật nút Skip

        dialogueBox.gameObject.SetActive(true);
        dialogueBox.localScale = Vector3.zero;

        holeMat.SetFloat("_Radius", 0);
        firefly.localPosition = new Vector3(-1500, -1500, 0);

        DOVirtual.Float(0, holeSmallRadius, 1.5f, (v) => holeMat.SetFloat("_Radius", v));
        yield return firefly.DOLocalMove(Vector2.zero, 1.5f).SetEase(Ease.OutQuad).WaitForCompletion();

        // BƯỚC 0: LỜI CHÀO MỞ ĐẦU PHASE 1

        RectTransform dialogRect = dialogueBox.GetComponent<RectTransform>();
        dialogRect.pivot = new Vector2(0.5f, 0f); // Tâm ở dưới đáy

        // Đặt khung thoại nằm ngay trên đầu đom đóm
        dialogueBox.position = firefly.position + new Vector3(0, Mathf.Abs(textOffset.y), 0);

        dialogueText.text = "Chào bạn! Để mình hướng dẫn bạn làm quen với các nút nhé.";
        yield return dialogueBox.DOScale(1, 0.4f).SetEase(Ease.OutBack).WaitForCompletion();

        yield return new WaitForSeconds(3.5f); // Đứng chờ 3.5s cho người chơi đọc

        yield return dialogueBox.DOScale(0, 0.2f).SetEase(Ease.InBack).WaitForCompletion();



        foreach (var step in steps)
        {
            if (step.targetUI == null) continue;

            DOVirtual.Float(holeMat.GetFloat("_Radius"), holeSmallRadius, 0.4f, (v) => holeMat.SetFloat("_Radius", v));
            yield return firefly.DOMove(step.targetUI.position, 1.2f).SetEase(Ease.InOutQuad).WaitForCompletion();

            Tween fireflyHover = firefly.DOBlendableLocalMoveBy(new Vector3(0, 15, 0), 0.8f).SetLoops(-1, LoopType.Yoyo);

            float targetRadius = step.customHoleSize > 0 ? step.customHoleSize : holeBigRadius;
            DOVirtual.Float(holeSmallRadius, targetRadius, holeExpandSpeed, (v) => holeMat.SetFloat("_Radius", v)).SetEase(Ease.OutBack);

            // ==========================================
            // LOGIC TỌA ĐỘ 4 GÓC (SỬA LỖI GIẬT & TRÀN MÀN HÌNH)
            // ==========================================
            Vector2 newPivot = new Vector2(0.5f, 0.5f);
            Vector3 finalOffset = Vector3.zero;

            // Xử lý ngang (Trái / Phải)
            if (step.targetUI.position.x < Screen.width * 0.4f)
            {
                newPivot.x = 0f; // Tâm ở mép trái
                finalOffset.x = Mathf.Abs(textOffset.x); // Đẩy sang phải
            }
            else if (step.targetUI.position.x > Screen.width * 0.6f)
            {
                newPivot.x = 1f; // Tâm ở mép phải
                finalOffset.x = -Mathf.Abs(textOffset.x); // Đẩy sang trái
            }
            else
            {
                newPivot.x = 0.5f; // Ở giữa
                finalOffset.x = 0;
            }

            // Xử lý dọc (Trên / Dưới)
            if (step.targetUI.position.y > Screen.height * 0.6f)
            {
                newPivot.y = 1f; // Tâm ở đỉnh
                finalOffset.y = -Mathf.Abs(textOffset.y); // Đẩy xuống dưới
            }
            else
            {
                newPivot.y = 0f; // Tâm ở đáy
                finalOffset.y = Mathf.Abs(textOffset.y); // Đẩy lên trên
            }

            // Gán Pivot và Position NGAY LẬP TỨC TRƯỚC KHI SCALE (Tránh Flicker)
            dialogRect.pivot = newPivot;
            dialogueBox.position = step.targetUI.position + finalOffset;

            // Set Text và Nảy lên
            dialogueText.text = step.message;
            yield return dialogueBox.DOScale(1, 0.4f).SetEase(Ease.OutBack).WaitForCompletion();

            yield return new WaitForSeconds(3f);

            yield return dialogueBox.DOScale(0, 0.2f).SetEase(Ease.InBack).WaitForCompletion();
            fireflyHover.Kill();
        }

        yield return DOVirtual.Float(holeMat.GetFloat("_Radius"), 0, 0.8f, (v) => holeMat.SetFloat("_Radius", v)).WaitForCompletion();

        DisableTutorialUI();
        PlayerPrefs.SetInt(GetTutorialSaveKey(), 1);
        PlayerPrefs.Save();

        // Xong pha 1 tự động chuyển pha 2
        StartFarmingPhase();
    }

    void DisableTutorialUI()
    {
        isTutorialRunning = false;
        overlayImage.gameObject.SetActive(false);
        firefly.gameObject.SetActive(false);
        if (skipButton) skipButton.gameObject.SetActive(false);
        if (dialogueBox != null) dialogueBox.localScale = Vector3.zero;
    }
}
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class FarmingTutorialController : MonoBehaviour
{
    public static bool IsTutorialMode = false;
    public static FarmingTutorialController Instance;

    [Header("--- TÍN HIỆU TIẾN ĐỘ ---")]
    public static bool isBagOpened = false;
    public static bool isSeedPlanted = false;
    public static bool isWatered = false;
    public static bool isFertilized = false;
    public static bool isPesticided = false;

    [Header("--- KẾT NỐI UI & SHADER ---")]
    public TutorialManager tutorialManager;
    public Image holeOverlay;
    public HoleRaycastFilter raycastFilter;
    public Button skipButton;

    [Header("--- VỊ TRÍ MỤC TIÊU ---")]
    public Transform tutorialSoil;       // Ô đất mồi (World Space)
    public RectTransform bagButtonUI;    // Nút mở túi đồ (UI)
    public RectTransform waterToolUI;    // Nút Nước
    public RectTransform ferToolUI;      // Nút Phân Bón
    public RectTransform pesToolUI;      // Nút Thuốc Sâu

    [Header("--- CĂN CHỈNH DIALOGUE ---")]
    [Tooltip("Khoảng cách đẩy xa ra khỏi mục tiêu (X: Ngang, Y: Dọc)")]
    public Vector2 dialogueOffset = new Vector2(120f, 130f);

    private Material holeMat;

    void Awake()
    {
        Instance = this;
        holeMat = Instantiate(holeOverlay.material);
        holeOverlay.material = holeMat;
    }

    void Update()
    {
        if (IsTutorialMode && tutorialManager.firefly.gameObject.activeSelf)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, tutorialManager.firefly.position);
            Vector2 viewportPos = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
            holeMat.SetVector("_Center", viewportPos);
        }
    }

    public void OnBagOpenedClicked()
    {
        if (IsTutorialMode) isBagOpened = true;
    }

    public void StartFarming()
    {
        IsTutorialMode = true;
        isBagOpened = isSeedPlanted = isWatered = isFertilized = isPesticided = false;

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(SkipTutorialPhase2);
        }

        StartCoroutine(FarmingRoutine());
    }

    // ==========================================
    // [THÊM MỚI] HÀM KÉO CAMERA VỀ Ô ĐẤT
    // ==========================================
    private void FocusCameraOnSoil()
    {
        if (Camera.main != null && tutorialSoil != null)
        {
            // Giữ nguyên trục Z của Camera, chỉ dời X và Y về phía ô đất
            Vector3 camPos = Camera.main.transform.position;
            Vector3 targetPos = new Vector3(tutorialSoil.position.x, tutorialSoil.position.y, camPos.z);

            // Kéo camera trượt mượt mà về vị trí đất trong 0.5 giây
            Camera.main.transform.DOMove(targetPos, 0.5f).SetEase(Ease.InOutQuad);
        }
    }

    public void FocusOnSoilDuringDrag()
    {
        if (!IsTutorialMode) return;

        // [SỬA LỖI OUT SCENE] Trượt Camera về lại ô đất ngay lập tức
        FocusCameraOnSoil();

        Vector3 soilScreenPos = Camera.main.WorldToScreenPoint(tutorialSoil.position);

        DOVirtual.Float(holeMat.GetFloat("_Radius"), 0.15f, 0.2f, (v) => holeMat.SetFloat("_Radius", v));
        tutorialManager.firefly.DOMove(soilScreenPos, 0.2f);

        raycastFilter.clickableRadius = 3000f;
        HideText();
    }

    IEnumerator FarmingRoutine()
    {
        holeOverlay.gameObject.SetActive(true);
        tutorialManager.firefly.gameObject.SetActive(true);
        tutorialManager.dialogueBox.gameObject.SetActive(true);
        tutorialManager.dialogueBox.localScale = Vector3.zero;

        if (skipButton != null) skipButton.gameObject.SetActive(true);

        holeMat.SetFloat("_Radius", 0f);
        tutorialManager.firefly.localPosition = new Vector3(-1500, -1500, 0);

        // Đưa Camera về ô đất ngay từ đầu để đảm bảo không bị lệch
        FocusCameraOnSoil();

        DOVirtual.Float(0f, 0.05f, 1f, (v) => holeMat.SetFloat("_Radius", v));
        yield return tutorialManager.firefly.DOLocalMove(Vector3.zero, 1f).SetEase(Ease.OutQuad).WaitForCompletion();

        // 0: LỜI CHÀO MỞ ĐẦU PHASE 2
        Vector3 centerScreen = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        yield return MoveFireflyAndOpenHole(centerScreen, 0.35f, 3000f);
        ShowText("Chào mừng đến với khu vườn! Bây giờ chúng ta sẽ học cách trồng cây nhé.", centerScreen);
        yield return new WaitForSeconds(3.5f);
        HideText();

        // 1. MỞ TÚI ĐỒ
        yield return MoveFireflyAndOpenHole(bagButtonUI.position, 0.08f, 150f);
        ShowText("Đầu tiên, hãy nhấp vào Túi Đồ để mở kho chứa nhé!", bagButtonUI.position);
        yield return new WaitUntil(() => isBagOpened);
        HideText();

        // 2. GIEO HẠT 
        yield return MoveFireflyAndOpenHole(centerScreen, 0.4f, 3000f);
        ShowText("Hãy nắm kéo 1 Hạt Giống từ túi đồ ra ngoài để gieo hạt.", centerScreen);
        yield return new WaitUntil(() => isSeedPlanted);
        HideText();

        // 3. TƯỚI NƯỚC
        yield return MoveFireflyAndOpenHole(waterToolUI.position, 0.06f, 150f);
        ShowText("Cây nảy mầm rồi! Kéo Bình Nước tưới cho cây nào.", waterToolUI.position);
        yield return new WaitUntil(() => isWatered);
        HideText();

        // 4. BÓN PHÂN
        yield return MoveFireflyAndOpenHole(ferToolUI.position, 0.06f, 150f);
        ShowText("Cây cần thêm dinh dưỡng. Hãy bón phân cho nó nhé.", ferToolUI.position);
        yield return new WaitUntil(() => isFertilized);
        HideText();

        // 5. THUỐC SÂU
        yield return MoveFireflyAndOpenHole(pesToolUI.position, 0.06f, 150f);
        ShowText("Bước cuối cùng, phun thuốc bảo vệ cây khỏi sâu bệnh!", pesToolUI.position);
        yield return new WaitUntil(() => isPesticided);
        HideText();

        // 6. THU HOẠCH
        FocusCameraOnSoil(); // Đảm bảo Camera đang nhìn vào ô đất trước khi soi sáng thu hoạch
        Vector3 soilScreenPos = Camera.main.WorldToScreenPoint(tutorialSoil.position);
        yield return MoveFireflyAndOpenHole(soilScreenPos, 0.12f, 2000f);
        ShowText("Cây đã chín! Nhấp vào nút 'Claim' để thu hoạch nhé.", soilScreenPos);
    }

    IEnumerator MoveFireflyAndOpenHole(Vector3 targetPos, float targetHoleRadius, float clickablePixelRadius)
    {
        DOVirtual.Float(holeMat.GetFloat("_Radius"), 0.04f, 0.4f, (v) => holeMat.SetFloat("_Radius", v));
        yield return tutorialManager.firefly.DOMove(targetPos, 1.2f).SetEase(Ease.InOutQuad).WaitForCompletion();
        tutorialManager.firefly.DOBlendableLocalMoveBy(new Vector3(0, 15, 0), 0.8f).SetLoops(-1, LoopType.Yoyo);
        DOVirtual.Float(0.04f, targetHoleRadius, 0.6f, (v) => holeMat.SetFloat("_Radius", v)).SetEase(Ease.OutBack);

        if (raycastFilter != null) raycastFilter.clickableRadius = clickablePixelRadius;
    }

    void ShowText(string msg, Vector3 targetScreenPos)
    {
        RectTransform dialogRect = tutorialManager.dialogueBox.GetComponent<RectTransform>();
        Vector2 newPivot = new Vector2(0.5f, 0.5f);
        Vector3 finalOffset = Vector3.zero;

        if (targetScreenPos.x < Screen.width * 0.4f) { newPivot.x = 0f; finalOffset.x = dialogueOffset.x; }
        else if (targetScreenPos.x > Screen.width * 0.6f) { newPivot.x = 1f; finalOffset.x = -dialogueOffset.x; }
        else { newPivot.x = 0.5f; finalOffset.x = 0f; }

        if (targetScreenPos.y > Screen.height * 0.6f) { newPivot.y = 1f; finalOffset.y = -dialogueOffset.y; }
        else { newPivot.y = 0f; finalOffset.y = dialogueOffset.y; }

        dialogRect.pivot = newPivot;
        tutorialManager.dialogueBox.position = targetScreenPos + finalOffset;
        tutorialManager.dialogueText.text = msg;
        tutorialManager.dialogueBox.DOScale(1, 0.4f).SetEase(Ease.OutBack);
    }

    void HideText() { tutorialManager.dialogueBox.DOScale(0, 0.2f).SetEase(Ease.InBack); }

    public void OnTutorialClaimed()
    {
        if (!IsTutorialMode) return;
        StartCoroutine(EndTutorialRoutine());
    }

    // ==========================================
    // [SỬA LỖI SKIP DÍNH FIREFLY] HÀM KẾT THÚC
    // ==========================================
    IEnumerator EndTutorialRoutine()
    {
        HideText();
        yield return new WaitForSeconds(0.2f);

        // Bắt buộc đom đóm bay ra giữa màn hình và mở lỗ sáng to dù là kết thúc tự nhiên hay Skip
        Vector3 centerScreen = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        yield return MoveFireflyAndOpenHole(centerScreen, 0.35f, 3000f);

        ShowText("Xuất sắc! Bạn đã tốt nghiệp khóa học làm nông.", centerScreen);

        yield return new WaitForSeconds(3f);
        HideText();
        yield return DOVirtual.Float(holeMat.GetFloat("_Radius"), 0f, 0.8f, (v) => holeMat.SetFloat("_Radius", v)).WaitForCompletion();

        tutorialManager.dialogueBox.gameObject.SetActive(false);
        tutorialManager.firefly.gameObject.SetActive(false);
        holeOverlay.gameObject.SetActive(false);

        if (skipButton != null) skipButton.gameObject.SetActive(false);
        tutorialManager.firefly.DOKill();

        IsTutorialMode = false;
        PlayerPrefs.SetInt(TutorialManager.GetTutorialSaveKey(), 1);
        PlayerPrefs.Save();
    }

    public void SkipTutorialPhase2()
    {
        if (!IsTutorialMode) return;

        StopAllCoroutines();
        tutorialManager.firefly.DOKill();
        tutorialManager.dialogueBox.DOKill();

        // Không tắt lỗ thủng về 0 nữa, để EndTutorialRoutine tự điều khiển bay ra giữa
        if (raycastFilter != null) raycastFilter.clickableRadius = 3000f;

        if (skipButton != null) skipButton.gameObject.SetActive(false);

        StartCoroutine(EndTutorialRoutine());
    }
}
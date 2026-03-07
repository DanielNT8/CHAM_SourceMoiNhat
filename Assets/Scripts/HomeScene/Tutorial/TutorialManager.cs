using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("--- UI & COMPONENTS ---")]
    public Image overlayImage;        // Panel đen
    public RectTransform firefly;     // Con đom đóm
    public TextMeshProUGUI dialogueText;         // Text thoại
    public GameObject guidebookPanel; // Sách hướng dẫn
    public Button skipButton;         // Nút Skip ở góc màn hình (MỚI)

    [Header("--- CẤU HÌNH ---")]
    public float flyDuration = 1.5f;  // Bay chậm lại xíu cho uốn lượn
    public float smallRadius = 0.05f;
    public float bigRadius = 0.2f;

    [System.Serializable]
    public struct TutorialStep
    {
        public RectTransform targetButton;
        [TextArea] public string message;
    }
    public TutorialStep[] steps;

    private Material holeMat;
    private bool isSkipping = false; // Cờ kiểm tra xem có đang skip không
    private Coroutine currentFlow;   // Lưu coroutine để stop khi cần

    void Start()
    {
        // 1. Setup Material
        holeMat = Instantiate(overlayImage.material);
        overlayImage.material = holeMat;

        // 2. Gán sự kiện nút Skip
        if (skipButton) skipButton.onClick.AddListener(OnSkipPressed);

        // 3. Kiểm tra Save Game: Chỉ chạy nếu chưa từng chơi
        // Key "TutorialPlayed" = 1 nghĩa là đã chơi rồi
        if (PlayerPrefs.GetInt("TutorialPlayed", 0) == 0)
        {
            PlayTutorial();
        }
        else
        {
            // Nếu chơi rồi thì ẩn hết
            DisableTutorialUI();
        }
    }

    void Update()
    {
        // Cập nhật shader lỗ thủng theo đom đóm
        if (overlayImage.gameObject.activeSelf)
        {
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, firefly.position);
            Vector2 viewportPos = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
            holeMat.SetVector("_Center", viewportPos);
        }
    }

    // --- HÀM PUBLIC ĐỂ GỌI TỪ MENU ---
    public void PlayTutorial()
    {
        // Reset trạng thái skip
        isSkipping = false;
        if (currentFlow != null) StopCoroutine(currentFlow);
        currentFlow = StartCoroutine(RunFlow());
    }

    // --- HÀM SỰ KIỆN NÚT SKIP ---
    void OnSkipPressed()
    {
        isSkipping = true; // Bật cờ skip

        // Dừng flow hiện tại
        if (currentFlow != null) StopCoroutine(currentFlow);

        // Kill tất cả tween đang chạy trên đom đóm để nó không bay lung tung nữa
        firefly.DOKill();

        // Chuyển thẳng sang đoạn kết
        StartCoroutine(EndSequence());
    }

    // --- LOGIC CHÍNH ---
    IEnumerator RunFlow()
    {
        // SETUP BAN ĐẦU
        overlayImage.gameObject.SetActive(true);
        skipButton.gameObject.SetActive(true); // Hiện nút Skip
        firefly.gameObject.SetActive(true);
        guidebookPanel.SetActive(false);

        holeMat.SetFloat("_Radius", 0); // Tối om

        // --- YÊU CẦU 2: BAY TỪ GÓC MÀN HÌNH RA ---
        // Đặt vị trí xuất phát ở góc dưới trái (ngoài màn hình một chút)
        firefly.position = new Vector3(-100, -100, 0);

        // Bay uốn lượn ra giữa màn hình
        Vector3 centerPos = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        yield return FlyCurved(centerPos); // Gọi hàm bay cong

        dialogueText.text = "Chào bạn! Mình là Đom Đóm.";
        yield return DOVirtual.Float(0, smallRadius, 1f, SetRadius).WaitForCompletion();
        yield return new WaitForSeconds(1f);

        // VÒNG LẶP CÁC BƯỚC
        foreach (var step in steps)
        {
            if (isSkipping) yield break; // Kiểm tra an toàn

            if (step.targetButton == null) continue;

            // 1. Thu nhỏ lỗ & Bay cong
            dialogueText.text = "...";
            DOVirtual.Float(holeMat.GetFloat("_Radius"), smallRadius, 0.5f, SetRadius);

            // --- YÊU CẦU 1: BAY UỐN LƯỢN (CURVED) ---
            yield return FlyCurved(step.targetButton.position);

            // 2. Highlight & Juice
            dialogueText.text = step.message;
            DOVirtual.Float(smallRadius, bigRadius, 0.5f, SetRadius);
            step.targetButton.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5, 1);

            yield return new WaitForSeconds(3f);
        }

        // KẾT THÚC BÌNH THƯỜNG
        yield return EndSequence();
    }

    // Hàm xử lý kết thúc (Dùng chung cho cả chạy thường và Skip)
    IEnumerator EndSequence()
    {
        dialogueText.text = "Giờ hãy xem bí kíp nhé!";

        // Thu nhỏ lỗ thủng về 0
        yield return DOVirtual.Float(holeMat.GetFloat("_Radius"), 0, 0.5f, SetRadius).WaitForCompletion();

        DisableTutorialUI();

        // Hiện Guidebook
        guidebookPanel.SetActive(true);

        // Lưu lại là đã chơi rồi
        PlayerPrefs.SetInt("TutorialPlayed", 1);
        PlayerPrefs.Save();
    }

    void DisableTutorialUI()
    {
        overlayImage.gameObject.SetActive(false);
        firefly.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);
    }

    void SetRadius(float x) => holeMat.SetFloat("_Radius", x);

    // --- HÀM LOGIC BAY CONG (BEZIER CURVE) ---
    IEnumerator FlyCurved(Vector3 targetPos)
    {
        Vector3 startPos = firefly.position;

        // Tính toán điểm trung gian (Control Point) để tạo độ cong
        // Logic: Lấy điểm giữa, cộng thêm một khoảng Random để nó lệch đi
        Vector3 midPoint = (startPos + targetPos) / 2;

        // Random độ lệch (-200 đến 200 pixel) để lúc cong lên lúc cong xuống
        float randomOffsetX = Random.Range(-200f, 200f);
        float randomOffsetY = Random.Range(-200f, 200f);
        midPoint += new Vector3(randomOffsetX, randomOffsetY, 0);

        // Dùng DOPath của DOTween với PathType.CatmullRom để đường cong mượt
        Vector3[] path = new Vector3[] { midPoint, targetPos };

        yield return firefly.DOPath(path, flyDuration, PathType.CatmullRom)
                            .SetEase(Ease.InOutSine) // Ease in out cho mượt
                            .WaitForCompletion();
    }
}
using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ItemClass))]
[RequireComponent(typeof(Collider2D))]
public class TreeGrowth : MonoBehaviour
{
    [Header("Định danh")]
    public string baseId = "TA";

    [Header("Cấp & XP")]
    [Range(0, 3)] public int level = 0;
    public int currentXP = 0;
    public int xpToLevel = 100;

    [Header("Sprites / Prefabs")]
    public GameObject SeedPrefab;
    public GameObject TreeLv1;
    public GameObject TreeLv2;
    public GameObject TreeLv3;

    [Header("UI Prefabs (Resources)")]
    public string claimButtonResourcePath = "Prefabs/Tree/TreeXPAndRelate/ClaimButton";
    public GameObject environmentCanvas;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip customTouchSound;

    [Header("UI Settings")]
    [SerializeField] private float uiWorldYOffset = 0.25f;

    private RectTransform collectionTargetUI;

    private TreeInfoUI infoUI;
    private RectTransform infoRect;
    private Button claimBtn;
    private ItemClass item;
    private SpriteRenderer sr;
    private bool isClaimReady = false;

    private long lastWT = 0, lastFR = 0, lastPS = 0;
    private const int WT_XP = 10, FR_XP = 40, PS_XP = 40;
    private static readonly TimeSpan WT_CD = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan FR_CD = TimeSpan.FromHours(1);
    private static readonly TimeSpan PS_CD = TimeSpan.FromHours(1);

    [Header("Raycast")]
    public LayerMask treeMask = -1;

    private Behaviour_Soil _soilBehaviour;

    public Func<TreeGrowth, bool> LevelUpRequirements;

    private void Awake()
    {
        item = GetComponent<ItemClass>();
        sr = GetComponentInChildren<SpriteRenderer>(true);
        LevelUpRequirements = (tree) => true;

        _soilBehaviour = GetComponentInParent<Behaviour_Soil>();

        if (_soilBehaviour == null)
            FindSoil();

        _soilBehaviour?.InitializeRequirements(this);

        if (!environmentCanvas)
            environmentCanvas = FindCanvasInScene();

        if (item != null && string.IsNullOrEmpty(item.itemId))
            item.itemId = $"{baseId}{level}";

        UpdateVisualForLevel();
    }

    private void FindSoil()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.1f);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Behaviour_Soil>(out var behaviour_Soil))
            {
                _soilBehaviour = behaviour_Soil;
                break;
            }
        }
    }

    private void Update()
    {
        HandleClick();
        UpdateUIPosition();
        RefreshTimersOnly();
    }

    private void HandleClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log(EventSystem.current.lastSelectedGameObject);
            return;
        }

        Camera cam = Camera.main;
        if (!cam) return;

        Vector2 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorldPos, treeMask.value);

        TreeGrowth chosen = null;
        int bestOrder = int.MinValue;

        foreach (var hit in hits)
        {
            var tg = hit.GetComponentInParent<TreeGrowth>();
            if (tg == null) continue;

            var rend = tg.sr ? tg.sr : tg.GetComponentInChildren<SpriteRenderer>(true);
            int order = rend ? rend.sortingOrder : 0;

            if (order >= bestOrder)
            {
                bestOrder = order;
                chosen = tg;
            }
        }

        if (chosen != null)
            chosen.OnTreeClicked();
        else
            infoUI?.Hide();
    }

    private void UpdateUIPosition()
    {
        if (environmentCanvas == null) return;

        Canvas canvasComp = environmentCanvas.GetComponent<Canvas>();
        if (canvasComp == null) return;

        Vector3 worldPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = environmentCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        Vector2 anchoredPos;
        if (canvasComp.renderMode == RenderMode.ScreenSpaceOverlay)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out anchoredPos);
        else
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvasComp.worldCamera, out anchoredPos);

        if (claimBtn != null)
        {
            RectTransform claimRect = claimBtn.GetComponent<RectTransform>();
            claimRect.anchoredPosition = anchoredPos + new Vector2(0, -120f);
        }
    }


    public void OnTreeClicked()
    {
        transform.DOPunchScale(Vector3.one * 0.05f, 0.2f, 10, 1f);

        PlayTouchSound();

        if (isClaimReady)
        {
            HideUI();
            ShowClaim(true);
            return;
        }

        infoUI = TreeInfoUI.Get();
        infoUI.Bind(this);
        infoUI.SetName(baseId).SetLevel(level).SetXP(currentXP, xpToLevel);
        infoRect = infoUI.RectTransform;
        infoUI.gameObject.SetActive(true);

        ShowClaim(false);

        bool active = !infoRect.gameObject.activeSelf;
        //infoRect.gameObject.SetActive(active);
        if (active) UpdateUIPosition();

        if (claimBtn != null)
            claimBtn.gameObject.SetActive(!active);
    }

#nullable enable
    public void ShowInfoUI(string? additionalMessage = null)
#nullable disable
    {
        infoUI = TreeInfoUI.Get();
        infoUI.Bind(this);
        infoUI.SetName(baseId).SetLevel(level).SetXP(currentXP, xpToLevel);

        if (additionalMessage != null)
        {
            infoUI.SetAndShowInfoMessage(additionalMessage);
        }
    }

    private void HideUI()
    {
        if (infoRect) infoUI.Hide();
        if (claimBtn) claimBtn.gameObject.SetActive(false);
    }

    public bool ApplyTool(string toolId)
    {
        if (level >= 3 && currentXP >= xpToLevel)
        {
            ShowClaim(true);
            return false;
        }

        int addXP = 0;
        bool canUse = false;
        long now = DateTimeOffset.UtcNow.Ticks;

        switch (toolId)
        {
            case "WT":
                canUse = (now - lastWT) >= WT_CD.Ticks;
                if (canUse)
                {
                    addXP = WT_XP; lastWT = now; FindAnyObjectByType<DailyTaskManage>()?.MarkWatered(); _soilBehaviour?.WaterSoil();
                }
                else
                {
                    ShowInfoUI($"Cây cần chờ thêm {Mathf.CeilToInt((WT_CD.Ticks - (now - lastWT)) / TimeSpan.TicksPerSecond)} giây để tưới nước tiếp.");
                }
                break;
            case "FR":
                canUse = (now - lastFR) >= FR_CD.Ticks;
                if (canUse)
                {
                    addXP = FR_XP; lastFR = now; FindAnyObjectByType<DailyTaskManage>()?.MarkFertilized();
                }
                else
                {
                    ShowInfoUI($"Cây cần chờ thêm {Mathf.CeilToInt((FR_CD.Ticks - (now - lastFR)) / TimeSpan.TicksPerSecond)} giây để sử dụng phân bón tiếp.");
                }
                break;
            case "PS":
                canUse = (now - lastPS) >= PS_CD.Ticks;
                if (canUse) { addXP = PS_XP; lastPS = now; _soilBehaviour?.CurePest(); }
                else
                {
                    ShowInfoUI($"Cây cần chờ thêm {Mathf.CeilToInt((PS_CD.Ticks - (now - lastPS)) / TimeSpan.TicksPerSecond)} giây để sử dụng thuốc trừ sâu tiếp.");
                }
                break;
        }

        if (!canUse)
        {
            infoRect?.DOShakeScale(0.2f, 0.1f, 10, 90f);
            return false;
        }

        infoUI = TreeInfoUI.Get();
        if (infoUI != null && infoUI.gameObject.activeInHierarchy)
        {
            infoUI.Bind(this);
            infoUI.SetAndShowInfoMessage(string.Empty);
        }
        GainXP(addXP);
        return true;
    }

    private void GainXP(int xp)
    {
        int from = currentXP;
        currentXP = Mathf.Clamp(currentXP + xp, 0, xpToLevel);
        infoUI?.TweenXP(from, currentXP, xpToLevel);

        if (currentXP >= xpToLevel && level < 3 && AllRequirementsMet())
        {
            currentXP -= xpToLevel;
            LevelUp();
        }
        else if (level >= 3 && currentXP >= xpToLevel)
        {
            ShowClaim(true);
        }
        else
            RefreshUI();
    }

    private void LevelUp()
    {
        level = Mathf.Clamp(level + 1, 0, 3);
        StartCoroutine(DoLevelUpTransition());
    }

    private bool AllRequirementsMet()
    {
        if (LevelUpRequirements != null)
        {
            foreach (var requirement in LevelUpRequirements.GetInvocationList())
                if (!((Func<TreeGrowth, bool>)requirement).Invoke(this))
                    return false;
        }
        return true;
    }

    private IEnumerator DoLevelUpTransition()
    {
        sr?.DOFade(0f, 0.3f);
        transform.DOScale(0.8f, 0.3f).SetEase(Ease.InOutSine);
        yield return new WaitForSeconds(0.35f);
        ReplaceWithPrefabForLevel(level);
    }

    private void ReplaceWithPrefabForLevel(int lv)
    {
        GameObject prefab = lv switch
        {
            0 => SeedPrefab,
            1 => TreeLv1,
            2 => TreeLv2,
            _ => TreeLv3
        };

        if (!prefab) return;

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;
        var parent = transform.parent;
        var oldXP = currentXP;
        var oldCanvas = environmentCanvas;
        var oldBaseId = baseId;

        Destroy(gameObject);

        var newTree = Instantiate(prefab, pos, rot, parent);
        var growth = newTree.GetComponent<TreeGrowth>() ?? newTree.AddComponent<TreeGrowth>();
        growth.level = lv;
        growth.currentXP = oldXP;
        growth.environmentCanvas = oldCanvas;
        growth.baseId = oldBaseId;
        growth.SeedPrefab = SeedPrefab;
        growth.TreeLv1 = TreeLv1;
        growth.TreeLv2 = TreeLv2;
        growth.TreeLv3 = TreeLv3;
        growth.claimButtonResourcePath = claimButtonResourcePath;
        growth.infoUI = this.infoUI;
        growth.infoUI?.SetLevel(level).SetXP(currentXP, xpToLevel).Hide();

        growth.sr = newTree.GetComponentInChildren<SpriteRenderer>(true);
        if (!growth.sr) growth.sr = newTree.AddComponent<SpriteRenderer>();
        growth.UpdateVisualForLevel();

        var targetScale = newTree.transform.localScale;
        newTree.transform.localScale = Vector3.zero;
        newTree.transform.DOScale(targetScale * 1.2f, 0.4f).SetEase(Ease.OutBack)
            .OnComplete(() => newTree.transform.DOScale(targetScale, 0.2f));
    }

    private void UpdateVisualForLevel()
    {
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>(true);

        GameObject src = level switch
        {
            0 => SeedPrefab,
            1 => TreeLv1,
            2 => TreeLv2,
            _ => TreeLv3
        };

        if (src)
        {
            var s = src.GetComponentInChildren<SpriteRenderer>(true);
            if (s)
            {
                if (!sr) sr = gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = s.sprite;
            }
        }

        if (item != null)
        {
            if (string.IsNullOrEmpty(item.itemId))
                item.itemId = $"{baseId}{level}";
            item.type = (level == 0 ? "Seed" : "Tree");
        }
    }

    private GameObject FindCanvasInScene()
    {
        const string targetName = "environmentCanvas";
        var canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        if (canvas != null)
        {
            if (canvas.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"✅ Found environmentCanvas: {canvas.name}");
                return canvas.gameObject;
            }

            var allCanvas = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allCanvas)
            {
                if (c.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"✅ Found environmentCanvas (secondary search): {c.name}");
                    return c.gameObject;
                }
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Không tìm thấy bất kỳ Canvas nào trong scene khi gọi FindCanvasInScene().");
        }

        Debug.LogWarning("⚠️ Không tìm thấy Canvas có tên 'environmentCanvas' trong scene. Hãy kiểm tra lại tên trong Hierarchy!");
        return null;
    }



    private void ShowClaim(bool show)
    {
        if (!environmentCanvas) return;
        Canvas canvasComp = environmentCanvas.GetComponent<Canvas>();
        if (!canvasComp) return;

        if (show)
        {
            isClaimReady = true;
            if (infoRect) infoRect.gameObject.SetActive(false); // Ẩn TreeInfo

            if (claimBtn == null)
            {
                var btnPrefab = Resources.Load<GameObject>(claimButtonResourcePath);
                if (!btnPrefab) return;

                var btnGO = Instantiate(btnPrefab, environmentCanvas.transform);
                claimBtn = btnGO.GetComponent<Button>();
                claimBtn.onClick.RemoveAllListeners();
                claimBtn.onClick.AddListener(Claim);
            }

            claimBtn.gameObject.SetActive(true);

            // Cập nhật vị trí của nút claim theo vị trí cây
            UpdateUIPosition();

            // Hiệu ứng rung nút claim
            claimBtn.transform.DOKill();
            claimBtn.transform.localScale = Vector3.one;
            claimBtn.transform.DOPunchScale(Vector3.one * 0.1f, 0.35f, 8, 0.8f)
                .SetLoops(-1, LoopType.Restart);
        }
        else
        {
            isClaimReady = false;
            if (claimBtn)
            {
                claimBtn.transform.DOKill();
                Destroy(claimBtn.gameObject);
                claimBtn = null;
            }
        }
    }



    private void Claim()
    {
        // 1. Ẩn nút Claim UI ngay lập tức
        if (claimBtn) claimBtn.gameObject.SetActive(false);
        infoUI?.Hide();

        // 2. Lưu lại các dữ liệu quan trọng trước khi ẩn cây
        Vector3 spawnPosition = transform.position; // Lưu vị trí cây đang đứng
        Sprite treeSprite = sr ? sr.sprite : null;  // Lưu hình ảnh cây để làm hiệu ứng bay

        var data = new ClaimedTreeData
        {
            sprite = treeSprite,
            name = baseId,
            level = level,
            itemId = item != null ? item.itemId : $"{baseId}{level}"
        };

        // 3. [QUAN TRỌNG] Ẩn cây ngay lập tức (Visual Disappearance)
        // Thay vì Destroy ngay, ta tắt SpriteRenderer và Collider để người chơi thấy cây biến mất
        if (sr) sr.enabled = false;

        // Tắt tất cả các sprite con (nếu cây có nhiều phần)
        foreach (var r in GetComponentsInChildren<SpriteRenderer>()) r.enabled = false;

        // Tắt va chạm để không click trúng cái cây "tàng hình"
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        // 4. Tìm đích đến (Túi đồ)
        if (collectionTargetUI == null)
        {
            GameObject bagObj = GameObject.Find("ConvertedTree");
            if (bagObj != null) collectionTargetUI = bagObj.GetComponent<RectTransform>();
        }

        // 5. Chạy hiệu ứng bay (Dùng spawnPosition đã lưu thay vì transform.position)
        if (ResourceFlyManager.Instance != null && collectionTargetUI != null)
        {
            ResourceFlyManager.Instance.PlayFromWorld(
                spawnPosition, // Vị trí cây (đã lưu)
                collectionTargetUI,   // Đích đến
                data.sprite,   // Hình ảnh bay
                1,             // Số lượng
                () => {
                    // 6. Callback: Bay xong mới xử lý data và Destroy thật
                    FinishClaim(data);
                }
            );
        }
        else
        {
            // Fallback
            FinishClaim(data);
        }
    }

    // Hàm xử lý logic nhận thưởng và xóa cây (Tách ra để gọi trong Callback)
    private void FinishClaim(ClaimedTreeData data)
    {
        // Cộng EXP cho User
        if (UserSession.currentUser != null)
        {
            UserSession.currentUser.currentExp += 50;
            int xpPerLevel = 100;
            if (UserSession.currentUser.currentExp >= xpPerLevel)
            {
                int levelUps = UserSession.currentUser.currentExp / xpPerLevel;
                UserSession.currentUser.level += levelUps;
                UserSession.currentUser.currentExp %= xpPerLevel;
            }
            var ui = FindFirstObjectByType<LoadUser>();
            if (ui != null) ui.RefreshUserUI();
        }

        // Thêm vào Collection
        if (PlantLogTabManage.Instance != null)
            PlantLogTabManage.Instance.AddToCollection(data);

        // Xóa các UI liên quan
        DOTween.KillAll(claimBtn);
        if (claimBtn) Destroy(claimBtn.gameObject);

        // Xóa cây
        Destroy(gameObject);
    }

    private void RefreshUI()
    {
        infoUI?.SetLevel(level);
        infoUI?.SetXP(currentXP, xpToLevel);
    }

    private void RefreshTimersOnly() { }


    private void PlayTouchSound()
    {
        if (AudioManager.Instance == null) return;

        // Logic: Nếu cây có tiếng riêng thì dùng, không thì dùng tiếng mặc định
        AudioClip clipToPlay = customTouchSound != null
                               ? customTouchSound
                               : AudioManager.Instance.defaultTreeTouchClip;

        // Phát tiếng (PlayOneShot để không bị ngắt quãng nếu click nhanh)
        AudioManager.Instance.PlaySFX(clipToPlay);
    }

}

[System.Serializable]
public struct ClaimedTreeData
{
    public Sprite sprite;
    public string name;
    public int level;
    public string itemId;
}
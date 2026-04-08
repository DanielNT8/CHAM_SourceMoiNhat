using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;


public class DragAndInteract : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Quantity of tools (UI hiển thị số lượng)")]
    public TMP_Text quantityFer = null; // phân bón (FR)
    public TMP_Text quantityPes = null; // thuốc trừ sâu (PS)
                                        // nước (WT) - thêm nếu bạn có

    [Header("Resources")]
    public string toolsResourcesFolder = "Tools";

    [Header("Audio Settings")]
    [SerializeField] private AudioClip actionSound;

    [Header("Raycast")]
    public LayerMask targetMask;

    private GameObject ghostTool;
    private string toolId;
    private Camera mainCam;

    private List<Canvas> hiddenCanvas = new();

    private void Awake()
    {
        mainCam = Camera.main;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        toolId = gameObject.tag; // WT, FR, PS

        // 🧩 Kiểm tra số lượng còn lại trước khi cho kéo
        if (!CanUseTool(toolId))
        {
            Debug.LogWarning($"[DragAndInteract] ❌ Hết {toolId}, không thể sử dụng!");
            return;
        }

        HideAllCanvas();

        // [THÊM MỚI] - Dời lỗ thủng ra ô đất để soi sáng cây
        if (FarmingTutorialController.IsTutorialMode)
        {
            FarmingTutorialController.Instance.FocusOnSoilDuringDrag();
        }

        var prefab = Resources.Load<GameObject>($"{toolsResourcesFolder}/{toolId}");
        if (prefab == null)
        {
            Debug.LogError($"Không tìm thấy prefab: Resources/{toolsResourcesFolder}/{toolId}");
            return;
        }

        ghostTool = Instantiate(prefab);
        var ic = ghostTool.GetComponent<ItemClass>() ?? ghostTool.AddComponent<ItemClass>();
        ic.itemId = toolId;
        ic.type = "Tool";

        ghostTool.transform.localScale = Vector3.zero;
        ghostTool.transform.position = ScreenToWorld(eventData.position);
        ghostTool.transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack);
        Cursor.visible = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostTool) ghostTool.transform.position = ScreenToWorld(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Cursor.visible = true;

        if (ghostTool == null)
        {
            RestoreAllCanvas();
            return;
        }

        Vector2 world = ScreenToWorld(eventData.position);
        var hits = Physics2D.OverlapPointAll(world, targetMask);

        //var hit = Physics2D.OverlapPoint(world, targetMask);

        if (hits != null && hits.Length > 0)
        {
            var growth = /*hit.GetComponentInParent<TreeGrowth>();*/ hits.FirstOrDefault(hit => hit.GetComponentInParent<TreeGrowth>())?.GetComponentInParent<TreeGrowth>();
            if (growth != null)
            {
                ghostTool.transform.DOMove(growth.transform.position, 0.25f)
                    .OnComplete(() =>
                    {
                        if (growth.ApplyTool(toolId))
                        {
                            ConsumeTool(toolId);

                            // [THÊM MỚI] Bắn tín hiệu hoàn thành từng bước
                            if (FarmingTutorialController.IsTutorialMode)
                            {
                                if (toolId == "WT") FarmingTutorialController.isWatered = true;
                                if (toolId == "FR") FarmingTutorialController.isFertilized = true;
                                if (toolId == "PS") FarmingTutorialController.isPesticided = true;
                            }
                        }

                        if (AudioManager.Instance != null && actionSound != null)
                        {
                            AudioManager.Instance.PlaySFX(actionSound);
                        }

                        ghostTool.transform.DOScale(0f, 0.15f).OnComplete(() => Destroy(ghostTool));
                    });
            }
            else
            {
                ghostTool.transform.DOScale(0f, 0.15f).OnComplete(() => Destroy(ghostTool));
            }
        }
        else
        {
            ghostTool.transform.DOScale(0f, 0.15f).OnComplete(() => Destroy(ghostTool));
        }

        RestoreAllCanvas();
    }

    private Vector3 ScreenToWorld(Vector2 screen)
    {
        var pos = mainCam.ScreenToWorldPoint(screen);
        pos.z = 0f;
        return pos;
    }

    private void HideAllCanvas()
    {
        hiddenCanvas.Clear();
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.enabled) continue;
            hiddenCanvas.Add(canvas);
            canvas.enabled = false;
        }
    }

    private void RestoreAllCanvas()
    {
        foreach (var canvas in hiddenCanvas)
        {
            if (canvas) canvas.enabled = true;
        }
        hiddenCanvas.Clear();
    }


    #region --- Tool Quantity Logic ---
    private bool CanUseTool(string toolId)
    {
        //Đang chạy Tutorial thì cho xài tool thoải mái (dù đang có 0 cái)
        if (FarmingTutorialController.IsTutorialMode) return true;

        TMP_Text targetText = GetToolText(toolId);
        if (targetText == null) return true;

        if (int.TryParse(targetText.text, out int qty))
        {
            if (qty <= 0)
            {
                targetText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.8f);
                return false;
            }
        }

        return true;
    }

    private void ConsumeTool(string toolId)
    {
        //Đang chạy Tutorial thì không trừ số lượng trên UI
        if (FarmingTutorialController.IsTutorialMode) return;

        TMP_Text targetText = GetToolText(toolId);
        if (targetText == null) return;

        if (int.TryParse(targetText.text, out int qty))
        {
            qty = Mathf.Max(0, qty - 1);
            targetText.text = qty.ToString();

            // 🔹 Nếu về 0 thì làm hiệu ứng rung cảnh báo
            if (qty == 0)
                targetText.transform.DOShakeScale(0.3f, 0.3f, 10, 90f);

            Debug.Log($"[DragAndInteract] 🔧 Dùng {toolId}, còn lại {qty}");
        }
    }

    private TMP_Text GetToolText(string toolId)
    {
        switch (toolId)
        {
            case "FR": return quantityFer;
            case "PS": return quantityPes;
            default: return null;
        }
    }
    #endregion
}

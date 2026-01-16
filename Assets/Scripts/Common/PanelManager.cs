using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PanelManager : MonoBehaviour
{
    [System.Serializable]
    public class PanelItem
    {
        public string panelName;       // Tên panel (Inventory, Settings, Shop, ...)
        public GameObject panelObject; // GameObject chứa panel
        public List<GameObject> childPanels; // 🆕 Panel con (gán trong Inspector)
    }

    public List<PanelItem> panels;
    public float fadeDuration = 0.25f;

    private Dictionary<string, CanvasGroup> panelDict = new Dictionary<string, CanvasGroup>();
    private Dictionary<string, List<CanvasGroup>> panelChildrenDict = new Dictionary<string, List<CanvasGroup>>();
    private string currentPanel = null;

    void Awake()
    {
        foreach (var p in panels)
        {
            if (p.panelObject != null)
            {
                // Lấy CanvasGroup của panel cha
                CanvasGroup cg = p.panelObject.GetComponent<CanvasGroup>();
                if (cg == null) cg = p.panelObject.AddComponent<CanvasGroup>();
                panelDict[p.panelName] = cg;
                SetPanelState(cg, false, true);

                // 🆕 Lưu danh sách panel con (nếu có)
                if (p.childPanels != null && p.childPanels.Count > 0)
                {
                    List<CanvasGroup> childGroups = new List<CanvasGroup>();
                    foreach (var child in p.childPanels)
                    {
                        if (child != null)
                        {
                            CanvasGroup childCg = child.GetComponent<CanvasGroup>();
                            if (childCg == null) childCg = child.AddComponent<CanvasGroup>();
                            SetPanelState(childCg, false, true);
                            childGroups.Add(childCg);
                        }
                    }
                    panelChildrenDict[p.panelName] = childGroups;
                }
            }
        }
    }

    // ==========================================================
    // ✅ HÀM MỚI: Hiện panel và các panel con của nó
    // ==========================================================
    public void ShowPanelWithChildren(string name)
    {
        if (!panelDict.ContainsKey(name)) return;

        // Ẩn tất cả panel KHÔNG PHẢI panel cha hoặc panel con của nó
        foreach (var kv in panelDict)
        {
            bool isParent = kv.Key == name;
            bool isChild = panelChildrenDict.ContainsKey(name) &&
                           panelChildrenDict[name].Exists(cg => cg.gameObject == kv.Value.gameObject);

            if (isParent || isChild)
                continue; // giữ nguyên
            else
                StartCoroutine(FadePanel(kv.Value, false)); // ẩn panel khác
        }

        // Hiện panel cha
        StartCoroutine(FadePanel(panelDict[name], true));

        // Hiện panel con
        if (panelChildrenDict.ContainsKey(name))
        {
            foreach (var childCg in panelChildrenDict[name])
            {
                StartCoroutine(FadePanel(childCg, true));
            }
        }

        currentPanel = name;
    }


    // 🆕 Ẩn panel và panel con của nó
    public void HidePanelWithChildren(string name)
    {
        if (panelDict.ContainsKey(name))
        {
            StartCoroutine(FadePanel(panelDict[name], false));
        }

        if (panelChildrenDict.ContainsKey(name))
        {
            foreach (var childCg in panelChildrenDict[name])
            {
                StartCoroutine(FadePanel(childCg, false));
            }
        }

        if (currentPanel == name)
            currentPanel = null;
    }

    // ==========================================================
    // Giữ nguyên các hàm cũ của bạn (không thay đổi)
    // ==========================================================

    public void ShowPanel(string name)
    {
        foreach (var kv in panelDict)
        {
            if (kv.Key == name) StartCoroutine(FadePanel(kv.Value, true));
            else StartCoroutine(FadePanel(kv.Value, false));
        }
        currentPanel = name;
    }

    public void HidePanel(string name)
    {
        if (panelDict.ContainsKey(name))
        {
            StartCoroutine(FadePanel(panelDict[name], false));
            if (currentPanel == name) currentPanel = null;
        }
    }

    public void TogglePanel(string name)
    {
        if (currentPanel == name)
            HidePanel(name);
        else
            ShowPanel(name);
    }

    private IEnumerator FadePanel(CanvasGroup cg, bool show)
    {
        float startAlpha = cg.alpha;
        float targetAlpha = show ? 1f : 0f;
        float time = 0f;

        if (show)
        {
            cg.gameObject.SetActive(true);
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        cg.alpha = targetAlpha;

        if (!show)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
            cg.gameObject.SetActive(false);
        }
    }

    private void SetPanelState(CanvasGroup cg, bool show, bool instant = false)
    {
        if (instant)
        {
            cg.alpha = show ? 1f : 0f;
            cg.interactable = show;
            cg.blocksRaycasts = show;
            cg.gameObject.SetActive(show);
        }
    }

    void Update()
    {
        if (currentPanel != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!IsPointerOverUIObject(panelDict[currentPanel].gameObject))
                {
                    HidePanel(currentPanel);
                }
            }
        }
    }

    private bool IsPointerOverUIObject(GameObject panelObj)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count == 0) return false;

        foreach (var r in results)
        {
            if (r.gameObject == panelObj || r.gameObject.transform.IsChildOf(panelObj.transform))
                return true;
        }
        return false;
    }

    public void ShowPanelOnly(string name)
    {
        List<string> subPanels = new List<string> { "RemoveAds", "CoinsPanel" };
        foreach (string panelName in subPanels)
        {
            if (panelDict.ContainsKey(panelName))
            {
                bool shouldShow = panelName == name;
                StartCoroutine(FadePanel(panelDict[panelName], shouldShow));
            }
        }
    }
}

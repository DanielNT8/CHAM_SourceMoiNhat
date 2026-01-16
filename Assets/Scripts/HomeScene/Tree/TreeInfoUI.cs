using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TreeInfoUI : MonoBehaviour
{
    private static TreeInfoUI _instance;

    [field: SerializeField] public RectTransform RectTransform { get; private set; }

    [SerializeField] private Canvas canvas;
    [SerializeField] private SlicedFilledImage xpBar;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI treeName;
    [SerializeField] private TextMeshProUGUI infoMessage;

    private TreeGrowth bound;

    private void OnEnable()
    {
        canvas.worldCamera = Camera.main;
    }

    private void OnDisable()
    {
        infoMessage.text = string.Empty;
        infoMessage.gameObject.SetActive(false);
    }

    public void Bind(TreeGrowth growth)
    {
        bound = growth;
        transform.position = growth.transform.position + new Vector3(4, 2);
        transform.localScale = Vector3.zero;
        infoMessage.text = string.Empty;
        infoMessage.gameObject.SetActive(false);
        transform.DOKill();
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }

    public void Hide(bool immediate = false)
    {
        if (immediate)
        {
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }
        else
        {
            transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => gameObject.SetActive(false));
        }
    }

    public TreeInfoUI SetName(string name)
    {
        treeName.text = name;
        return this;
    }

    public TreeInfoUI SetLevel(int lv)
    {
        levelText.text = lv.ToString();
        return this;
    }

    public TreeInfoUI SetXP(int current, int max)
    {
        xpBar.fillAmount = max <= 0 ? 0 : (float)current / max;
        xpText.text = $"{current}/{max}";
        return this;
    }

    public TreeInfoUI SetAndShowInfoMessage(string message)
    {
        infoMessage.gameObject.SetActive(true);
        infoMessage.text = message;
        return this;
    }

    public static TreeInfoUI Get()
    {
        if(_instance == null)
            _instance = Instantiate(Resources.Load<TreeInfoUI>("Prefabs/Tree/TreeXPAndRelate/TreeInfo"));

        return _instance;
    }

    public void TweenXP(int from, int to, int max)
    {
        if (!xpBar) return;
        float start = max <= 0 ? 0 : (float)from / max;
        float end = max <= 0 ? 0 : (float)to / max;

        DOTween.To(() => start, x => 
        {
            start = x;
            xpBar.fillAmount = x;
            int currentXP = Mathf.RoundToInt(x * max);
            xpText.text = $"{currentXP}/{max}";
        }, end, 1f).SetEase(Ease.OutCubic);
    }
}

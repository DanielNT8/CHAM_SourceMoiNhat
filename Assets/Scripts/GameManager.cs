using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-999)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private RectTransform PopupLoading;

    private TextMeshProUGUI _loadingDescriptionText;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowLoading(string description)
    {
        PopupLoading.gameObject.SetActive(true);
        _loadingDescriptionText ??= PopupLoading.GetComponentInChildren<TextMeshProUGUI>();
        _loadingDescriptionText.text = description;
    }

    public void HideLoading()
    {
        PopupLoading.gameObject.SetActive(false);
    }
}

using System.Collections.Generic;
using Outsource.Modules.AI.Scripts;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AiOptionPopupView : MonoBehaviour
{
    [field: SerializeField] public TextMeshProUGUI Content    { get; set; }
    [field: SerializeField] public List<ButtonAI>  ListButton { get; set; }
    [field: SerializeField] public Button          CloseBtn   { get; set; }
    [field: SerializeField] public Image           IconNoti   { get; set; }

    private IssueType     issueType;
    private BehaviourTree sender;

    private void Awake()
    {
        this.RegisterEvents();
    }

    private void Start()
    {
        this.IconNoti.gameObject.SetActive(false);
        this.ClosePopup();
    }

    private void RegisterEvents()
    {
        ManagerInGame_Weather.Instance.OnOpenAiOption += this.OpenPopup;
        this.CloseBtn.onClick.AddListener(this.ClosePopup);
        AIManager.Instance.HandleIssuse += this.HandleIssue;
        foreach (var btnAi in this.ListButton)
        {
            if (btnAi != null && btnAi.Button != null)
            {
                btnAi.Button.onClick.AddListener(() => this.OnOptionClicked(btnAi));
            }
        }
    }

    private void OpenPopup()
    {
        this.gameObject.SetActive(true);
        this.SetContent();
    }

    private void ClosePopup()
    {
        this.gameObject.SetActive(false);
    }

    private void SetContent()
    {
        this.Content.text = AIManager.Instance.DescriptionIssue;
    }

    private void OnOptionClicked(ButtonAI btnAi)
    {
        if (this.issueType == btnAi.IssueType)
        {
            IssueEventBus.ResolveIssue(this.sender, this.issueType);
            this.ClosePopup();
            this.HandleWhenDone();
        }
    }

    private void HandleIssue(BehaviourTree behaviourTree, IssueType issueType)
    {
        this.sender    = behaviourTree;
        this.issueType = issueType;
        this.IconNoti.gameObject.SetActive(true);
        this.OpenIconBtnNoti(issueType);
    }

    private void OpenIconBtnNoti(IssueType issueType)
    {
        if(this.ListButton.Count < 0) return;
        this.ListButton.ForEach(x => x.IconNoti.gameObject.SetActive(false));
        var buttonAi = this.ListButton.Find(x => x.IssueType == issueType);
        buttonAi.IconNoti.gameObject.SetActive(true);
    }

    private void CloseAllIconBtnNoti()
    {
        if(this.ListButton.Count < 0) return;
        this.ListButton.ForEach(x => x.IconNoti.gameObject.SetActive(false));
    }

    private void HandleWhenDone()
    {
        this.IconNoti.gameObject.SetActive(false);
        this.CloseAllIconBtnNoti();
    }
}
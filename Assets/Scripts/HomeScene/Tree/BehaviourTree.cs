using System;
using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Outsource.Modules.AI.Scripts;
using UnityEngine;
using Random = UnityEngine.Random;

public class BehaviourTree : MonoBehaviour
{
    [Header("Tree Config")]
    [field: SerializeField] private float minTimeIssue = 15f;
    [field: SerializeField] private float            maxTimeIssue = 30f;
    [field: SerializeField] private IssueType        issueType;
    [field: SerializeField] private OptionController optionController;
    [field: SerializeField] private GameObject       NotiIcon;
    [field: SerializeField] private SpriteRenderer   SpriteIssue;

    [SerializedDictionary("Issue Type", "Point Mask")]
    public SerializedDictionary<IssueType, Transform> ListPointMaskIssue;

    [Header("Sprite Issue")]
    [field: SerializeField] private Sprite spriteIssueA;
    [field: SerializeField] private Sprite spriteIssueB;
    [field: SerializeField] private Sprite spriteIssueC;
    [field: SerializeField] private Sprite spriteIssueD;

    private bool isIssueActive = false;
    private bool isSendIssue   = false;

    private void OnEnable()
    {
        IssueEventBus.OnResolveIssue += this.ResolveIssue;
    }

    private void Start()
    {
        this.StartCoroutine(this.IssueWorkflowRoutine());
        this.NotiIcon.SetActive(false);
        this.SpriteIssue.gameObject.SetActive(false);
    }

    private IEnumerator IssueWorkflowRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(this.minTimeIssue, this.maxTimeIssue);
            yield return new WaitForSeconds(waitTime);

            IssueType issueType = this.GetRandomIssueType();
            this.issueType = issueType;
            this.SetBehaviourTree(this.issueType);
            this.isIssueActive = true;
            this.NotiIcon.gameObject.SetActive(true);

            yield return new WaitUntil(() => !this.isIssueActive);
        }
    }

    private void SetBehaviourTree(IssueType issueType)
    {
        this.SpriteIssue.gameObject.SetActive(true);
        this.SpriteIssue.transform.position = this.ListPointMaskIssue[issueType].position;
        switch (issueType)
        {
            case IssueType.A:
                this.SpriteIssue.sprite = this.spriteIssueA;
                break;
            case IssueType.B:
                this.SpriteIssue.sprite = this.spriteIssueB;
                break;
            case IssueType.C:
                this.SpriteIssue.sprite = this.spriteIssueC;
                break;
            case IssueType.D:
                this.SpriteIssue.sprite = this.spriteIssueD;
                break;
        }
    }

    private void OnMouseDown()
    {
        if (this.isIssueActive && !this.isSendIssue)
        {
            IssueEventBus.Publish(this, this.issueType);
            this.isSendIssue = true;
            Debug.Log("Send Issue: " + this.issueType + "");
        }
    }

    private IssueType GetRandomIssueType()
    {
        Array values = Enum.GetValues(typeof(IssueType));
        return (IssueType)values.GetValue(Random.Range(1, values.Length));
    }

    public void ResolveIssue()
    {
        Debug.Log("Resolve Issue Done");
        this.isIssueActive = false;
        this.NotiIcon.SetActive(false);
        this.isSendIssue = false;
        this.SpriteIssue.gameObject.SetActive(false);
    }

    private void ResolveIssue(BehaviourTree behaviourTree, IssueType issueType)
    {
        if (behaviourTree == this)
        {
            this.optionController.Action(issueType, this.ResolveIssue);
        }
    }
}

[Serializable]
public enum IssueType
{
    None,
    A,
    B,
    C,
    D,
}
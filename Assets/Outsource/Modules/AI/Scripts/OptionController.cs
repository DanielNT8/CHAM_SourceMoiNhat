namespace Outsource.Modules.AI.Scripts
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using AYellowpaper.SerializedCollections;
    using DG.Tweening;
    using Sirenix.Utilities;
    using UnityEngine;

    public class OptionController : MonoBehaviour
    {
        [Header("OptionA Config")]
        [field: SerializeField] private Sprite          faceHappy;
        [field: SerializeField] private Sprite          perfume;
        [field: SerializeField] private List<Transform> ListPoint;
        [field: SerializeField] private GameObject      objectPerfome;

        [Header("OptionB Config")]
        [field: SerializeField] private Sprite          oxiTank;
        [field: SerializeField] private Sprite          faceFunny;
        [field: SerializeField] private List<Transform> ListPointOpB;
        [field: SerializeField] private GameObject      objectOxiTank;

        [Header("OptionC Config")]
        [field: SerializeField] private Sprite          bandage;
        [field: SerializeField] private Sprite          faceStrong;
        [field: SerializeField] private GameObject      ObjectBandage;
        [field: SerializeField] private List<Transform> ListPointOptionC;

        [Header("OptionD Config")]
        [field: SerializeField] private Sprite          hat;
        [field: SerializeField] private Sprite          faceSuper;
        [field: SerializeField] private GameObject      ObjectHat;
        [field: SerializeField] private List<Transform> ListPointOptionD;

        [Header("Main Config")]
        [field: SerializeField] private SpriteRenderer MainSprite;
        [field: SerializeField] private float moveSpeed = 3f;

        [SerializedDictionary("Issue Type", "Option")] public SerializedDictionary<IssueType, GameObject> ListOption;

        private void Start()
        {
            this.Init();
        }

        private void Init()
        {
            this.MainSprite.gameObject.SetActive(false);
            this.objectPerfome.SetActive(false);
            this.ListOption.ForEach(x => x.Value.SetActive(false));
        }

        public void Action(IssueType issueType, Action onComplete = null)
        {
            this.ClearMainSprite();
            this.MainSprite.gameObject.SetActive(true);
            var option = this.ListOption[issueType];
            option.SetActive(true);
            switch (issueType)
            {
                case IssueType.A:
                    this.PlayOptionA(onComplete);
                    break;
                case IssueType.B:
                    this.PlayOptionB(onComplete);
                    break;
                case IssueType.C:
                    this.PlayOptionC(onComplete);
                    break;
                case IssueType.D:
                    this.PlayOptionD(onComplete);
                    break;
            }
        }

        #region OptionA

        private void PlayOptionA(Action onComplete = null)
        {
            Debug.Log("A");
            this.objectPerfome.SetActive(true);
            this.objectPerfome.transform.position = this.ListPoint[0].position;
            this.StopAllCoroutines();
            this.StartCoroutine(this.MoveThroughPoints(onComplete));
        }

        private IEnumerator MoveThroughPoints(Action onComplete = null)
        {
            for (var i = 1; i < this.ListPoint.Count; i++)
            {
                var target = this.ListPoint[i].position;
                this.objectPerfome.transform.eulerAngles = Vector3.zero;
                while (Vector3.Distance(this.objectPerfome.transform.position, target) > 0.01f)
                {
                    yield return new WaitForSeconds(1f);
                    this.objectPerfome.transform.DOMove(target, 1f);
                    this.objectPerfome.transform.eulerAngles = new Vector3(0, -180f, 0);
                    yield return null;
                }
            }
            onComplete?.Invoke();
            yield return this.WhenObjectPerfumeMoveComplete();
        }

        private IEnumerator WhenObjectPerfumeMoveComplete()
        {
            this.MainSprite.sprite = this.faceHappy;
            this.objectPerfome.SetActive(false);
            yield return new WaitForSeconds(1f);
            this.MainSprite.gameObject.SetActive(false);
            this.ClearMainSprite();
            this.ListOption.ForEach(x => x.Value.SetActive(false));
        }

        #endregion

        #region OptionB

        private void PlayOptionB(Action onComplete = null)
        {
            Debug.Log("B");
            this.objectOxiTank.transform.position = this.ListPointOpB[0].position;
            var target = this.ListPointOpB[1].position;
            this.objectOxiTank.transform.DOMove(target, 1.5f).SetEase(Ease.Linear).OnComplete(() =>
            {
                onComplete?.Invoke();
                this.StartCoroutine(this.HandleWhenOxiTankMoveComplete());
            });
        }

        private IEnumerator HandleWhenOxiTankMoveComplete()
        {
            yield return new WaitForSeconds(0.1f);
            this.objectOxiTank.SetActive(false);
            this.MainSprite.sprite = this.faceFunny;
            yield return new WaitForSeconds(1f);
            this.MainSprite.gameObject.SetActive(false);
            this.ClearMainSprite();
        }
        #endregion

        #region OptionC

        private void PlayOptionC(Action onComplete = null)
        {
            Debug.Log("C");
            this.ObjectBandage.transform.position = this.ListPointOptionC[0].position;
            this.MoveToTarget(onComplete);
        }

        private void MoveToTarget(Action onComplete = null)
        {
            var target = this.ListPointOptionC[1].position;
            this.ObjectBandage.transform.DOJump(target, 2.5f, 1, 1.2f).SetEase(Ease.Linear).OnComplete((() =>
            {
                onComplete?.Invoke();
                this.StartCoroutine(this.HandleWhenBandageComplete());
            }));
        }

        private IEnumerator HandleWhenBandageComplete()
        {
            yield return new WaitForSeconds(0.1f);
            this.ObjectBandage.gameObject.SetActive(false);
            this.MainSprite.sprite = this.faceStrong;
            yield return new WaitForSeconds(1.5f);
            this.MainSprite.gameObject.SetActive(false);
            this.ClearMainSprite();
        }

        #endregion

        #region OptionD

        private void PlayOptionD(Action onComplete = null)
        {
            Debug.Log("D");
            this.ObjectHat.transform.position = this.ListPointOptionD[0].position;
            this.MoveToTargetPos(onComplete);
        }

        private void MoveToTargetPos(Action onComplete = null)
        {
            var target = this.ListPointOptionD[1].position;
            this.ObjectHat.transform.DOMove(target, 1.5f).SetEase(Ease.Linear).OnComplete(() =>
            {
                onComplete?.Invoke();
                this.StartCoroutine(this.HanderWhenDone());
            });
            this.ObjectHat.transform.DORotate(new Vector3(0, 0, 360), 1f, RotateMode.LocalAxisAdd);
        }

        private IEnumerator HanderWhenDone()
        {
            yield return new WaitForSeconds(0.1f);
            this.ObjectHat.gameObject.SetActive(false);
            this.MainSprite.sprite = this.faceSuper;
            yield return new WaitForSeconds(1.5f);
            this.MainSprite.gameObject.SetActive(false);
            this.ClearMainSprite();
        }

        #endregion


        private void ClearMainSprite()
        {
            this.MainSprite.sprite = null;
        }
    }
}
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransitionScene : MonoBehaviour
{
    public static TransitionScene Instance;

    [SerializeField] private List<Image> _leftWings;
    [SerializeField] private List<Image> _rightWings;

    public const string HOME_SCENE = "HomeScene";
    public const string GAME_SCENE = "GameScene";

    private Dictionary<Image, Vector2> _leftWingOriginalPos = new Dictionary<Image, Vector2>();
    private Dictionary<Image, Vector2> _rightWingOriginalPos = new Dictionary<Image, Vector2>();

    private void Awake()
    {
        Instance = this;

        foreach (var wing in _leftWings)
            _leftWingOriginalPos.Add(wing, wing.rectTransform.anchoredPosition);
        foreach (var wing in _rightWings)
            _rightWingOriginalPos.Add(wing, wing.rectTransform.anchoredPosition);
    }

    [Sirenix.OdinInspector.Button("Get all wings")]
    private void GetWings()
    {
        _leftWings = new List<Image>();
        _rightWings = new List<Image>();

        foreach (var wing in GetComponentsInChildren<Image>())
        {
            if (wing.rectTransform.anchoredPosition.x < 0)
                _leftWings.Add(wing);
            else
                _rightWings.Add(wing);
        }
    }

    public void TransitionToNewScene(string sceneName)
    {
        StartCoroutine(Transition(() => SceneManager.LoadScene(sceneName), null));
    }

    /// <summary>
    /// In case reusing the current scene, this method will transition the scene to itself
    /// </summary>
    public void TransitionCurrentScene(Action onHideCurrentSceneAction = null, float moveTime = 0.5f, float transitionTime = 0.5f)
    {
        StartCoroutine(Transition(onHideCurrentSceneAction, null, moveTime, transitionTime));
    }

    private IEnumerator Transition(Action onHideCurrentSceneAction, Action onShowNextSceneAction, float moveTime = 0.5f, float transitionTime = 0.5f)
    {
        yield return null;

        foreach (var wing in _leftWings)
        {
            wing.rectTransform.anchoredPosition = new Vector2(-Screen.width / 2 - 900f, _leftWingOriginalPos[wing].y);
            wing.gameObject.SetActive(true);
            wing.rectTransform.DOAnchorPos(_leftWingOriginalPos[wing], moveTime);
        }

        foreach (var wing in _rightWings)
        {
            wing.rectTransform.anchoredPosition = new Vector2(Screen.width / 2 + 900f, _rightWingOriginalPos[wing].y);
            wing.gameObject.SetActive(true);
            wing.rectTransform.DOAnchorPos(_rightWingOriginalPos[wing], moveTime);
        }

        yield return new WaitForSeconds(transitionTime);
        onHideCurrentSceneAction?.Invoke();

        yield return new WaitForSeconds(transitionTime);

        foreach (var wing in _leftWings)
            wing.rectTransform.DOAnchorPos(new Vector2(-Screen.width / 2 - 900f, _leftWingOriginalPos[wing].y), moveTime);

        foreach (var wing in _rightWings)
            wing.rectTransform.DOAnchorPos(new Vector2(Screen.width / 2 + 900f, _rightWingOriginalPos[wing].y), moveTime);

        onShowNextSceneAction?.Invoke();
    }
}

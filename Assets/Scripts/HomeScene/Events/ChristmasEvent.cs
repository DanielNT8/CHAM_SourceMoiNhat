using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChristmasEvent : SerializedMonoBehaviour
{
    public static ChristmasEvent Instance { get; private set; }

    private const string CHRISTMAS_EVENT_KEY = "ChristmasEventActive";

    [SerializeField] private Dictionary<Sprite, Sprite> _newSpriteByOldSprites;
    [SerializeField] protected List<SpriteRenderer> _level1Trees;
    [SerializeField] protected List<SpriteRenderer> _level2Trees;
    [SerializeField] protected List<SpriteRenderer> _level3Trees;
    [SerializeField] protected Sprite _level1ChristmasTree;
    [SerializeField] protected Sprite _level2ChristmasTree;
    [SerializeField] protected Sprite _level3ChristmasTree;

    [Space(10f)]
    [SerializeField] protected List<SpriteRenderer> _seeds;
    [SerializeField] protected Sprite _christmasSeed;

    private Dictionary<SpriteRenderer, Sprite> _initialLevel1Trees;
    private Dictionary<SpriteRenderer, Sprite> _initialLevel2Trees;
    private Dictionary<SpriteRenderer, Sprite> _initialLevel3Trees;
    private Dictionary<SpriteRenderer, Sprite> _initialSeeds;

    private SpriteRenderer[] _allSpriteRenderers;
    private Image[] _allImages;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _allSpriteRenderers = FindObjectsOfType<SpriteRenderer>();
        _allImages = FindObjectsOfType<Image>();

        _initialLevel1Trees = new();
        foreach (var tree in _level1Trees)
            _initialLevel1Trees[tree] = tree.sprite;

        _initialLevel2Trees = new();
        foreach (var tree in _level2Trees)
            _initialLevel2Trees[tree] = tree.sprite;

        _initialLevel3Trees = new();
        foreach (var tree in _level3Trees)
            _initialLevel3Trees[tree] = tree.sprite;

        _initialSeeds = new();
        foreach (var seed in _seeds)
            _initialSeeds[seed] = seed.sprite;

        if (PlayerPrefs.HasKey(CHRISTMAS_EVENT_KEY))
            ApplyChristmasTheme();
    }

    private void OnDestroy()
    {
        try
        {
            foreach (var tree in _level1Trees)
                tree.sprite = _initialLevel1Trees[tree];

            foreach (var tree in _level2Trees)
                tree.sprite = _initialLevel2Trees[tree];

            foreach (var tree in _level3Trees)
                tree.sprite = _initialLevel3Trees[tree];

            foreach (var seed in _seeds)
                seed.sprite = _initialSeeds[seed];
        }
        catch
        { }
    }

    public void ApplyChristmasTheme()
    {
        PlayerPrefs.SetString(CHRISTMAS_EVENT_KEY, "true");

        foreach (var spriteRenderer in _allSpriteRenderers)
        {
            if (_newSpriteByOldSprites.TryGetValue(spriteRenderer.sprite, out Sprite newSprite))
            {
                spriteRenderer.sprite = newSprite;
            }
        }

        foreach (var image in _allImages)
        {
            if (_newSpriteByOldSprites.TryGetValue(image.sprite, out Sprite newSprite))
            {
                image.sprite = newSprite;
                image.rectTransform.sizeDelta = GetFittedSize(image.rectTransform.sizeDelta, newSprite.bounds.size);
            }
        }

        foreach (var tree in _level1Trees)
        {
            tree.sprite = _level1ChristmasTree;
        }

        foreach (var tree in _level2Trees)
        {
            tree.sprite = _level2ChristmasTree;
        }

        foreach (var tree in _level3Trees)
        {
            tree.sprite = _level3ChristmasTree;
        }

        foreach (var seed in _seeds)
        {
            seed.sprite = _christmasSeed;
        }
    }

#if UNITY_EDITOR
    [Button]
    private void Reskin()
    {
        SpriteRenderer[] allSpriteRenderers = FindObjectsOfType<SpriteRenderer>();
        Image[] allImages = FindObjectsOfType<Image>();

        foreach (var spriteRenderer in allSpriteRenderers)
        {
            if (_newSpriteByOldSprites.TryGetValue(spriteRenderer.sprite, out Sprite newSprite))
            {
                spriteRenderer.sprite = newSprite;
            }
        }

        foreach (var image in allImages)
        {
            if (_newSpriteByOldSprites.TryGetValue(image.sprite, out Sprite newSprite))
            {
                image.sprite = newSprite;
                image.rectTransform.sizeDelta = GetFittedSize(image.rectTransform.sizeDelta, newSprite.bounds.size);
            }
        }
    }
#endif

    public static Vector2 GetFittedSize(Vector2 targetContainerSize, Vector2 originalImageSize)
    {
        float widthRatio = targetContainerSize.x / originalImageSize.x;
        float heightRatio = targetContainerSize.y / originalImageSize.y;

        float minScale = Mathf.Max(widthRatio, heightRatio);

        return originalImageSize * minScale;
    }
}

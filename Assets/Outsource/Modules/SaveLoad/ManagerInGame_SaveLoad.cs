using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class SaveGameSceneRequest
{
    [JsonProperty("userId")]
    public string UserId;

    [JsonProperty("status")]
    public string Status;

    [JsonProperty("dateSave")]
    public DateTime? DateSave;

    [JsonProperty("sceneDetails")]
    public List<SceneDetailDto> SceneDetails = new List<SceneDetailDto>();
}

[Serializable]
public class SceneDetailDto
{
    [JsonProperty("itemId")]
    public string ItemId;

    [JsonProperty("name")]
    public string Name;

    [JsonProperty("level")]
    public int? Level;

    [JsonProperty("expPerLevel")]
    public int? ExpPerLevel;

    [JsonProperty("positionX")]
    public double? PositionX;

    [JsonProperty("positionY")]
    public double? PositionY;
}

[Serializable]
public class GameSceneResponse
{
    [JsonProperty("userId")]
    public string UserId;

    [JsonProperty("status")]
    public string Status;

    [JsonProperty("dateSave")]
    public DateTime? DateSave;

    [JsonProperty("sceneDetails")]
    public List<SceneDetailDto> SceneDetails = new List<SceneDetailDto>();
}

[Serializable]
public class ApiResponse<T>
{
    [JsonProperty("status")]
    public string Status;

    [JsonProperty("data")]
    public T Data;
}

public class ManagerInGame_SaveLoad : MonoBehaviour
{
    public static ManagerInGame_SaveLoad Instance;

    private string _currentUserId;
    private const string BASE_URL = "https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net";
    private bool _allowQuitting = false;

    private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
    private Dictionary<int, string> _decorIds = new Dictionary<int, string>();

    private readonly string[] _resourceFolders = new string[]
    {
        "Prefabs/Decor/Chair", "Prefabs/Decor/Flower", "Prefabs/Decor/Frence",
        "Prefabs/Decor/Grass", "Prefabs/Decor/House", "Prefabs/Decor/Wood",
        "Prefabs/Tree/Seed", "Prefabs/Tree/SeedPackage", "Prefabs/Tree/TreeState",
        "Prefabs/Tree/TreeXPAndRelate"
    };

    private void Awake()
    {
        Instance = this;
        _currentUserId = (UserSession.currentUser != null && !string.IsNullOrEmpty(UserSession.currentUser.userId))
                         ? UserSession.currentUser.userId
                         : "test_user";

        Application.wantsToQuit += HandleWantsToQuit;
        LoadResourceCache();
    }

    private void Start()
    {
        Load();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) StartCoroutine(SaveCoroutine(null));
    }

    private bool HandleWantsToQuit()
    {
        if (_allowQuitting) return true;
        StartCoroutine(SaveAndQuitSequence());
        return false;
    }

    private IEnumerator SaveAndQuitSequence()
    {
        yield return SaveCoroutine(() => { });
        _allowQuitting = true;
        Application.Quit();
    }

    private void LoadResourceCache()
    {
        _prefabCache.Clear();
        foreach (var folder in _resourceFolders)
        {
            var prefabs = Resources.LoadAll<GameObject>(folder);
            foreach (var p in prefabs)
            {
                if (!_prefabCache.ContainsKey(p.name))
                    _prefabCache.Add(p.name, p);
            }
        }
    }

    [Button]
    public void Save()
    {
        StartCoroutine(SaveCoroutine(null));
    }

    private IEnumerator SaveCoroutine(Action onComplete)
    {
        SaveGameSceneRequest request = new SaveGameSceneRequest
        {
            UserId = _currentUserId,
            Status = "Active",
            DateSave = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        HashSet<string> usedItemIds = new HashSet<string>();

        var decors = FindObjectsOfType<DragDropPlacedDecor>();
        foreach (var decor in decors)
        {
            if (decor == null || decor.gameObject == null || decor.OriginalDecor) continue;

            int goId = decor.gameObject.GetInstanceID();
            string itemId;

            if (_decorIds.ContainsKey(goId))
            {
                itemId = _decorIds[goId];
            }
            else
            {
                itemId = decor.gameObject.name.Replace("(Clone)", "").Trim();
                _decorIds.Add(goId, itemId);
            }

            if (usedItemIds.Contains(itemId))
            {
                itemId = decor.gameObject.name.Replace("(Clone)", "").Trim();
                _decorIds[goId] = itemId;
            }
            usedItemIds.Add(itemId);

            string decorName = decor.gameObject.name.Replace("(Clone)", "").Trim();
            if (string.IsNullOrEmpty(decorName)) continue;

            request.SceneDetails.Add(new SceneDetailDto
            {
                ItemId = itemId,
                Name = decorName,
                Level = 0,
                ExpPerLevel = 0,
                PositionX = CheckDouble(decor.transform.position.x),
                PositionY = CheckDouble(decor.transform.position.y)
            });
        }

        var trees = FindObjectsOfType<TreeGrowth>();
        foreach (var tree in trees)
        {
            if (tree == null || tree.gameObject == null) continue;

            if (string.IsNullOrEmpty(tree.baseId))
            {
                tree.baseId = tree.gameObject.name.Replace("(Clone)", "").Trim();
            }

            if (usedItemIds.Contains(tree.baseId))
            {
                tree.baseId = tree.gameObject.name.Replace("(Clone)", "").Trim();
            }
            usedItemIds.Add(tree.baseId);

            string treeName = tree.gameObject.name.Replace("(Clone)", "").Trim();
            if (string.IsNullOrEmpty(treeName)) continue;

            request.SceneDetails.Add(new SceneDetailDto
            {
                ItemId = tree.baseId,
                Name = treeName,
                Level = Mathf.Max(0, tree.level),
                ExpPerLevel = Mathf.Max(0, tree.currentXP),
                PositionX = CheckDouble(tree.transform.position.x),
                PositionY = CheckDouble(tree.transform.position.y)
            });
        }

        string jsonBody = JsonConvert.SerializeObject(request);
        Debug.Log($"[SaveLoad] Sending data: {jsonBody}");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest webRequest = new UnityWebRequest($"{BASE_URL}/api/save", "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SaveLoad] Error: {webRequest.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"[SaveLoad] Save successful: {webRequest.downloadHandler.text}");
            }
        }

        onComplete?.Invoke();
    }

    private double CheckDouble(float val) => float.IsNaN(val) ? 0.0 : (double)val;

    [Button]
    public void Load()
    {
        StartCoroutine(LoadCoroutine());
    }

    private IEnumerator LoadCoroutine()
    {
        string url = $"{BASE_URL}/api/load/{_currentUserId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    ApiResponse<GameSceneResponse> response =
                        JsonConvert.DeserializeObject<ApiResponse<GameSceneResponse>>(request.downloadHandler.text);

                    if (response != null && response.Data != null && response.Data.SceneDetails.Count > 0)
                    {
                        ClearScene();
                        ProcessLoadedData(response.Data);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                }
            }
            else
            {
                Debug.LogWarning(request.error);
            }
        }
    }

    private void ProcessLoadedData(GameSceneResponse sceneData)
    {
        _decorIds.Clear();

        foreach (var detail in sceneData.SceneDetails)
        {
            if (_prefabCache.TryGetValue(detail.Name, out GameObject prefab))
            {
                float posX = detail.PositionX.HasValue ? (float)detail.PositionX.Value : 0f;
                float posY = detail.PositionY.HasValue ? (float)detail.PositionY.Value : 0f;

                GameObject obj = Instantiate(prefab, new Vector3(posX, posY, 0), Quaternion.identity);

                TreeGrowth treeComp = obj.GetComponent<TreeGrowth>();
                if (treeComp != null)
                {
                    treeComp.baseId = detail.ItemId;
                    treeComp.level = detail.Level ?? 0;
                    treeComp.currentXP = detail.ExpPerLevel ?? 0;
                    treeComp.SendMessage("UpdateVisualForLevel", SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    int goId = obj.GetInstanceID();
                    if (!string.IsNullOrEmpty(detail.ItemId))
                    {
                        _decorIds[goId] = detail.ItemId;
                    }
                }
            }
        }
    }

    private void ClearScene()
    {
        foreach (var d in FindObjectsOfType<DragDropPlacedDecor>()) 
            if(!d.OriginalDecor)
                Destroy(d.gameObject);
        foreach (var t in FindObjectsOfType<TreeGrowth>()) Destroy(t.gameObject);
    }
}
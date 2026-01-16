using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SaveBag : MonoBehaviour
{
    [SerializeField] private BagController bagController;

    private string apiUrl = "https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/Category/SaveCategory";

    [Header("Auto Save Settings")]
    public bool enableAutoSave = true;
    public float autoSaveInterval = 180f;

    private Coroutine autoSaveCoroutine;
    private bool isSaving = false;

    void Start()
    {
        if (enableAutoSave)
            autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            if (bagController != null && bagController.IsReady())
            {
                Debug.Log("🕒 Auto-saving bag data...");
                yield return SaveBagCoroutine();
            }
        }
    }

    public void SaveBagToServer()
    {
        if (!isSaving)
            StartCoroutine(SaveBagCoroutine());
    }

    private void OnApplicationQuit()
    {
        Debug.Log("💾 OnApplicationQuit → Lưu túi trước khi thoát game...");
        SaveBagToServer();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Debug.Log("📴 App bị tạm dừng → Lưu túi an toàn...");
            SaveBagToServer();
        }
    }

    private IEnumerator SaveBagCoroutine()
    {
        if (isSaving) yield break;
        isSaving = true;

        var user = UserSession.currentUser;
        if (user == null || string.IsNullOrEmpty(user.userId))
        {
            Debug.LogWarning("⚠ Không có userId, không thể lưu lên server.");
            isSaving = false;
            yield break;
        }

        List<LoadBag.BagItemData> currentBag = bagController.GetCurrentBagData();
        if (currentBag == null || currentBag.Count == 0)
        {
            Debug.Log("📦 Túi trống, không cần lưu.");
            isSaving = false;
            yield break;
        }

        List<CateDAO> cateList = new List<CateDAO>();
        foreach (var item in currentBag)
        {
            cateList.Add(new CateDAO
            {
                itemId = item.itemId ?? string.Empty,
                quantity = item.quantity
            });
        }

        string jsonBody = JsonHelper.ToJson(cateList.ToArray(), false);
        string fullUrl = $"{apiUrl}?userId={user.userId}";

        Debug.Log($"📤 Sending to {fullUrl}\nBody: {jsonBody}");

        using (UnityWebRequest www = new UnityWebRequest(fullUrl, "PUT"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool isNetworkError = www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError;
#else
            bool isNetworkError = www.isNetworkError || www.isHttpError;
#endif

            if (!isNetworkError)
            {
                Debug.Log($"✅ Túi đã được lưu lên server thành công! Status: {www.responseCode}");
                Debug.Log("📨 Server response: " + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"❌ Không thể lưu túi lên server: {www.error} (code: {www.responseCode})");
            }
        }

        isSaving = false;
    }

    [System.Serializable]
    public class CateDAO
    {
        public string itemId;
        public int quantity;
    }
}

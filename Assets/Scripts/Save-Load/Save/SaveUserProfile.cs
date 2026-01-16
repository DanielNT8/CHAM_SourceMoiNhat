using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class SaveUserProfile : MonoBehaviour
{
    [Header("User Information to save")]
    public TMP_Text currentLevel;
    public TMP_Text exp;
    public TMP_Text coins;

    private string baseUrl = "https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/User/UpdateUser";

    [Header("Auto Save Settings")]
    public bool enableAutoSave = true;
    public float autoSaveInterval = 120f; // auto-save mỗi 2 phút

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
            yield return SaveUserProfileCoroutine();
        }
    }

    public void SaveNow()
    {
        if (!isSaving)
            StartCoroutine(SaveUserProfileCoroutine());
    }

    private void OnApplicationQuit()
    {
        Debug.Log("💾 OnApplicationQuit → Lưu user profile trước khi thoát...");
        SaveNow();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Debug.Log("📴 App bị tạm dừng → lưu user profile...");
            SaveNow();
        }
    }

    private IEnumerator SaveUserProfileCoroutine()
    {
        if (isSaving) yield break;
        isSaving = true;

        var user = UserSession.currentUser;
        if (user == null || string.IsNullOrEmpty(user.userId))
        {
            Debug.LogWarning("⚠ Không có userId, không thể lưu user profile lên server.");
            isSaving = false;
            yield break;
        }

        var exps = exp.text.Split('/');

        UserDAO data = new UserDAO
        {
            userId = user.userId,
            username = "string",
            password = "string",
            email = "string",
            memberTypeId = "string",
            level = int.TryParse(currentLevel.text, out int lvl) ? lvl : 0,
            currentExp = int.TryParse(exps[0], out int currExp) ? currExp : 0,
            expPerLevel = int.TryParse(exps[1], out int expPerLevel) ? expPerLevel : 0,
            coin = int.TryParse(coins.text, out int coinVal) ? coinVal : 100,
            status = "string"
        };

        string jsonBody = JsonUtility.ToJson(data, true);
        Debug.Log($"📤 Sending to {baseUrl}\nBody: {jsonBody}");

        using (UnityWebRequest www = new UnityWebRequest(baseUrl, "PUT"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool isError = www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError;
#else
            bool isError = www.isNetworkError || www.isHttpError;
#endif

            if (!isError)
            {
                Debug.Log($"✅ User profile saved successfully! ({www.responseCode})");
            }
            else
            {
                Debug.LogError($"❌ Save failed: {www.error}\nResponse: {www.downloadHandler.text}");
            }
        }

        isSaving = false;
    }

    [System.Serializable]
    public class UserDAO
    {
        public string userId;
        public string username;
        public string password;
        public string email;
        public string memberTypeId;
        public int level;
        public int currentExp;
        public int expPerLevel;
        public int coin;
        public string status;
    }
}

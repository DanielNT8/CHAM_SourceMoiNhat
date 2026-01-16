using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LoadBag : MonoBehaviour
{
    [System.Serializable]
    public class BagItemData
    {
        public string userId;
        public string itemId;
        public int quantity;
        public Sprite icon;
    }

    [Header("API Settings")]
    private string Url = "https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/Category/CategoryByUserId?userId=";

    // ✅ Load túi từ server
    public IEnumerator LoadBagData(System.Action<List<BagItemData>> onSuccess, System.Action<string> onError)
    {
        var user = UserSession.currentUser;
        if (user == null || string.IsNullOrEmpty(user.userId))
        {
            onError?.Invoke("⚠ Không có userId hợp lệ.");
            yield break;
        }

        string api = Url + user.userId;
        Debug.Log($"🌐 Đang tải túi từ API: {api}");

        using (UnityWebRequest www = UnityWebRequest.Get(api))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool isNetworkError = www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError;
#else
            bool isNetworkError = www.isNetworkError || www.isHttpError;
#endif

            if (!isNetworkError)
            {
                try
                {
                    string json = www.downloadHandler.text;
                    BagItemData[] items = JsonHelper.FromJson<BagItemData>(json);

                    List<BagItemData> validItems = new List<BagItemData>();
                    foreach (var i in items)
                    {
                        if (i != null && i.quantity > 0)
                            validItems.Add(i);
                    }

                    Debug.Log($"✅ Đã tải thành công {validItems.Count} item từ API!");
                    onSuccess?.Invoke(validItems);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("❌ Parse JSON lỗi: " + ex.Message);
                    onError?.Invoke("Parse JSON error: " + ex.Message);
                }
            }
            else
            {
                onError?.Invoke($"⚠ Không thể tải từ API: {www.error}");
            }
        }
    }
}

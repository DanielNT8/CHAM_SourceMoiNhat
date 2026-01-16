using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class SaveTools : MonoBehaviour
{
    [Header("Set qty of tools")]
    public TMP_Text qtyFer; // Số lượng phân bón
    public TMP_Text qtyPes; // Số lượng thuốc trừ sâu

    private string API = "https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/User/SaveUserTools";
    private bool isSaving = false;

    // 🧩 Gọi khi app thoát hoàn toàn
    private void OnApplicationQuit()
    {
        Debug.Log("💾 OnApplicationQuit → Lưu tools trước khi thoát game...");
        SaveImmediately(); // Gửi blocking (chờ hoàn tất)
    }

    // 💤 Gọi khi app bị tạm dừng (Android/iOS)
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Debug.Log("📴 App bị tạm dừng → Lưu tools an toàn...");
            SaveImmediately();
        }
    }

    // 🪣 Gửi dữ liệu ngay lập tức (blocking)
    private void SaveImmediately()
    {
        if (UserSession.currentUser == null)
        {
            Debug.LogWarning("⚠ Không có thông tin user, bỏ qua lưu.");
            return;
        }

        if (!int.TryParse(qtyFer.text, out int qtyFr))
        {
            Debug.LogWarning("⚠ Giá trị phân bón không hợp lệ!");
            return;
        }

        if (!int.TryParse(qtyPes.text, out int qtyPs))
        {
            Debug.LogWarning("⚠ Giá trị thuốc trừ sâu không hợp lệ!");
            return;
        }

        string userId = UserSession.currentUser.userId;
        string url = $"{API}?userId={userId}&qtyFr={qtyFr}&qtyPs={qtyPs}";

        Debug.Log($"🔗 Gửi request: {url}");
        using (UnityWebRequest request = UnityWebRequest.Put(url, ""))
        {
            request.SetRequestHeader("Content-Type", "application/json");

            var asyncOp = request.SendWebRequest();

            // 🕒 Chờ blocking tối đa 2 giây để request hoàn tất
            float startTime = Time.realtimeSinceStartup;
            while (!asyncOp.isDone && Time.realtimeSinceStartup - startTime < 2f)
            {
                // giữ cho Unity sống thêm chút để gửi xong request
            }

#if UNITY_2020_1_OR_NEWER
            bool isError = request.result == UnityWebRequest.Result.ConnectionError ||
                           request.result == UnityWebRequest.Result.ProtocolError;
#else
            bool isError = request.isNetworkError || request.isHttpError;
#endif

            if (isError)
            {
                Debug.LogError($"❌ Lưu thất bại: {request.error} (code: {request.responseCode})");
            }
            else
            {
                Debug.Log("✅ Tools saved successfully (blocking send)!");
            }
        }
    }

    // 🧠 Lưu theo coroutine khi gọi thủ công trong runtime
    public void SaveToolsToServer()
    {
        if (isSaving) return;
        if (UserSession.currentUser == null)
        {
            Debug.LogWarning("⚠ Không có thông tin user, không thể lưu tools.");
            return;
        }

        if (!int.TryParse(qtyFer.text, out int qtyFr))
        {
            Debug.LogWarning("⚠ Giá trị phân bón không hợp lệ!");
            return;
        }

        if (!int.TryParse(qtyPes.text, out int qtyPs))
        {
            Debug.LogWarning("⚠ Giá trị thuốc trừ sâu không hợp lệ!");
            return;
        }

        string userId = UserSession.currentUser.userId;
        StartCoroutine(SaveToolsAPI(userId, qtyFr, qtyPs));
    }

    private IEnumerator SaveToolsAPI(string userId, int qtyFr, int qtyPs)
    {
        isSaving = true;
        string url = $"{API}?userId={userId}&qtyFr={qtyFr}&qtyPs={qtyPs}";

        using (UnityWebRequest request = UnityWebRequest.Put(url, ""))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool isError = request.result == UnityWebRequest.Result.ConnectionError ||
                           request.result == UnityWebRequest.Result.ProtocolError;
#else
            bool isError = request.isNetworkError || request.isHttpError;
#endif

            if (isError)
            {
                Debug.LogError($"❌ Failed to save tools: {request.error} (code: {request.responseCode})");
            }
            else
            {
                Debug.Log("✅ Tools saved successfully via coroutine!");
            }
        }

        isSaving = false;
    }
}

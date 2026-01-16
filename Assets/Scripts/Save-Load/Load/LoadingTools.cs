using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class ToolData
{
    public string userId;
    public string toolsId;
    public int quantity;
}

public class LoadingTools : MonoBehaviour
{
    [Header("Prefabs Tools (chứa TMP_Text bên trong)")]
    public TMP_Text fertilizer;   // Tool có ID = FR
    public TMP_Text pesticides;   // Tool có ID = PS

    void Start()
    {
        LoadToolsFromApi();
    }

    public void LoadToolsFromApi()
    {
        string userId = UserSession.currentUser.userId;
        string api = $"https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/User/GetUserTool?userId={userId}";
        StartCoroutine(LoadToolsCoroutine(api));
    }

    private IEnumerator LoadToolsCoroutine(string api)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(api))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                ToolData[] tools = JsonHelper.FromJson<ToolData>(json);

                int qtyFr = 0;
                int qtyPs = 0;

                foreach (ToolData tool in tools)
                {
                    if (tool.toolsId == "FR")
                        qtyFr = tool.quantity;
                    else if (tool.toolsId == "PS")
                        qtyPs = tool.quantity;
                }

                // Cập nhật text bên trong prefab
                if (fertilizer != null)
                {
                    fertilizer.text = qtyFr.ToString();
                }

                if (pesticides != null)
                {
                    pesticides.text = qtyPs.ToString();
                }

                Debug.Log($"✅ Load tools thành công: FR={qtyFr}, PS={qtyPs}");
            }
            else
            {
                Debug.LogError($"❌ Lỗi tải API: {request.error}");
            }
        }
    }
}
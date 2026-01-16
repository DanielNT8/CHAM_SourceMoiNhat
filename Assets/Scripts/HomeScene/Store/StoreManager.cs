using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

#region Payment DTOs
[System.Serializable]
public class PaymentItem
{
    public string name;
    public int quantity;
    public int price;

    public PaymentItem(string name, int quantity, int price)
    {
        this.name = name;
        this.quantity = quantity;
        this.price = price;
    }
}

[System.Serializable]
public class PaymentRequest
{
    public string userId;
    public int amount;
    public string description;
    public List<PaymentItem> items;
    public string returnUrl;
    public string cancelUrl;
}

[System.Serializable]
public class PaymentResponse
{
    public string status;
    public PaymentResponseData data;
}

[System.Serializable]
public class PaymentResponseData
{
    public string OrderId;
    public string PaymentUrl;
    public string TransactionId;
    public int Amount;
    public string Status;
    public string CreatedAt;
}

[System.Serializable]
public class AltPaymentResponse
{
    public string status;
    public AltPaymentResponseData data;
}

[System.Serializable]
public class AltPaymentResponseData
{
    public string orderId;
    public string paymentUrl;
    public string transactionId;
    public int amount;
    public string status;
    public string createdAt;
}
#endregion

public class StoreManager : MonoBehaviour
{
    [Header("API Endpoint")]
    private string API = "https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/Payment/create-order";
    private string USER_API = "https://localhost:7293/api/User/GetUserById?userId=";

    [Header("UI Setup")]
    public TMP_Text statusText;

    [Header("Coin Buttons")]
    public Button[] coinButtons;
    public int[] coinAmounts = { 500, 1600, 3500, 7500, 20000 };
    public int[] prices = { 10000, 23000, 33000, 67000, 132000 };

    private void Start()
    {
        for (int i = 0; i < coinButtons.Length; i++)
        {
            int index = i;
            coinButtons[i].onClick.AddListener(() =>
            {
                OnBuyCoins(coinAmounts[index], prices[index]);
            });
        }
    }

    public void OnBuyCoins(int coins, int amount)
    {
        string userId = UserSession.currentUser.userId;
        Debug.Log($"🪙 [StoreManager] User {userId} chọn mua {coins} coins với giá {amount}.");

        PaymentRequest request = new PaymentRequest
        {
            userId = userId,
            amount = amount,
            description = "Coins",
            items = new List<PaymentItem>
            {
                new PaymentItem("Coins", coins, 0)
            },
            returnUrl = "https://www.eco.info.vn",
            cancelUrl = "https://www.eco.info.vn"
        };

        StartCoroutine(CallCreateOrderAPI(request));
    }

    private IEnumerator CallCreateOrderAPI(PaymentRequest request)
    {
        string json = JsonUtility.ToJson(request, true);
        Debug.Log("📦 [StoreManager] Sending Payment Request JSON:\n" + json);

        using (UnityWebRequest www = new UnityWebRequest(API, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("🚀 [StoreManager] Sending POST to: " + API);
            yield return www.SendWebRequest();
            Debug.Log("✅ [StoreManager] Response received.");

            string responseText = www.downloadHandler.text;
            Debug.Log($"📥 [StoreManager] Raw Response: {responseText}");

            if (www.result == UnityWebRequest.Result.Success)
            {
                string checkoutUrl = "";

                try
                {
                    var response = JsonUtility.FromJson<PaymentResponse>(responseText);
                    if (response != null && response.data != null && !string.IsNullOrEmpty(response.data.PaymentUrl))
                        checkoutUrl = response.data.PaymentUrl;
                    else
                    {
                        var altResponse = JsonUtility.FromJson<AltPaymentResponse>(responseText);
                        if (altResponse != null && altResponse.data != null)
                            checkoutUrl = altResponse.data.paymentUrl;
                    }
                }
                catch
                {
                    Debug.LogWarning("⚠️ [StoreManager] JsonUtility parse thất bại, fallback sang tìm chuỗi URL trong response.");
                    int start = responseText.IndexOf("http");
                    if (start != -1)
                    {
                        int end = responseText.IndexOf('"', start);
                        if (end > start)
                            checkoutUrl = responseText.Substring(start, end - start);
                    }
                }

                if (!string.IsNullOrEmpty(checkoutUrl))
                {
                    Debug.Log("🌐 [StoreManager] Opening Payment URL: " + checkoutUrl);
                    if (statusText != null)
                        statusText.text = "Redirecting to checkout page...";
                    Application.OpenURL(checkoutUrl);

                    // ✅ Gọi API reload user ngay sau đó
                    StartCoroutine(ReloadUserData());
                }
                else
                {
                    Debug.LogError(" [StoreManager] Không tìm thấy PaymentUrl trong response!");
                    if (statusText != null)
                        statusText.text = "Payment link not found!";
                }
            }
            else
            {
                Debug.LogError("❌ [StoreManager] API error: " + www.error);
                Debug.LogError(responseText);
                if (statusText != null)
                    statusText.text = "Error creating payment: " + www.error;
            }
        }
    }

    // ✅ Gọi API load lại user sau khi thanh toán
    private IEnumerator ReloadUserData()
    {
        yield return new WaitForSeconds(2f); // chờ server cập nhật

        string userId = UserSession.currentUser.userId;
        string url = USER_API + userId;
        Debug.Log($"🔄 [StoreManager] Reloading user data from: {url}");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                Debug.Log("✅ [StoreManager] User data reloaded: " + json);

                try
                {
                    var newUser = JsonUtility.FromJson<UserProfile>(json);
                    if (newUser != null)
                    {
                        UserSession.currentUser = newUser;

                        // ✅ Cập nhật UI nếu có LoadUser trong scene
                        LoadUser uiLoader = FindObjectsByType<LoadUser>();
                        if (uiLoader != null)
                        {
                            uiLoader.RefreshUserUI();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("❌ Parse JSON user error: " + ex.Message);
                }
            }
            else
            {
                Debug.LogError($"❌ [StoreManager] Failed to reload user: {www.error}");
            }
        }
    }

    private T FindObjectsByType<T>()
    {
        throw new NotImplementedException();
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RegisterManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public TMP_InputField emailInput;

    [Header("Popup UI")]
    public CanvasGroup popupCanvasGroup;
    public TMP_Text popupMessageText;
    public Button popupOkButton;


    private string registerApiUrl = "https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/User/Register";
    private string emailVerifyApiUrl = "https://rapid-email-verifier.fly.dev/api/validate?email=";

    private Coroutine popupRoutine;

    private void Awake()
    {
        if (popupOkButton != null)
            popupOkButton.onClick.AddListener(HidePopupInstant);

        HidePopupInstant();
    }

    // 🟢 Khi nhấn nút Register
    public void OnRegisterButtonClick()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();
        string confirmPassword = confirmPasswordInput.text.Trim();
        string email = emailInput.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(email))
        {
            ShowPopup("Vui lòng nhập đầy đủ thông tin", new Color(1f, 0.6f, 0f));
            return;
        }

        if (password != confirmPassword)
        {
            ShowPopup("Mật khẩu xác nhận không trùng khớp", Color.red);
            return;
        }

        StartCoroutine(ValidateEmailBeforeRegister(username, password, email));
    }

    // 🔍 Kiểm tra email hợp lệ
    private IEnumerator ValidateEmailBeforeRegister(string username, string password, string email)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(emailVerifyApiUrl + UnityWebRequest.EscapeURL(email)))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                ShowPopup($"Lỗi khi kiểm tra email: {www.error}", Color.red);
                yield break;
            }

            EmailValidationResponse validation = JsonUtility.FromJson<EmailValidationResponse>(www.downloadHandler.text);
            if (validation == null || validation.validations == null)
            {
                ShowPopup("Không thể kiểm tra email", Color.red);
                yield break;
            }

            if (!validation.validations.syntax ||
                !validation.validations.domain_exists ||
                !validation.validations.mailbox_exists)
            {
                ShowPopup("Email không tồn tại hoặc không hợp lệ", Color.red);
                yield break;
            }

            // ✅ Email hợp lệ → đăng ký
            StartCoroutine(RegisterUser(username, password, email));
        }
    }

    // 📤 Gọi API đăng ký
    private IEnumerator RegisterUser(string username, string password, string email)
    {
        // ✅ Dùng class Serializable thay cho anonymous object
        var userData = new RegisterRequest
        {
            username = username,
            password = password,
            email = email
        };

        string jsonData = JsonUtility.ToJson(userData);
        Debug.Log("Sending JSON: " + jsonData);
        Debug.Log("To URL: " + registerApiUrl);

        using (UnityWebRequest www = new UnityWebRequest(registerApiUrl, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            Debug.Log($"responseCode: {www.responseCode}");
            Debug.Log(" downloadHandler.text: " + (www.downloadHandler?.text ?? "<null>"));
            Debug.Log(" error: " + (www.error ?? "<null>"));

            bool isError = www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError;

            if (!isError)
            {
                string responseText = www.downloadHandler?.text ?? "";
                Debug.Log("Register success response: " + responseText);
                try
                {
                    var response = JsonUtility.FromJson<ApiResponse>(responseText);
                    if (response != null && response.status == "success")
                        ShowPopup("Đăng ký thành công!", Color.green);
                    else
                        ShowPopup(response?.message ?? "Đăng ký thành công (no message)", Color.green);
                }
                catch
                {
                    ShowPopup("Đăng ký thành công!", Color.green);
                }
            }
            else
            {
                string serverText = www.downloadHandler?.text ?? "<no body>";
                Debug.LogError($" Register failed. code={www.responseCode}, error={www.error}, body={serverText}");

                try
                {
                    var srv = JsonUtility.FromJson<ApiResponse>(serverText);
                    if (srv != null && !string.IsNullOrEmpty(srv.message))
                        ShowPopup($"Lỗi đăng ký: {srv.message}", Color.red);
                    else
                        ShowPopup($"Lỗi đăng ký: HTTP {www.responseCode}", Color.red);
                }
                catch
                {
                    ShowPopup($"Lỗi đăng ký: HTTP {www.responseCode}", Color.red);
                }
            }
        }
    }

    // ================== POPUP UI ==================
    private void ShowPopup(string message, Color textColor, bool autoHide = true, float duration = 2.5f)
    {
        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        popupMessageText.text = message;
        popupMessageText.color = textColor;

        popupRoutine = StartCoroutine(FadePopup(autoHide, duration));
    }

    private IEnumerator FadePopup(bool autoHide, float duration)
    {
        yield return StartCoroutine(FadeCanvasGroup(popupCanvasGroup, 0, 1, 0.3f));
        popupCanvasGroup.interactable = true;
        popupCanvasGroup.blocksRaycasts = true;

        if (autoHide)
        {
            yield return new WaitForSeconds(duration);
            yield return StartCoroutine(FadeCanvasGroup(popupCanvasGroup, 1, 0, 0.3f));
        }
    }

    private void HidePopupInstant()
    {
        popupCanvasGroup.alpha = 0;
        popupCanvasGroup.interactable = false;
        popupCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float end, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, end, time / duration);
            yield return null;
        }
        group.alpha = end;

        if (end == 0)
        {
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    // ================== DATA MODELS ==================
    [System.Serializable]
    public class RegisterRequest
    {
        public string username;
        public string password;
        public string email;
    }

    [System.Serializable]
    public class EmailValidationResponse
    {
        public string email;
        public ValidationData validations;
        public int score;
        public string status;
    }

    [System.Serializable]
    public class ValidationData
    {
        public bool syntax;
        public bool domain_exists;
        public bool mx_records;
        public bool mailbox_exists;
        public bool is_disposable;
        public bool is_role_based;
    }

    [System.Serializable]
    public class ApiResponse
    {
        public string status;
        public string message;
    }
}

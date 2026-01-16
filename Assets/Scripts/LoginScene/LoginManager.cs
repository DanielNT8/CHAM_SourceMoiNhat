using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    private static string REMEMBER_ME_KEY = "RememberMe";

    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    [SerializeField] private Toggle rememberMeToggle;

    // --- 🟢 PHẦN MỚI: Thêm tham chiếu cho Popup ---
    [Header("Popup UI")]
    public CanvasGroup popupCanvasGroup;
    public TMP_Text popupMessageText;
    public Button popupOkButton;
    private Coroutine popupRoutine;
    // ------------------------------------------------

    private string apiBaseUrl = "https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/User/Login";
    private string hardcodeUser = "admin";
    private string hardcodePass = "123456";

    private class UserLoginCache { public bool rememberMe; public string username; public string password; }
    private readonly string _defaultUserName = JsonConvert.SerializeObject(new UserLoginCache() { rememberMe = false, username = string.Empty, password = string.Empty });

    // Class nhỏ để parse lỗi từ server trả về (cho đồng bộ với RegisterManager)
    private class ServerErrorResponse { public string status; public string message; }

    private void Awake()
    {
        // --- 🟢 PHẦN MỚI: Setup Popup ---
        if (popupOkButton != null)
            popupOkButton.onClick.AddListener(HidePopupInstant);
        HidePopupInstant();
        // ---------------------------------

        // Logic cũ: Xử lý Remember Me
        var rememberMeData = JsonConvert.DeserializeObject<UserLoginCache>(PlayerPrefs.GetString(REMEMBER_ME_KEY, _defaultUserName));
        if (rememberMeData.rememberMe)
        {
            rememberMeToggle.isOn = true;
            usernameInput.text = rememberMeData.username;
            passwordInput.text = rememberMeData.password;
        }
    }

    public void OnLoginButton()
    {
        string user = usernameInput.text.Trim();
        string pass = passwordInput.text.Trim();

        // --- 🟢 PHẦN MỚI: Validate Input trước khi gọi API ---
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            ShowPopup("Vui lòng nhập tên đăng nhập hoặc mật khẩu", new Color(1f, 0.6f, 0f)); // Màu cam
            return;
        }
        // ----------------------------------------------------

        if (string.IsNullOrEmpty(apiBaseUrl) || (user == hardcodeUser && pass == hardcodePass))
        {
            CheckLoginHardcode(user, pass);
        }
        else
        {
            StartCoroutine(CheckLoginAPI(user, pass));
        }
    }

    private void CheckLoginHardcode(string user, string pass)
    {
        if (user == hardcodeUser && pass == hardcodePass)
        {
            ShowPopup(" Login Hardcode Success!", Color.green);
            // Chờ xíu rồi load scene
            StartCoroutine(WaitAndLoadScene());
        }
        else
        {
            ShowPopup("❌ Invalid hardcode credentials", Color.red);
        }
    }

    private IEnumerator CheckLoginAPI(string user, string pass)
    {
        // ... (Phần tạo JSON và gửi Request giữ nguyên) ...
        var loginData = new { email = user, password = pass };
        string jsonData = JsonConvert.SerializeObject(loginData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(apiBaseUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = 10;

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text;
                Debug.Log($"✅ [Response] Success: {response}");

                // 🟢 1. Tạo biến cờ để kiểm tra trạng thái
                bool loginSuccess = false;

                try
                {
                    UserProfile userProfile = JsonConvert.DeserializeObject<UserProfile>(response);

                    if (userProfile != null && !string.IsNullOrEmpty(userProfile.userId))
                    {
                        ShowPopup("Đăng nhập thành công!", Color.green);

                        // Xử lý Remember Me (Giữ nguyên)
                        if (rememberMeToggle.isOn)
                        {
                            var cacheData = new UserLoginCache() { rememberMe = true, username = user, password = pass };
                            PlayerPrefs.SetString(REMEMBER_ME_KEY, JsonConvert.SerializeObject(cacheData));
                        }
                        else
                        {
                            PlayerPrefs.DeleteKey(REMEMBER_ME_KEY);
                        }
                        PlayerPrefs.Save();

                        UserSession.currentUser = userProfile;

                        // 🟢 2. Đánh dấu là thành công, NHƯNG CHƯA yield return ở đây
                        loginSuccess = true;
                    }
                    else
                    {
                        ShowPopup("⚠️ Đăng nhập thành công nhưng dữ liệu hồ sơ người dùng trống", Color.yellow);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ JSON Parse Error: {ex.Message}");
                    ShowPopup("Lỗi khi xử lý phản hồi từ máy chủ", Color.red);
                }

                // 🟢 3. Ra khỏi try/catch mới thực hiện lệnh chờ và chuyển cảnh
                if (loginSuccess)
                {
                    yield return new WaitForSeconds(1.0f); // ✅ Hết lỗi CS1626
                    SceneManager.LoadScene("HomeScene");
                }
            }
            else
            {
                // ... (Phần xử lý lỗi mạng giữ nguyên) ...
                string errorMessage = $"Đăng nhập thất bại ({www.responseCode})";
                if (www.downloadHandler != null)
                {
                    try
                    {
                        var errorObj = JsonConvert.DeserializeObject<ServerErrorResponse>(www.downloadHandler.text);
                        if (errorObj != null && !string.IsNullOrEmpty(errorObj.message)) errorMessage = errorObj.message;
                    }
                    catch { }
                }
                ShowPopup($"❌ {errorMessage}", Color.red);
            }
        }
    }

    private IEnumerator WaitAndLoadScene()
    {
        yield return new WaitForSeconds(1.0f);
        SceneManager.LoadScene("HomeScene");
    }

    // ==========================================================
    // 🟢 COPY TỪ REGISTER MANAGER: CÁC HÀM XỬ LÝ POPUP
    // ==========================================================
    private void ShowPopup(string message, Color textColor, bool autoHide = true, float duration = 2.5f)
    {
        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        if (popupMessageText != null)
        {
            popupMessageText.text = message;
            popupMessageText.color = textColor;
        }

        popupRoutine = StartCoroutine(FadePopup(autoHide, duration));
    }

    private IEnumerator FadePopup(bool autoHide, float duration)
    {
        yield return StartCoroutine(FadeCanvasGroup(popupCanvasGroup, 0, 1, 0.3f));

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.interactable = true;
            popupCanvasGroup.blocksRaycasts = true;
        }

        if (autoHide)
        {
            yield return new WaitForSeconds(duration);
            yield return StartCoroutine(FadeCanvasGroup(popupCanvasGroup, 1, 0, 0.3f));
        }
    }

    private void HidePopupInstant()
    {
        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0;
            popupCanvasGroup.interactable = false;
            popupCanvasGroup.blocksRaycasts = false;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float end, float duration)
    {
        if (group == null) yield break;

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
}
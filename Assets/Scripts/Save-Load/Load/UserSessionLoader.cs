using UnityEngine;

public class UserSessionLoader : MonoBehaviour
{
    private void Awake()
    {
        LoadUserSession();
    }

    private void LoadUserSession()
    {
        // Nếu đã có user session (ví dụ đăng nhập online) thì không cần load lại
        if (UserSession.currentUser != null && !string.IsNullOrEmpty(UserSession.currentUser.userId))
        {
            Debug.Log("✅ UserSession đã tồn tại, bỏ qua việc load lại.");
            return;
        }

        Debug.LogWarning("⚠ Không có user session được lưu, cần đăng nhập online.");
    }
}

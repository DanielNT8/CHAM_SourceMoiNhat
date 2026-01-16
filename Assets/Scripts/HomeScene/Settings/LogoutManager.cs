using UnityEngine;

public class LogoutManager : MonoBehaviour
{
    // Kéo hàm này vào sự kiện OnClick của nút Logout
    public void OnLogoutAndExit()
    {
        // 1. (Tùy chọn) Xóa thông tin đăng nhập đã lưu để lần sau mở lại phải nhập lại
        // Nếu bạn muốn giữ "Remember Me" thì xóa dòng PlayerPrefs bên dưới đi
        PlayerPrefs.DeleteKey("RememberMe");
        PlayerPrefs.Save();

        Debug.Log("Đang tắt ứng dụng...");

        // 2. Code tắt ứng dụng
#if UNITY_EDITOR
        // Nếu đang chạy thử trong Unity Editor thì dừng chế độ Play
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Nếu đang chạy trên điện thoại hoặc PC thật thì tắt ứng dụng
        Application.Quit();
#endif
    }
}
using UnityEngine;

public class UserSession : MonoBehaviour
{
    public static UserProfile currentUser;

    private void Awake()
    {
        // ✅ Giữ object này khi chuyển scene
        DontDestroyOnLoad(this.gameObject);

        // Nếu chưa có instance user, gán luôn instance này
        if (FindObjectsByType<UserSession>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject); // tránh trùng
        }
    }
}

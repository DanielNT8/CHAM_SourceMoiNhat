using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [Header("Chọn nhạc cho màn này")]
    public AudioClip backgroundMusic; // Kéo file nhạc vào đây

    private void Start()
    {
        // Khi màn chơi bắt đầu, gọi AudioManager phát bài này
        if (AudioManager.Instance != null && backgroundMusic != null)
        {
            AudioManager.Instance.PlayMusic(backgroundMusic);
        }
    }
}
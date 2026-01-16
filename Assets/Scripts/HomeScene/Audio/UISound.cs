using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Bắt buộc có để bắt sự kiện Click

public class UISound : MonoBehaviour, IPointerClickHandler
{
    [Header("Tùy chọn (Để trống sẽ dùng tiếng Click mặc định)")]
    public AudioClip customSound;

    // Hàm này tự động chạy khi người dùng Click vào object chứa script này
    public void OnPointerClick(PointerEventData eventData)
    {
        // Kiểm tra nút có bị tắt (interactable = false) không, nếu tắt thì không kêu
        Button btn = GetComponent<Button>();
        if (btn != null && !btn.interactable) return;

        // Nếu có âm thanh riêng thì phát, không thì phát tiếng chung
        if (customSound != null)
        {
            AudioManager.Instance.PlaySFX(customSound);
        }
        else
        {
            AudioManager.Instance.PlayClickSound(); // Gọi hàm phát tiếng click chung
        }
    }
}
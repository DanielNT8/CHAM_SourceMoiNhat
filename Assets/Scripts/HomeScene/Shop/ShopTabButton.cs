using UnityEngine;
using UnityEngine.UI;

public class ShopTabButton : MonoBehaviour
{
    public ShopManager shopManager;                // Tham chiếu tới ShopManager
    public ShopManager.ShopTab tab;                // Tab nào sẽ mở khi click nút này


    [Header("Tab Background Settings")]
    public Image tabBackground;          // Ảnh nền phía sau icon
    public Sprite activeSprite;          // Sprite khi tab được chọn (nâu)
    public Sprite inactiveSprite;        // Sprite khi tab không được chọn (viền trắng)
    void Start()
    {
        // Bắt sự kiện click của nút
        GetComponent<Button>().onClick.AddListener(() =>
        {
            shopManager.OpenTab(tab);
            shopManager.UpdateTabVisual(this);
        });
    }

    // Hàm được gọi từ ShopManager để đổi sprite
    public void SetActive(bool isActive)
    {
        if (tabBackground != null)
        {
            tabBackground.sprite = isActive ? activeSprite : inactiveSprite;
        }
    }
}

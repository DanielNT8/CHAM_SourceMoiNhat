using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [System.Serializable]
    public class ShopItem
    {
        public GameObject itemPrefab;
        public int price;
    }


    public enum ShopTab
    {
        Seeds,
        Fence,
        Chair,
        Grass,
        Flower,
        Wood,
        House
    }

    [Header("Shop Settings")]
    public List<ShopItem> seedItems;
    public List<ShopItem> FrenceItems;
    public List<ShopItem> ChairItems;
    public List<ShopItem> GrassItems;
    public List<ShopItem> FlowerItems;
    public List<ShopItem> WoodItems;
    public List<ShopItem> HouseItems;

    public GameObject shopSlotPrefab;
    public Transform contentParent;

    [Header("References")]
    public BagController bagController; // Gán trong Scene
    public TMP_Text UserCoins; // Gán trong Scene

    private List<GameObject> currentSlots = new List<GameObject>();
    private ShopTab currentTab = ShopTab.Seeds;

    [Header("Tab Buttons")]
    public List<ShopTabButton> tabButtons;

    // Popup
    private GameObject popupInstance;
    private TMP_Text popupMessage;
    private Button popupYesButton;
    private Button popupNoButton;

    void Start()
    {
        LoadPopupPrefab();
        OpenTab(ShopTab.Seeds);
    }

    private void LoadPopupPrefab()
    {
        if (popupInstance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("UI/ConfirmPopup");
            if (prefab != null)
            {
                // ✅ Tìm Canvas riêng dành cho popup
                Canvas popupCanvas = GameObject.Find("CanvasPopup")?.GetComponent<Canvas>();
                if (popupCanvas == null)
                {
                    Debug.LogError(" Không tìm thấy Canvas_Popup trong Scene! Hãy tạo một Canvas riêng cho popup.");
                    return;
                }

                popupInstance = Instantiate(prefab, popupCanvas.transform);
                popupInstance.SetActive(false);

                popupMessage = popupInstance.transform.Find("Text")?.GetComponent<TMP_Text>();
                popupYesButton = popupInstance.transform.Find("YesButton")?.GetComponent<Button>();
                popupNoButton = popupInstance.transform.Find("NoButton")?.GetComponent<Button>();
            }
            else
            {
                Debug.LogError(" Không tìm thấy prefab popup tại Resources/UI/ConfirmPopup");
            }
        }
    }

    public void OpenTab(ShopTab tab)
    {
        currentTab = tab;

        // Xóa slot cũ
        foreach (var slot in currentSlots)
        {
            Destroy(slot);
        }
        currentSlots.Clear();

        // Lấy danh sách item theo tab
        List<ShopItem> items = GetItemsByTab(tab);

        // Tạo slot mới
        foreach (var item in items)
        {
            GameObject slot = Instantiate(shopSlotPrefab, contentParent);
            currentSlots.Add(slot);

            Image iconImg = slot.transform.Find("IconItem")?.GetComponent<Image>();
            TMP_Text priceTxt = slot.transform.Find("Price")?.GetComponent<TMP_Text>();

            // Lấy sprite từ prefab item
            Sprite itemSprite = null;
            if (item.itemPrefab != null)
            {
                SpriteRenderer sr = item.itemPrefab.GetComponent<SpriteRenderer>();
                if (sr != null) itemSprite = sr.sprite;

                Image img = item.itemPrefab.GetComponent<Image>();
                if (img != null) itemSprite = img.sprite;
            }

            if (iconImg != null && itemSprite != null)
                iconImg.sprite = itemSprite;

            if (priceTxt != null)
                priceTxt.text = item.price.ToString();

            // Gắn sự kiện click
            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                int price = item.price;
                GameObject prefab = item.itemPrefab;
                btn.onClick.AddListener(() => ShowBuyConfirm(prefab, price));
            }
        }
    }

    private List<ShopItem> GetItemsByTab(ShopTab tab)
    {
        switch (tab)
        {
            case ShopTab.Seeds: return seedItems;
            case ShopTab.Fence: return FrenceItems;
            case ShopTab.Chair: return ChairItems;
            case ShopTab.Grass: return GrassItems;
            case ShopTab.Flower: return FlowerItems;
            case ShopTab.Wood: return WoodItems;
            case ShopTab.House: return HouseItems;
            default: return new List<ShopItem>();
        }
    }

    // 🔹 Hiển thị popup xác nhận mua
    private void ShowBuyConfirm(GameObject itemPrefab, int price)
    {
        if (!int.TryParse(UserCoins?.text, out int userCoins))
        {
            Debug.LogWarning(" Không thể đọc số coins từ UserCoins!");
            return;
        }

        if (popupInstance == null)
        {
            Debug.LogError(" Popup chưa được load!");
            return;
        }

        popupInstance.SetActive(true);
        string itemName = itemPrefab != null ? itemPrefab.name : "vật phẩm";

        if (userCoins < price)
        {
            if (popupMessage != null)
                popupMessage.text = $"Bạn không đủ xu để mua {itemName}!";
            popupYesButton.gameObject.SetActive(false);
            popupNoButton.GetComponentInChildren<TMP_Text>().text = "Close";

            popupNoButton.onClick.RemoveAllListeners();
            popupNoButton.onClick.AddListener(() => popupInstance.SetActive(false));
            return;
        }

        // Đủ tiền → hiển thị xác nhận
        if (popupMessage != null)
            popupMessage.text = $"Bạn có muốn mua {itemName} với giá {price} xu không?";

        popupYesButton.gameObject.SetActive(true);
        popupNoButton.GetComponentInChildren<TMP_Text>().text = "No";

        popupYesButton.onClick.RemoveAllListeners();
        popupNoButton.onClick.RemoveAllListeners();

        popupYesButton.onClick.AddListener(() =>
        {
            popupInstance.SetActive(false);
            BuyConfirmed(itemPrefab, price);
        });

        popupNoButton.onClick.AddListener(() =>
        {
            popupInstance.SetActive(false);
        });
    }

    private void BuyConfirmed(GameObject itemPrefab, int price)
    {
        if (itemPrefab == null) return;

        ItemClass itemInfo = itemPrefab.GetComponent<ItemClass>();
        if (itemInfo == null)
        {
            Debug.LogError("❌ Prefab không có ItemClass!");
            return;
        }

        int coinsAfterBuy = int.Parse(UserCoins.text) - price;
        UserCoins.text = coinsAfterBuy.ToString();
        if (itemInfo.itemId.StartsWith("S_"))
        {
            itemInfo.itemId = itemInfo.itemId.Substring(2);
        }
        bagController.AddItemToBag(itemInfo.itemId, 1);
        Debug.Log($"✅ Đã mua {itemInfo.itemId} với giá {price}");
    }

#if UNITY_EDITOR
    [Sirenix.OdinInspector.Button]
    private void HackGold(int newValue)
    {
        UserCoins.text = newValue.ToString(); 
    }

    [Sirenix.OdinInspector.Button]
    private void HackSeed()
    {
        bagController.AddItemToBag("TB0", 99);
    }    
#endif

    public void UpdateTabVisual(ShopTabButton activeButton)
    {
        foreach (var btn in tabButtons)
        {
            bool isActive = btn == activeButton;
            btn.SetActive(isActive);
        }
    }
}

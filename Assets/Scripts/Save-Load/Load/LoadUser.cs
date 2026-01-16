using TMPro;
using UnityEngine;

public class LoadUser : MonoBehaviour
{
    private UserProfile user;
    public TMP_Text userLevel;
    public TMP_Text userCoin;
    public TMP_Text userXP;
    public TMP_Text level;
    public TMP_Text UserName;
    [SerializeField] SlicedFilledImage _expProgress;

    private RectTransform parentXpBar;

    private void Start()
    {
        RefreshUserUI();
    }
    
    public void RefreshUserUI()
    {
        user = UserSession.currentUser;
        if (user == null)
        {
            return;
        }

        userCoin.text = user.coin.ToString();
        userXP.text = $"{user.currentExp}/{user.expPerLevel}";
        level.text = user.level.ToString();
        UserName.text = user.userName.ToString();
        _expProgress.fillAmount = (float)user.currentExp / user.expPerLevel;
    }
}

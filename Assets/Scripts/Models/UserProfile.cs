public class UserProfile
{
    public string userId;
    public string userName;
    public int level;
    public int currentExp;
    public int expPerLevel = 100;
    public int coin;
    public string memberTypeId;

    public static UserProfile instance;
}

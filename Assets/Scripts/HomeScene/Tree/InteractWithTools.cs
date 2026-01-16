using UnityEngine;

public class InteractWithTools : MonoBehaviour
{
    [Header("Setup Tree State Level")]
    public GameObject SeedPrefab;
    public GameObject TreeLv1;
    public GameObject TreeLv2;
    public GameObject TreeLv3;

    [Header("XP / Level")]
    public int level = 0;             // 0 = Seed
    public int currentXP = 0;
    public int xpToLevel = 100;

}

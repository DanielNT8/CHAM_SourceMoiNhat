using UnityEngine;

public class SaveItemData : MonoBehaviour
{
    [Header("Base Info")]
    public string itemId;
    public string itemName;
    public int level = 1;

    [Header("Runtime Data")]
    [SerializeField] private int expPerLevel;
    [SerializeField] private float positionX;
    [SerializeField] private float positionY;

    private void Awake()
    {
        // Khi spawn ra thì tự cập nhật vị trí ban đầu
        UpdatePosition(transform.position);
    }

    private void Update()
    {
        // Nếu item có thể di chuyển, update vị trí liên tục
        UpdatePosition(transform.position);
    }

    // Hàm cập nhật exp + level
    public void SetExp(int exp, int level)
    {
        this.expPerLevel = exp;
        this.level = level;
    }

    // Hàm cập nhật vị trí
    public void UpdatePosition(Vector2 pos)
    {
        this.positionX = pos.x;
        this.positionY = pos.y;
    }

    // Lấy lại vị trí dưới dạng Vector2
    public Vector2 GetPosition()
    {
        return new Vector2(positionX, positionY);
    }

    // Debug dễ nhìn
    public override string ToString()
    {
        return $"Item: {itemId} - {name}, Level: {level}, Exp: {expPerLevel}, Pos: ({positionX}, {positionY})";
    }
}

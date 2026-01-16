using UnityEngine;

public static class TilemapValidator
{
    public static bool IsValidDropPosition(Vector3 worldPosition, string itemType)
    {
        // Không ép kiểu cứng, dùng Collider2D cho an toàn
        Collider2D hit = Physics2D.OverlapPoint(worldPosition);
        if (hit == null) return false;

        // Kiểm tra tag của collider
        switch (itemType)
        {
            case "Seed":
                return hit.CompareTag("Soil");

            case "Decor":
                return hit.CompareTag("Background");

            default:
                return false;
        }
    }

}

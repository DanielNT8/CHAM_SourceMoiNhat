using UnityEngine;
using UnityEngine.UI;

public class HoleRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    [Header("Tâm của lỗ thủng (Thường là con đom đóm)")]
    public RectTransform holeCenter;

    [Header("Bán kính cho phép click (Tính bằng Pixel)")]
    public float clickableRadius = 150f;

    // Hàm này của Unity: 
    // Trả về TRUE -> Chặn click lại tại màn đen.
    // Trả về FALSE -> Cho click xuyên qua màn đen xuống Game.
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (holeCenter == null || !holeCenter.gameObject.activeSelf)
            return true; // Nếu không có lỗ, chặn toàn bộ click

        // Chuyển tọa độ tâm lỗ ra tọa độ màn hình
        Vector2 centerScreenPos = RectTransformUtility.WorldToScreenPoint(eventCamera, holeCenter.position);

        // Tính khoảng cách từ chuột đến tâm lỗ
        float distance = Vector2.Distance(screenPoint, centerScreenPos);

        // Nếu chuột nằm NGOÀI lỗ -> TRUE (Chặn lại)
        // Nếu chuột nằm TRONG lỗ -> FALSE (Cho click xuyên qua)
        return distance > clickableRadius;
    }
}
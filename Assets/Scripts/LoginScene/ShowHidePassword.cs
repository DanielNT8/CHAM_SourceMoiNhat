using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowHidePassword : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField passwordInputField;
    public Image toggleIcon;
    public Sprite showPasswordSprite;
    public Sprite hidePasswordSprite;

    private bool isPasswordVisible = false;

    public void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;

        // Đổi kiểu hiển thị
        passwordInputField.contentType = isPasswordVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        // Đặt lại text để cập nhật kiểu hiển thị
        string currentText = passwordInputField.text;
        passwordInputField.text = "";
        passwordInputField.text = currentText;

        // Đổi icon
        if (toggleIcon != null)
        {
            toggleIcon.sprite = isPasswordVisible ? hidePasswordSprite : showPasswordSprite;
        }

        // Giữ focus và caret
        passwordInputField.ActivateInputField();
    }
}

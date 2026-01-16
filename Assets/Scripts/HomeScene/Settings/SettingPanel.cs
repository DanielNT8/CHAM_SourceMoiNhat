using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Start()
    {
        // Lấy giá trị đã lưu để hiển thị lên Slider khi mở game
        masterSlider.value = PlayerPrefs.GetFloat("Vol_Master", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("Vol_Music", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("Vol_SFX", 1f);

        // Đăng ký sự kiện: Khi kéo slider thì gọi hàm bên AudioManager
        masterSlider.onValueChanged.AddListener(OnMasterChange);
        musicSlider.onValueChanged.AddListener(OnMusicChange);
        sfxSlider.onValueChanged.AddListener(OnSFXChange);
    }

    private void OnMasterChange(float val)
    {
        AudioManager.Instance.SetMasterVolume(val);
    }

    private void OnMusicChange(float val)
    {
        AudioManager.Instance.SetMusicVolume(val);
    }

    private void OnSFXChange(float val)
    {
        AudioManager.Instance.SetSFXVolume(val);
    }
}
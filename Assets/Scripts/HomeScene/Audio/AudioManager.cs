using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("1. References (Kéo Mixer và Group vào)")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private AudioMixerGroup musicGroup; // Nhóm Music trong Mixer
    [SerializeField] private AudioMixerGroup sfxGroup;   // Nhóm SFX trong Mixer

    [Header("2. Audio Sources (Tự tạo hoặc Code tự tìm)")]
    [SerializeField] private AudioSource musicSource; // Loa phát nhạc nền
    [SerializeField] private AudioSource sfxSource;   // Loa phát hiệu ứng (Click, Thu hoạch...)

    [Header("3. Common Clips (Thư viện âm thanh)")]
    public AudioClip uiClickClip; // Tiếng click mặc định cho toàn game
    public AudioClip defaultTreeTouchClip;

    [Header("Settings Keys (Tên biến đã Expose trong Mixer)")]
    private const string MIXER_MASTER = "MasterVol";
    private const string MIXER_MUSIC = "MusicVol";
    private const string MIXER_SFX = "SFXVol";

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Tự động tạo AudioSource nếu quên tạo trong Inspector
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // --- CẤU HÌNH OUTPUT CHO LOA ---
        // Đảm bảo nhạc đi vào kênh Music, SFX đi vào kênh SFX
        musicSource.outputAudioMixerGroup = musicGroup;
        sfxSource.outputAudioMixerGroup = sfxGroup;

        // --- LOAD SETTING ÂM LƯỢNG ĐÃ LƯU ---
        SetMasterVolume(PlayerPrefs.GetFloat("Vol_Master", 1f));
        SetMusicVolume(PlayerPrefs.GetFloat("Vol_Music", 1f));
        SetSFXVolume(PlayerPrefs.GetFloat("Vol_SFX", 1f));
    }

    // ========================================================================
    // PHẦN A: XỬ LÝ SETTING VOLUME (Dùng cho Setting Panel)
    // ========================================================================

    public void SetMasterVolume(float value)
    {
        float volume = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        mainMixer.SetFloat(MIXER_MASTER, volume);
        PlayerPrefs.SetFloat("Vol_Master", value);
    }

    public void SetMusicVolume(float value)
    {
        float volume = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        mainMixer.SetFloat(MIXER_MUSIC, volume);
        PlayerPrefs.SetFloat("Vol_Music", value);
    }

    public void SetSFXVolume(float value)
    {
        float volume = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        mainMixer.SetFloat(MIXER_SFX, volume);
        PlayerPrefs.SetFloat("Vol_SFX", value);
    }

    // ========================================================================
    // PHẦN B: XỬ LÝ PHÁT ÂM THANH (Dùng cho Gameplay & UI)
    // ========================================================================

    /// <summary>
    /// Phát nhạc nền (Lặp lại)
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource.clip == clip) return; // Nếu đang phát bài này rồi thì thôi

        musicSource.clip = clip;
        musicSource.loop = true; // Nhạc nền phải lặp
        musicSource.Play();
    }

    /// <summary>
    /// Phát SFX bất kỳ (Thu hoạch cây, Mở rương...)
    /// Có thể phát chồng nhiều tiếng cùng lúc (OneShot)
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            // PlayOneShot là chìa khóa để xử lý nhiều SFX cùng lúc
            sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Phát tiếng Click UI mặc định (Dùng cho script UISound)
    /// </summary>
    public void PlayClickSound()
    {
        PlaySFX(uiClickClip);
    }
}
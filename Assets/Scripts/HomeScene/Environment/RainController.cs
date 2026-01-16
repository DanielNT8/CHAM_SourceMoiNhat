using UnityEngine;

public class RainController : MonoBehaviour
{
    [Header("🌧️ Gán Particle System mưa vào đây")]
    public ParticleSystem rainEffect;

    private bool isRaining = true;

    void Start()
    {
        ManagerInGame_Weather.Instance.OnWeatherChanged += OnWeatherChanged;
    }

    protected void OnWeatherChanged(AbstractWeather newWeather)
    {
        if (newWeather is Weather_Rainy)
        {
            rainEffect.Play();
        }
        else
        {
            rainEffect.Stop();
        }
    }

    public void ToggleRain()
    {
        if (rainEffect == null) return;

        isRaining = !isRaining;

        if (isRaining)
        {
            rainEffect.Play();
            ManagerInGame_Weather.Instance.ForceChangeToRainWeather();
        }
        else
        {
            rainEffect.Stop();
            ManagerInGame_Weather.Instance.ForceChangeToSunnyWeather();
        }
    }
}

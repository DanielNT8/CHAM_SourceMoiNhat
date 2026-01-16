using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;

[ExecuteAlways]
public class DayNightGlobalLight : MonoBehaviour
{
    [SerializeField] private Light2D globalLight;

    [Header("Settings")]
    [Range(0, 24)] public float timeOfDay = 8f;
    public float cycleDuration = 180f;
    public bool useRealTime = false;

    private Gradient colorGradient;
    private AnimationCurve intensityCurve;

    private void OnEnable()
    {
        InitializeData();
        ValidateGlobalLight();
    }

    private void Update()
    {
        if (globalLight == null) ValidateGlobalLight();

        CalculateTime();
        UpdateLighting();
    }

    private void ValidateGlobalLight()
    {
        if (globalLight != null && globalLight.lightType == Light2D.LightType.Global) return;

        var lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.lightType == Light2D.LightType.Global)
            {
                globalLight = light;
                return;
            }
        }

        GameObject go = new GameObject("Global Light 2D");
        globalLight = go.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
    }

    private void CalculateTime()
    {
        if (useRealTime)
        {
            timeOfDay = (float)DateTime.Now.TimeOfDay.TotalHours;
        }
        else
        {
            timeOfDay += Time.deltaTime * (24f / cycleDuration);
            if (timeOfDay >= 24f) timeOfDay %= 24f;
        }
    }

    private void UpdateLighting()
    {
        if (globalLight == null) return;

        float t = timeOfDay / 24f;
        globalLight.color = colorGradient.Evaluate(t);
        globalLight.intensity = intensityCurve.Evaluate(t);
    }

    private void InitializeData()
    {
        colorGradient = new Gradient();
        colorGradient.colorKeys = new GradientColorKey[]
        {
        // 🟢 Sửa màu đêm (0h, 0.2f, 0.8f, 1f) sáng hơn chút (ví dụ: 80, 80, 120)
        new GradientColorKey(new Color32(80, 80, 130, 255), 0f),
        new GradientColorKey(new Color32(80, 80, 130, 255), 0.2f),

        new GradientColorKey(new Color32(255, 200, 150, 255), 0.25f), // Bình minh
        new GradientColorKey(new Color32(255, 244, 214, 255), 0.5f),  // Trưa
        new GradientColorKey(new Color32(255, 180, 120, 255), 0.75f), // Hoàng hôn

        new GradientColorKey(new Color32(80, 80, 130, 255), 0.8f),
        new GradientColorKey(new Color32(80, 80, 130, 255), 1f)
        };

        colorGradient.alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };

        intensityCurve = new AnimationCurve(
            new Keyframe(0f, 0.1f),
            new Keyframe(0.2f, 0.1f),
            new Keyframe(0.25f, 0.8f),
            new Keyframe(0.5f, 1.2f),
            new Keyframe(0.75f, 0.8f),
            new Keyframe(0.8f, 0.1f),
            new Keyframe(1f, 0.1f)
        );
    }
}
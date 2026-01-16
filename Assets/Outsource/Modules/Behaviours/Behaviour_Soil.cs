using Sirenix.OdinInspector;
using UnityEngine;

public class Behaviour_Soil : MonoBehaviour
{
    [field: SerializeField, ReadOnly] public float CurrentMoisture { get; private set; }

    [ShowInInspector, ReadOnly, PropertyOrder(1)]
    public bool IsPestInfested => _isPestInfested;

    private const float MOISTURE_DECREASE_RATE = 1f;
    private const float MOISTURE_INCREASING_BY_WATERING = 50f;

    private float _moistureMultiplierByWeather = -1f;
    [SerializeField, HideInInspector] private bool _isPestInfested = false;

    [Title("Pest Settings")]
    [BoxGroup("Pest Config")]
    [SerializeField]
    private float _pestCheckInterval = 2f;

    [BoxGroup("Pest Config")]
    [SerializeField, Range(0, 100)]
    private float _pestBaseProbability = 5f;

    [BoxGroup("Pest Config")]
    [SerializeField, Range(0, 100)]
    private float _pestMaxProbability = 80f;

    [BoxGroup("Pest Config")]
    [SerializeField, Min(1.0f)]
    private float _pestGrowthRate = 1.1f;

    private float _timeSpentOverMoistureLimit = 0f;
    private float _checkTimer = 0f;

    private void Awake()
    {
        CurrentMoisture = 100f;
    }

    private void OnEnable()
    {
        if (ManagerInGame_Weather.Instance != null)
            ManagerInGame_Weather.Instance.OnWeatherChanged += OnWeatherChanged;
    }

    private void OnDisable()
    {
        if (ManagerInGame_Weather.Instance != null)
            ManagerInGame_Weather.Instance.OnWeatherChanged -= OnWeatherChanged;
    }

    private void Update()
    {
        DecreaseMoistureOverTime();
        HandlePestInfestationLogic();
    }

    private void OnWeatherChanged(AbstractWeather newWeather)
    {
        switch (newWeather)
        {
            case Weather_Rainy _:
                _moistureMultiplierByWeather = 2f;
                break;
            case Weather_Sunny _:
                _moistureMultiplierByWeather = -2f;
                break;
            default:
                _moistureMultiplierByWeather = -1f;
                break;
        }
    }

    private void DecreaseMoistureOverTime()
    {
        if (CurrentMoisture > 0)
        {
            CurrentMoisture += MOISTURE_DECREASE_RATE * _moistureMultiplierByWeather * Time.deltaTime;
            CurrentMoisture = Mathf.Clamp(CurrentMoisture, 0, 110);
        }
    }

    private void HandlePestInfestationLogic()
    {
        if (_isPestInfested) return;

        if (CurrentMoisture > 100f)
        {
            _timeSpentOverMoistureLimit += Time.deltaTime;
            _checkTimer += Time.deltaTime;

            if (_checkTimer >= _pestCheckInterval)
            {
                CheckForInfestation();
                _checkTimer = 0f;
            }
        }
        else
        {
            _timeSpentOverMoistureLimit = 0f;
            _checkTimer = 0f;
        }
    }

    private void CheckForInfestation()
    {
        float steps = _timeSpentOverMoistureLimit / _pestCheckInterval;
        float currentChance = _pestBaseProbability * Mathf.Pow(_pestGrowthRate, steps);

        currentChance = Mathf.Min(currentChance, _pestMaxProbability);

        if (Random.Range(0f, 100f) < currentChance)
        {
            SetInfested(true);
        }
    }

    private void SetInfested(bool isInfested)
    {
        _isPestInfested = isInfested;
    }

    public void CurePest()
    {
        SetInfested(false);
        _timeSpentOverMoistureLimit = 0f;
    }

    public void WaterSoil()
    {
        CurrentMoisture += MOISTURE_INCREASING_BY_WATERING;
    }

    public void InitializeRequirements(TreeGrowth treeGrowth)
    {
        treeGrowth.LevelUpRequirements += MoistureRequirementMet;
        treeGrowth.LevelUpRequirements += PestInfestedRequirementMet;
    }

    protected bool MoistureRequirementMet(TreeGrowth treeGrowth)
    {
        bool output = CurrentMoisture >= 80;

        if (!output)
            treeGrowth.ShowInfoUI($"Vui lòng tưới thêm nước, cây chưa đủ độ ẩm. Hiện tại: {(int)CurrentMoisture}. Yêu cầu: 80");

        return output;
    }

    protected bool PestInfestedRequirementMet(TreeGrowth treeGrowth)
    {
        bool output = !_isPestInfested;

        if (!output)
            treeGrowth.ShowInfoUI("Vui lòng xử lý sâu bệnh trước khi cây có thể phát triển. Có thể dùng thuốc trừ sâu");

        return output;
    }
}
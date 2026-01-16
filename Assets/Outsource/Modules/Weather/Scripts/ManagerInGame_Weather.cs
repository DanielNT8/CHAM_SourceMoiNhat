using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using Sirenix.OdinInspector;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class ManagerInGame_Weather : MonoBehaviour
{
    public static ManagerInGame_Weather Instance;

    [SerializeField] private string openWeatherMapApiKey;
    [SerializeField] private GameObject manualInputPanel;
    [SerializeField] private GameObject confirmSavePanel;
    [SerializeField] private TMP_Dropdown dropdownCountry;
    [SerializeField] private TMP_Dropdown dropdownCity;
    [SerializeField] private TextMeshProUGUI weatherText;
    [SerializeField] private Button ButtonAIOption;

    public AbstractWeather CurrentRealWorldWeather { get; private set; }
    public Action<AbstractWeather> OnWeatherChanged;
    public Action OnOpenAiOption;

    private const string PLAYER_CITY_KEY = "PlayerCity_New";
    private const string IP_API_URL = "http://ip-api.com/json/";
    private const string WEATHER_API_URL = "https://api.openweathermap.org/data/2.5/weather";
    private const string REST_COUNTRIES_URL = "https://restcountries.com/v3.1/all?fields=name,cca2";
    private const string GEODB_CITIES_URL = "http://geodb-free-service.wirefreethought.com/v1/geo/cities";

    private List<RestCountryItem> _cachedCountries = new List<RestCountryItem>();
    private string _cityName = string.Empty;
    private string _pendingCityName = string.Empty;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        this.RegisterEventButton();
        SetWeather(new Weather_Sunny());

        if (PlayerPrefs.HasKey(PLAYER_CITY_KEY))
        {
            _cityName = PlayerPrefs.GetString(PLAYER_CITY_KEY);
            string url = $"{WEATHER_API_URL}?q={_cityName}&appid={openWeatherMapApiKey}&units=metric";
            StartCoroutine(CallWeatherAPI(url));
        }
        else
        {
            StartCoroutine(GetLocationByIP());
        }
    }

    private IEnumerator GetLocationByIP()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(IP_API_URL))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                EnableManualInput();
            }
            else
            {
                IPGeoData data = JsonUtility.FromJson<IPGeoData>(webRequest.downloadHandler.text);
                _cityName = data.city;
                StartCoroutine(GetWeatherFromOpenWeather(data.lat, data.lon));
            }
        }
    }

    public void EnableManualInput()
    {
        StartCoroutine(FetchCountries());
    }

    private IEnumerator FetchCountries()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(REST_COUNTRIES_URL))
        {
            GameManager.Instance.ShowLoading("Đang tải dữ liệu quốc gia...");
            yield return request.SendWebRequest();
            GameManager.Instance.HideLoading();

            if (request.result == UnityWebRequest.Result.Success)
            {
                manualInputPanel.SetActive(true);

                string jsonRaw = request.downloadHandler.text;
                string jsonFixed = "{\"items\":" + jsonRaw + "}";

                RestCountryWrapper wrapper = JsonUtility.FromJson<RestCountryWrapper>(jsonFixed);
                _cachedCountries = wrapper.items;
                _cachedCountries.Sort((a, b) => string.Compare(a.name.common, b.name.common));

                dropdownCountry.ClearOptions();
                List<string> countryNames = new List<string>();

                foreach (var item in _cachedCountries)
                {
                    countryNames.Add(item.name.common);
                }

                dropdownCountry.AddOptions(countryNames);

                dropdownCountry.onValueChanged.RemoveAllListeners();
                dropdownCountry.onValueChanged.AddListener(delegate {
                    string selectedCountryName = dropdownCountry.options[dropdownCountry.value].text;
                    string countryCode = GetCountryCodeByName(selectedCountryName);
                    StartCoroutine(FetchCities(countryCode));
                });

                if (_cachedCountries.Count > 0)
                {
                    StartCoroutine(FetchCities(_cachedCountries[0].cca2));
                }
            }
        }
    }

    private string GetCountryCodeByName(string name)
    {
        foreach (var item in _cachedCountries)
        {
            if (item.name.common == name) return item.cca2;
        }
        return "VN";
    }

    private IEnumerator FetchCities(string countryCode)
    {
        string url = $"{GEODB_CITIES_URL}?countryIds={countryCode}&minPopulation=100000&limit=10&sort=-population";
        dropdownCity.ClearOptions();

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                GeoDBCityResponse response = JsonUtility.FromJson<GeoDBCityResponse>(request.downloadHandler.text);
                List<string> cityNames = new List<string>();

                foreach (var item in response.data)
                {
                    cityNames.Add(item.city);
                }

                dropdownCity.AddOptions(cityNames);
            }
        }
    }

    public void OnManualConfirm()
    {
        _pendingCityName = dropdownCity.options[dropdownCity.value].text;

        if (manualInputPanel != null) manualInputPanel.SetActive(false);
        if (confirmSavePanel != null) confirmSavePanel.SetActive(true);
    }

    public void ConfirmSaveCity()
    {
        _cityName = _pendingCityName;
        PlayerPrefs.SetString(PLAYER_CITY_KEY, _cityName);
        PlayerPrefs.Save();

        string url = $"{WEATHER_API_URL}?q={_cityName}&appid={openWeatherMapApiKey}&units=metric";
        StartCoroutine(CallWeatherAPI(url));

        if (confirmSavePanel != null) confirmSavePanel.SetActive(false);
    }

    public void DeclineSaveCity()
    {
        _cityName = _pendingCityName;

        string url = $"{WEATHER_API_URL}?q={_cityName}&appid={openWeatherMapApiKey}&units=metric";
        StartCoroutine(CallWeatherAPI(url));

        if (confirmSavePanel != null) confirmSavePanel.SetActive(false);
    }

    private IEnumerator GetWeatherFromOpenWeather(float lat, float lon)
    {
        string url = $"{WEATHER_API_URL}?lat={lat}&lon={lon}&appid={openWeatherMapApiKey}&units=metric";
        yield return CallWeatherAPI(url);
    }

    private IEnumerator CallWeatherAPI(string url)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                OpenWeatherResponse response = JsonUtility.FromJson<OpenWeatherResponse>(webRequest.downloadHandler.text);

                if (!string.IsNullOrEmpty(response.name))
                    _cityName = response.name;

                ProcessWeatherResponse(response);
            }
        }
    }

    private void ProcessWeatherResponse(OpenWeatherResponse response)
    {
        if (response.weather == null || response.weather.Length == 0) return;

        if (weatherText != null)
        {
            weatherText.text = $"{_cityName}, {Mathf.RoundToInt(response.main.temp)}°C, <color=#0C1D46>{response.weather[0].main}</color>";
        }

        int weatherId = response.weather[0].id;
        AbstractWeather newState = null;

        if (weatherId >= 200 && weatherId <= 531) newState = new Weather_Rainy();
        else if (weatherId >= 600 && weatherId <= 622) newState = new Weather_Rainy();
        else newState = new Weather_Sunny();

        SetWeather(newState);
    }

    private void SetWeather(AbstractWeather newState)
    {
        if (CurrentRealWorldWeather != null)
        {
            CurrentRealWorldWeather.OnExit(this);
        }

        OnWeatherChanged?.Invoke(newState);

        CurrentRealWorldWeather = newState;
        CurrentRealWorldWeather.OnEnter(this);
    }

    private void RegisterEventButton()
    {
        if (ButtonAIOption != null)
            this.ButtonAIOption.onClick.AddListener(this.OpenAiOptionPopupView);
    }

    private void OpenAiOptionPopupView()
    {
        this.OnOpenAiOption?.Invoke();
    }

    public void ForceChangeToRainWeather()
    {
        SetWeather(new Weather_Rainy());
    }

    public void ForceChangeToSunnyWeather()
    {
        SetWeather(new Weather_Sunny());
    }
}
using System.Collections.Generic;
using System;

[Serializable]
public class IPGeoData
{
    public string city;
    public string country;
    public float lat;
    public float lon;
}

[Serializable]
public class OpenWeatherResponse
{
    public string name;
    public WeatherInfo[] weather;
    public MainInfo main;
}

[Serializable]
public class WeatherInfo
{
    public int id;
    public string main;
    public string description;
}

[Serializable]
public class MainInfo
{
    public float temp;
}

[Serializable]
public class RestCountryWrapper
{
    public List<RestCountryItem> items;
}

[Serializable]
public class RestCountryItem
{
    public RestCountryName name;
    public string cca2;
}

[Serializable]
public class RestCountryName
{
    public string common;
}

[Serializable]
public class GeoDBCityResponse
{
    public List<GeoDBCityItem> data;
}

[Serializable]
public class GeoDBCityItem
{
    public string city;
    public string countryCode;
}
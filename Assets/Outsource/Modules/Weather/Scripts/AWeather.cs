using UnityEngine;

public abstract class AbstractWeather
{
    public abstract void OnEnter(ManagerInGame_Weather manager);
    public abstract void OnExit(ManagerInGame_Weather manager);
}

public class Weather_Sunny : AbstractWeather
{
    public override void OnEnter(ManagerInGame_Weather manager)
    {
        Debug.Log("Weather Changed: SUNNY");
    }

    public override void OnExit(ManagerInGame_Weather manager) { }
}

public class Weather_Rainy : AbstractWeather
{
    public override void OnEnter(ManagerInGame_Weather manager)
    {
        Debug.Log("Weather Changed: RAINY");
    }

    public override void OnExit(ManagerInGame_Weather manager) { }
}
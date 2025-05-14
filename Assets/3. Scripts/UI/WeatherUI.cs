using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeatherUI : UIBehaviour
{
    [Header("Weather UI Settings")]
    [SerializeField]
    private WeatherUnit weatherUnitPrefab;
    [SerializeField]
    private Transform weatherUnitParent;
    [SerializeField]
    private Transform startPoint;
    [SerializeField]
    private Transform endPoint;


    public void Initialize()
    {
        var unitCount = 6;
        var increment = (startPoint.position.x - endPoint.position.x) / unitCount;
        var x = endPoint.position.x + increment;
        for (int i = -2; i < 4; i++)
        {
            var weatherUnit = Instantiate(weatherUnitPrefab, weatherUnitParent);
            var hour = TimeManager.Main.CurrentHour + i;
            if (hour >= 24) hour -= 24;
            weatherUnit.Initialize(hour.ToString("D2") + ":00", WeatherManager.Main.GetWeatherForcast(i) >= 0.5 ? "Rainy" : "Clear",
                new Vector2(x, startPoint.position.y), endPoint.position, TimeManager.Main.HourDuration * (3 + i));
            x += increment;
        }

        TimeManager.Main.OnHourChanged.AddListener(OnHourChanged);
    }

    private void OnHourChanged(int currentHour)
    {
        var weatherUnit = Instantiate(weatherUnitPrefab, weatherUnitParent);
        var hour = currentHour + 3;
        if (hour >= 24) hour -= 24;
        weatherUnit.Initialize(hour.ToString("D2") + ":00", WeatherManager.Main.GetWeatherForcast(3) >= 0.5 ? "Rainy" : "Clear",
                startPoint.position, endPoint.position, TimeManager.Main.HourDuration * 6);
    }
}
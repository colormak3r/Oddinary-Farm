using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.DebugUI;

public class WeatherManager : NetworkBehaviour
{
    public static WeatherManager Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    [Header("Required Components")]
    [SerializeField]
    private WeatherUI weatherUI;

    [Header("Light Settings")]
    [SerializeField]
    private Light2D sunlightLight;
    [SerializeField]
    private Gradient dryLightColor;
    [SerializeField]
    private Gradient rainLightColor;
    [SerializeField]
    private float transitionSpeed = 1f;

    [Header("Rain Settings")]
    [SerializeField]
    private ParticleSystem rainParticleSystem;
    [SerializeField]
    private float rainThreshold = 0.5f;
    [SerializeField]
    private Vector2 origin;
    [SerializeField]
    private Vector2 dimension;
    [SerializeField]
    private float scale;
    [SerializeField]
    private int octaves;
    [SerializeField]
    private float persistence;
    [SerializeField]
    private float frequency;
    [SerializeField]
    private float exp;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private int offset = 0;
    private bool isInitialized = false;

    private bool isRainning = false;
    private bool isRainning_cached = false;
    [SerializeField]
    private List<float> weatherData = new List<float>();

    [HideInInspector]
    public UnityEvent OnRainStarted;
    [HideInInspector]
    public UnityEvent OnRainStopped;

    public bool IsRainning => isRainning;

    private TimeManager timeManager;

    private void Start()
    {
        timeManager = TimeManager.Main;
    }

    public void Initialize()
    {
        var totalHours = (float)timeManager.CurrentTimeSpan.TotalHours;
        var currentHour = timeManager.CurrentTimeSpan.Hours;
        offset = currentHour;

        // Populate data for the current day (24 hours)
        var builder = "";
        for (int i = -currentHour; i < 24 - currentHour; i++)
        {
            var value = (float)System.Math.Round(WorldGenerator.GetNoise(0, totalHours + i,
                origin, dimension, scale, octaves, persistence, frequency, exp), 2);
            weatherData.Add(value);
            builder += $"Hour {currentHour + i} = {value}\n";
        }
        if (showDebugs) Debug.Log(builder);

        // Populate data for the next 7 days (168 hours) and calculate daily averages
        int totalDays = 7;
        int hoursPerDay = 24;
        var count = 0;
        var day = 1; // Starting day count from 1 for readability
        var sum = 0f;

        for (int i = 24 - currentHour; i < 24 - currentHour + hoursPerDay * totalDays; i++)
        {
            var value = (float)System.Math.Round(WorldGenerator.GetNoise(0, totalHours + i,
                origin, dimension, scale, octaves, persistence, frequency, exp), 2);
            weatherData.Add(value);
            sum += value;
            count++;

            if (count == hoursPerDay)
            {
                if (showDebugs) Debug.Log($"Day {day} average = {sum / hoursPerDay}");
                count = 0;
                sum = 0;
                day++;
            }
        }

        weatherUI.Initialize();

        isInitialized = true;
    }

    public float GetWeatherForcast(int hourAhead)
    {
        return weatherData[offset + hourAhead];
    }

    private float GetAverageRainChance(int dayIndex)
    {
        var range = 24f;
        if (dayIndex + 24 > weatherData.Count)
            range = weatherData.Count - dayIndex;

        var sum = 0f;
        for (int i = dayIndex; i < dayIndex + range; i++)
        {
            sum += weatherData[i];
        }
        return sum / range;
    }

    private void OnEnable()
    {
        TimeManager.Main.OnHourChanged.AddListener(UpdateRainValue);
    }

    private void OnDisable()
    {
        TimeManager.Main.OnHourChanged.RemoveListener(UpdateRainValue);
    }

    private void Update()
    {
        UpdateGlobalLight();
    }

    private void UpdateGlobalLight()
    {
        var timeRatio = (timeManager.CurrentTimeSpan.Minutes + timeManager.CurrentTimeSpan.Hours * 60) / (float)TimeManager.MINUTES_A_DAY;
        if (isRainning)
            sunlightLight.color = Color.Lerp(sunlightLight.color, rainLightColor.Evaluate(timeRatio), Time.deltaTime * transitionSpeed);
        else
            sunlightLight.color = Color.Lerp(sunlightLight.color, dryLightColor.Evaluate(timeRatio), Time.deltaTime * transitionSpeed);
    }

    private void UpdateRainValue(int currentHour)
    {
        // Generate new rain value using Perlin noise
        if (currentHour == 0)
        {
            var totalHours = (float)timeManager.CurrentTimeSpan.TotalHours;
            var sum = 0f;
            for (int i = 0; i < 24; i++)
            {
                weatherData.RemoveAt(0);
                var value = (float)System.Math.Round(WorldGenerator.GetNoise(0, 7 * 24 + totalHours + i,
                origin, dimension, scale, octaves, persistence, frequency, exp), 2);
                weatherData.Add(value);
                sum += value;
            }

            if (showDebugs)
            {
                var builder = "";
                sum = 0f;
                for (int i = -currentHour; i < 24 - currentHour; i++)
                {
                    var value = (float)System.Math.Round(WorldGenerator.GetNoise(0, totalHours + i,
                        origin, dimension, scale, octaves, persistence, frequency, exp), 2);
                    sum += value;
                    builder += $"Hour {currentHour + i} = {value}\n";
                }
                Debug.Log(builder);
                Debug.Log($"Today average = {sum / 24}");
            }
        }
        if (showDebugs)
        {
            if (currentHour % 4 == 0)
                Debug.Log($"Hour {currentHour} = {weatherData[currentHour]}");
        }

        // Update current weather based on the new current hour
        isRainning = weatherData[currentHour] > rainThreshold;

        if (isRainning != isRainning_cached)
        {
            isRainning_cached = isRainning;
            if (isRainning)
            {
                OnRainStarted?.Invoke();
                rainParticleSystem.Play();
            }
            else
            {
                OnRainStopped?.Invoke();
                rainParticleSystem?.Stop();
            }
        }
    }
}



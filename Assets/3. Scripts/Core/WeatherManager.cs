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

    public static int TOTAL_DAY = 7;
    public static int HOURS_PER_DAY = 24;

    [System.Serializable]
    private struct WeatherData
    {
        public int time;
        public float rainChance;

        public WeatherData(int time, float rainChance)
        {
            this.time = time;
            this.rainChance = rainChance;
        }
    }


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
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    [SerializeField]
    private bool isRainning = false;
    private bool isRainning_cached = false;
    private List<WeatherData> weatherData = new List<WeatherData>();

    [HideInInspector]
    public UnityEvent OnRainStarted;
    [HideInInspector]
    public UnityEvent OnRainStopped;

    public bool IsRainning => isRainning;

    private TimeManager timeManager;

    public void Initialize(TimeManager timeManager)
    {
        this.timeManager = timeManager;

        var totalHours = (float)timeManager.CurrentTimeSpan.TotalHours;
        var currentHour = timeManager.CurrentTimeSpan.Hours;

        var builder = "";
        for (int i = -currentHour; i < 24 - currentHour + HOURS_PER_DAY * TOTAL_DAY; i++)
        {
            var value = GetWeatherData(totalHours + i);
            weatherData.Add(new WeatherData(currentHour + i, value));
            builder += $"Hour {(currentHour + i) %24} = {value}\n";
        }
        if (showDebugs) Debug.Log(builder);

        weatherUI.Initialize();

        isInitialized = true;
    }

    private float GetWeatherData(float value)
    {
        return (float)System.Math.Round(WorldGenerator.GetNoise(0, value,
                origin, dimension, scale, octaves, persistence, frequency, exp), 2);
    }

    public float GetWeatherForcast(int hourAhead)
    {
        var currentHour = timeManager.CurrentTimeSpan.Hours;
        if (showDebugs) Debug.Log($"Forecast {weatherData[currentHour + hourAhead].time} = {weatherData[currentHour + hourAhead].rainChance}");
        return weatherData[currentHour + hourAhead].rainChance;
    }

    private float GetAverageRainChance(int dayIndex)
    {
        var range = 24f;
        if (dayIndex + 24 > weatherData.Count)
            range = weatherData.Count - dayIndex;

        var sum = 0f;
        for (int i = dayIndex; i < dayIndex + range; i++)
        {
            sum += weatherData[i].rainChance;
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
        if (!isInitialized) return;

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
        if (!isInitialized) return;

        // Generate new rain value using Perlin noise
        if (currentHour == 0)
        {
            var totalHours = (float)timeManager.CurrentTimeSpan.TotalHours;
            for (int i = 0; i < 24; i++)
            {
                weatherData.RemoveAt(0);
                var value = GetWeatherData(HOURS_PER_DAY * TOTAL_DAY + totalHours + i);
                weatherData.Add(new WeatherData(i, value));
            }

            if (showDebugs)
            {
                var builder = "";
                for (int i = 0; i < weatherData.Count; i++)
                {
                    builder += $"Hour {currentHour + i} = {weatherData[i].rainChance}\n";
                }
                Debug.Log(builder);
            }
        }

        // Update current weather based on the new current hour
        isRainning = weatherData[currentHour].rainChance >= rainThreshold;
        if (showDebugs) Debug.Log($"Hour {currentHour} = {weatherData[currentHour].rainChance} - Rainning: {isRainning}");

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



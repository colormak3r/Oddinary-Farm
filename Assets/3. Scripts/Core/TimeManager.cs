using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class TimeManager : NetworkBehaviour
{
    public static TimeManager Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    public static int SECONDS_A_DAY = 86400;
    public static int SECONDS_AN_HOUR = 3600;
    public static int SECONDS_A_MINUTE = 60;
    public static int MINUTES_A_DAY = 1440;

    [Header("Settings")]
    [SerializeField]
    private float timeScale = 1;
    [SerializeField]
    private float dayStartTime = 6;
    public bool IsDay => hour_cached >= dayStartTime && hour_cached < nightStartTime;
    [SerializeField]
    private float nightStartTime = 20;
    public bool IsNight => hour_cached >= nightStartTime || hour_cached < dayStartTime;

    [Header("Offset")]
    [SerializeField]
    private int dayOffset = 1;
    [SerializeField]
    private int hourOffset = 4;
    [SerializeField]
    private int minuteOffset = 30;

    [Header("Required Components")]
    [SerializeField]
    private TMP_Text timeText;

    [Header("Debugs")]
    [SerializeField]
    private float runTime;
    private TimeSpan timeSpan;
    private int day_cached = -1;
    private int hour_cached = -1;
    private int minute_cached = -1;

    private bool isInitialized = false;

    private NetworkManager networkManager;

    public TimeSpan CurrentTimeSpan => timeSpan;
    public int CurrentDay => timeSpan.Days;
    public int CurrentHour => timeSpan.Hours;
    public float HourDuration => SECONDS_AN_HOUR / timeScale;
    public bool IsInitialized => isInitialized;

    [HideInInspector]
    public UnityEvent<int> OnDayChanged;
    [HideInInspector]
    public UnityEvent<int> OnHourChanged;
    [HideInInspector]
    public UnityEvent OnDayStart;
    [HideInInspector]
    public UnityEvent OnNightStart;

    private void Start()
    {
        day_cached = dayOffset;
        hour_cached = hourOffset;
        minute_cached = -1;
    }

    public override void OnNetworkSpawn()
    {
        networkManager = NetworkManager.Singleton;
        UpdateTime();
        isInitialized = true;

        WeatherManager.Main.Initialize();
    }

    private void Update()
    {
        if (!isInitialized) return;
        UpdateTime();
    }

    private void UpdateTime()
    {
        // Get runtime from the server
        runTime = (float)networkManager.ServerTime.Time;

        // Get runtime after offset
        var offsetTime = (runTime +
            dayOffset * SECONDS_A_DAY / timeScale +
            hourOffset * SECONDS_AN_HOUR / timeScale +
            minuteOffset * SECONDS_A_MINUTE / timeScale)
            * timeScale;

        // Get timeSpan and display it in Day 1 - 12:30 format
        timeSpan = TimeSpan.FromSeconds(offsetTime);
        if (timeSpan.Minutes % 10 == 0 && timeSpan.Minutes != minute_cached)
        {
            timeText.text = $"Day {timeSpan.Days} - " + timeSpan.ToString(@"hh\:mm");
            minute_cached = timeSpan.Minutes;
        }

        // Trigger corresponding day our hour change events
        if (timeSpan.Days != day_cached)
        {
            day_cached = timeSpan.Days;
            OnDayChanged?.Invoke(day_cached);
        }

        if (timeSpan.Hours != hour_cached)
        {
            hour_cached = timeSpan.Hours;
            OnHourChanged?.Invoke(hour_cached);

            if (hour_cached == nightStartTime)
            {
                OnNightStart?.Invoke();
            }
            else if (hour_cached == dayStartTime)
            {
                OnDayStart?.Invoke();
            }
        }
    }
}


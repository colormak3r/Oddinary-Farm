/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/02/2025 (Khoa)
 * Notes:           <write here>
*/

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

        day_cached = DayOffset.Value;
        hour_cached = HourOffset.Value;
        minute_cached = -1;
    }

    public static int SECONDS_A_DAY = 86400;
    public static int SECONDS_AN_HOUR = 3600;
    public static int SECONDS_A_MINUTE = 60;
    public static int MINUTES_A_DAY = 1440;

    [Header("Settings")]
    [SerializeField]
    private NetworkVariable<float> RealMinutesPerInGameDay = new NetworkVariable<float>(5);

    [SerializeField]
    private float dayStartTime = 6;
    public bool IsDay => hour_cached >= dayStartTime && hour_cached < nightStartTime;
    [SerializeField]
    private float nightStartTime = 20;
    public bool IsNight => hour_cached >= nightStartTime || hour_cached < dayStartTime;

    [Header("Offset")]
    [SerializeField]
    private NetworkVariable<int> DayOffset = new NetworkVariable<int>(1);
    [SerializeField]
    private NetworkVariable<int> HourOffset = new NetworkVariable<int>(7);
    [SerializeField]
    private NetworkVariable<int> MinuteOffset = new NetworkVariable<int>(30);

    [Header("Required Components")]
    [SerializeField]
    private TMP_Text timeText;

    [Header("Debugs")]
    [SerializeField]
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    [SerializeField]
    private float runTime;
    private TimeSpan timeSpan;
    private int day_cached = -1;
    private int hour_cached = -1;
    private int minute_cached = -1;
    private float timeScale => MINUTES_A_DAY / RealMinutesPerInGameDay.Value;

    private NetworkManager networkManager;

    public TimeSpan CurrentTimeSpan => timeSpan;
    public int CurrentDate => timeSpan.Days;
    public int CurrentHour => timeSpan.Hours;
    public float HourDuration => SECONDS_AN_HOUR / timeScale;

    [HideInInspector]
    public UnityEvent<int> OnDateChanged;
    [HideInInspector]
    public UnityEvent<int> OnHourChanged;
    [HideInInspector]
    public UnityEvent OnDayStart;
    [HideInInspector]
    public UnityEvent OnNightStart;

    public void Initialize()
    {
        networkManager = NetworkManager.Singleton;
        UpdateTime();

        // Initialize WeatherManager
        WeatherManager.Main.Initialize(this);

        isInitialized = true;
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
            DayOffset.Value * SECONDS_A_DAY / timeScale +
            HourOffset.Value * SECONDS_AN_HOUR / timeScale +
            MinuteOffset.Value * SECONDS_A_MINUTE / timeScale)
            * timeScale;

        // Get timeSpan and display it in Day 1 - 12:30 format
        timeSpan = TimeSpan.FromSeconds(offsetTime);
        if (timeSpan.Minutes % 10 == 0 && timeSpan.Minutes != minute_cached)
        {
            // Create a base DateTime (e.g., midnight) and add the TimeSpan
            DateTime dateTime = DateTime.Today.Add(timeSpan);

            // Format using 12-hour time with AM/PM
            string formattedTime = dateTime.ToString("hh:mm tt"); // e.g., "03:45 PM"

            timeText.text = $"Day {timeSpan.Days} - {formattedTime}";
            minute_cached = timeSpan.Minutes;
        }

        // Trigger corresponding day our hour change events
        if (timeSpan.Days != day_cached)
        {
            day_cached = timeSpan.Days;
            OnDateChanged?.Invoke(day_cached);
        }

        if (timeSpan.Hours != hour_cached)
        {
            hour_cached = timeSpan.Hours;

            if (hour_cached == nightStartTime)
            {
                OnNightStart?.Invoke();
            }
            else if (hour_cached == dayStartTime)
            {
                OnDayStart?.Invoke();
            }

            OnHourChanged?.Invoke(hour_cached);
        }
    }

    #region Utility
    public void SetRealMinutesPerDay(float realMinutesPerDay)
    {
        SetRealMinutesPerDayRpc(realMinutesPerDay);
    }

    [Rpc(SendTo.Server)]
    private void SetRealMinutesPerDayRpc(float realMinutesPerDay)
    {
        RealMinutesPerInGameDay.Value = realMinutesPerDay;
    }

    public void SetTimeOffset(int dayOffset, int hourOffset, int minuteOffset)
    {
        SetTimeOffSetRpc(dayOffset, hourOffset, minuteOffset);
    }

    [Rpc(SendTo.Server)]
    private void SetTimeOffSetRpc(int dayOffset, int hourOffset, int minuteOffset)
    {
        DayOffset.Value = dayOffset;
        HourOffset.Value = hourOffset;
        MinuteOffset.Value = minuteOffset;
    }
    #endregion
}


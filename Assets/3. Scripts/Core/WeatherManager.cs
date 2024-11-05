using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class WeatherManager : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField]
    private Light2D sunlightLight;
    [SerializeField]
    private Gradient dryLightColor;
    [SerializeField]
    private Gradient rainLightColor;
    [SerializeField]
    private float transitionSpeed = 1f;

    private bool isRainning = false;



    private TimeManager timeManager;

    private void Start()
    {
        timeManager = TimeManager.Main;
    }

    private void Update()
    {
        UpdateGlobalLight();
    }

    private void UpdateGlobalLight()
    {
        var timeRatio = (timeManager.CurrentTimeSpan.Minutes + timeManager.CurrentTimeSpan.Hours * 60) / 1440f;
        sunlightLight.color = Color.Lerp(sunlightLight.color, rainLightColor.Evaluate(timeRatio), Time.deltaTime * transitionSpeed);
    }
}

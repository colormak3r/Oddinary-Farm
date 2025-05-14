using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

public class LightStructure : Structure
{
    [Header("Light Structure")]
    [SerializeField] private int turnOnTime = 19;
    [SerializeField] private int turnOffTime = 5;
    [SerializeField] private float lightIntensity = 0.5f;
    [SerializeField] private float lightIntensityMin = 0.45f;
    [SerializeField] private float lightIntensityMax = 0.55f;
    [SerializeField] private Light2D lightSource;
    [SerializeField] private ParticleSystem lightParticleSystem;

    [Header("Debugs")]
    [SerializeField] private bool isLightOn = false;

    private Coroutine lightCoroutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        TimeManager.Main.OnHourChanged.AddListener(OnHourChanged);
        OnHourChanged(TimeManager.Main.CurrentHour);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        TimeManager.Main.OnHourChanged.RemoveListener(OnHourChanged);
    }

    private void OnHourChanged(int currentHour)
    {
        if (currentHour >= turnOnTime || currentHour < turnOffTime)
        {
            SetLight(true);
        }
        else
        {
            if (!WeatherManager.Main.IsRainning)
                SetLight(false);
            else
                SetLight(true);
        }
    }

    public void SetLight(bool value)
    {
        isLightOn = value;

        if (lightCoroutine != null) StopCoroutine(lightCoroutine);
        if (value)
        {
            lightCoroutine = StartCoroutine(LightOnCoroutine(lightIntensity));
        }
        else
        {
            lightCoroutine = StartCoroutine(LightOffCoroutine());
        }
    }


    private IEnumerator LightOnCoroutine(float intensity)
    {
        lightSource.enabled = true;
        lightParticleSystem.Play();
        isLightOn = true;

        float duration = 1f;
        float elapsed = 0f;
        float startIntensity = lightSource.intensity;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            lightSource.intensity = Mathf.Lerp(startIntensity, intensity, elapsed / duration);
            yield return null;
        }
        lightSource.intensity = intensity;


        // Start flickering
        float nextIntensityChange = 0;
        float flickerIntensity = Random.Range(lightIntensityMin, lightIntensityMax);
        while (isLightOn)
        {
            if (Time.time > nextIntensityChange)
            {
                flickerIntensity = Random.Range(lightIntensityMin, lightIntensityMax);
                nextIntensityChange = Time.time + Random.Range(0.1f, 0.5f);
            }

            lightSource.intensity = Mathf.Lerp(lightSource.intensity, flickerIntensity, Time.deltaTime * 5f);
            yield return null;
        }
    }

    private IEnumerator LightOffCoroutine()
    {
        float duration = 1f;
        float elapsed = 0f;
        float startIntensity = lightSource.intensity;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            lightSource.intensity = Mathf.Lerp(startIntensity, 0, elapsed / duration);
            yield return null;
        }

        lightSource.intensity = 0;
        lightSource.enabled = false;
        lightParticleSystem.Stop();
        isLightOn = false;
    }


    [ContextMenu("Set Light On")]
    private void SetLightOn()
    {
        SetLight(true);
    }

    [ContextMenu("Set Light Off")]
    private void SetLightOff()
    {
        SetLight(false);
    }
}

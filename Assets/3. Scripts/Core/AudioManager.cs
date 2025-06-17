using ColorMak3r.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum SoundEffect
{
    UIClick,
    UIHover,
    UIError
}

public class AudioManager : MonoBehaviour
{
    private static string BGM_VOLUME_VALUE_STRING = "BGM_VOLUME_VALUE";
    private static string SFX_VOLUME_VALUE_STRING = "SFX_VOLUME_VALUE";
    private static string ABM_VOLUME_VALUE_STRING = "ABM_VOLUME_VALUE";

    public static AudioManager Main;

    [Header("Audio Settings")]
    [SerializeField]
    private float transitionDuration = 0.5f;
    [SerializeField]
    private float combatMusicRange = 20f;
    public float CombatMusicRange => combatMusicRange;
    [SerializeField]
    private float combatMusicDuration = 5f;
    [SerializeField]
    private AudioSource bgmAudioSource;
    [SerializeField]
    private AudioSource sfxAudioSource;
    [SerializeField]
    private AudioSource abmAudioSource;
    [SerializeField]
    private AudioSource pitchAudioSource;

    [Header("Sound Effects")]
    [SerializeField]
    private AudioClip uiClickSfx;
    [SerializeField]
    private AudioClip uiHoverSfx;
    [SerializeField]
    private AudioClip uiErrorSfx;

    [Header("Music Tracks")]
    [SerializeField]
    private AudioClip menuMusic;
    [SerializeField]
    private AudioClip grasslandDayMusic;
    [SerializeField]
    private AudioClip grasslandNightMusic;
    [SerializeField]
    private AudioClip oceanDayMusic;
    [SerializeField]
    private AudioClip oceanNightMusic;
    [SerializeField]
    private AudioClip rainDayMusic;
    [SerializeField]
    private AudioClip rainNightMusic;
    [SerializeField]
    private AudioClip combatMusic;

    [Header("Ambient Tracks")]
    [SerializeField]
    private AudioClip grasslandDayAmbient;
    [SerializeField]
    private AudioClip grasslandNightAmbient;
    [SerializeField]
    private AudioClip oceanDayAmbient;
    [SerializeField]
    private AudioClip oceanNightAmbient;
    [SerializeField]
    private AudioClip rainDayAmbient;
    [SerializeField]
    private AudioClip rainNightAmbient;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private bool isInCombat;

    private float currentElevation = 1.0f;
    private float oceanElevationThreshold = 0.5f;

    [HideInInspector]
    public UnityEvent<float> OnSfxVolumeChange;

    public float BgmVolume
    {
        get { return bgmAudioSource.volume; }
        set
        {
            value = Mathf.Clamp01(value);
            bgmAudioSource.volume = value;
            PlayerPrefs.SetFloat(BGM_VOLUME_VALUE_STRING, value);
        }
    }

    public float SfxVolume
    {
        get { return sfxAudioSource.volume; }
        set
        {
            value = Mathf.Clamp01(value);
            sfxAudioSource.volume = value;
            pitchAudioSource.volume = value; // Sync pitch audio source volume with SFX volume
            PlayerPrefs.SetFloat(SFX_VOLUME_VALUE_STRING, value);
            OnSfxVolumeChange?.Invoke(value);
        }
    }

    public float AbmVolume
    {
        get { return abmAudioSource.volume; }
        set
        {
            value = Mathf.Clamp01(value);
            abmAudioSource.volume = value;
            PlayerPrefs.SetFloat(ABM_VOLUME_VALUE_STRING, value);
        }
    }

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);

        bgmAudioSource.volume = PlayerPrefs.GetFloat(BGM_VOLUME_VALUE_STRING, 0.1f);
        if (showDebugs) Debug.Log("BGM Volume: " + bgmAudioSource.volume);
        sfxAudioSource.volume = PlayerPrefs.GetFloat(SFX_VOLUME_VALUE_STRING, 0.1f);
        if (showDebugs) Debug.Log("SFX Volume: " + sfxAudioSource.volume);
        abmAudioSource.volume = PlayerPrefs.GetFloat(ABM_VOLUME_VALUE_STRING, 0.1f);
        if (showDebugs) Debug.Log("ABM Volume: " + abmAudioSource.volume);
        pitchAudioSource.volume = sfxAudioSource.volume; // Sync pitch audio source volume with SFX volume
        if (showDebugs) Debug.Log("Pitch Audio Source Volume: " + pitchAudioSource.volume);

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnNetworkSpawn()
    {
        oceanElevationThreshold = WorldGenerator.Main.OceanElevationThreshold;
        TimeManager.Main.OnHourChanged.AddListener(OnHourChanged);
        OnHourChanged(TimeManager.Main.CurrentHour);
    }

    public void OnNetworkDespawn()
    {
        TimeManager.Main.OnHourChanged.RemoveListener(OnHourChanged);
        abmAudioSource.Stop();
    }

    private void OnHourChanged(int currentHour)
    {
        UpdateMusicNAmbientTracks();
    }

    public void OnElevationUpdated(float elevation)
    {
        currentElevation = elevation;
        UpdateMusicNAmbientTracks();
    }

    private void UpdateMusicNAmbientTracks()
    {
        if (isInCombat)
        {
            PlayBackgroundMusic(combatMusic);
            if (TimeManager.Main.IsDay)
            {
                // Day time logic
                if (currentElevation > oceanElevationThreshold)
                {
                    if (showDebugs) Debug.Log("Playing grassland day ambient and music in combat");
                    PlayAmbientSound(grasslandDayAmbient);
                }
                else
                {
                    if (showDebugs) Debug.Log("Playing ocean day ambient and music in combat");
                    PlayAmbientSound(oceanDayAmbient);
                }
            }
            else
            {
                // Night time logic
                if (currentElevation > oceanElevationThreshold)
                {
                    if (showDebugs) Debug.Log("Playing grassland night ambient and music in combat");
                    PlayAmbientSound(grasslandNightAmbient);

                }
                else
                {
                    if (showDebugs) Debug.Log("Playing ocean night ambient and music in combat");
                    PlayAmbientSound(oceanNightAmbient);
                }
            }
        }
        else if (WeatherManager.Main.IsRainning)
        {
            // When it rain, it has the same logic
            if (TimeManager.Main.IsDay)
            {
                if (showDebugs) Debug.Log("Playing rain day ambient and music");
                PlayAmbientSound(rainDayAmbient);
                PlayBackgroundMusic(rainDayMusic);
            }
            else
            {
                if (showDebugs) Debug.Log("Playing rain night ambient and music");
                PlayAmbientSound(rainNightAmbient);
                PlayBackgroundMusic(rainNightMusic);
            }
        }
        else
        {
            if (TimeManager.Main.IsDay)
            {
                // Day time logic
                if (currentElevation > oceanElevationThreshold)
                {
                    if (showDebugs) Debug.Log("Playing grassland day ambient and music");
                    PlayAmbientSound(grasslandDayAmbient);
                    PlayBackgroundMusic(grasslandDayMusic);
                }
                else
                {
                    if (showDebugs) Debug.Log("Playing ocean day ambient and music");
                    PlayAmbientSound(oceanDayAmbient);
                    PlayBackgroundMusic(oceanDayMusic);
                }
            }
            else
            {
                // Night time logic
                if (currentElevation > oceanElevationThreshold)
                {
                    if (showDebugs) Debug.Log("Playing grassland night ambient and music");
                    PlayAmbientSound(grasslandNightAmbient);
                    PlayBackgroundMusic(grasslandNightMusic);
                }
                else
                {
                    if (showDebugs) Debug.Log("Playing ocean night ambient and music");
                    PlayAmbientSound(oceanNightAmbient);
                    PlayBackgroundMusic(oceanNightMusic);
                }
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0) PlayBackgroundMusic(menuMusic);
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (clip != null)
            sfxAudioSource.PlayOneShot(clip);
    }

    public void PlayOneShot(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0)
            sfxAudioSource.PlayOneShot(clips.GetRandomElement());
    }

    public IEnumerator LerpVolumeCoroutine(AudioSource audioSource, float start, float end, float duration = 0.5f)
    {
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / duration;
            audioSource.volume = Mathf.Lerp(start, end, t);
            yield return null;
        }

        audioSource.volume = end;
    }

    public void PlayClickSound()
    {
        PlaySoundEffect(SoundEffect.UIClick);
    }

    public void PlaySoundEffect(SoundEffect sfx)
    {
        PlayOneShot(GetAudioClip(sfx));
    }

    public void TriggerCombatMusic()
    {
        if (showDebugs) Debug.Log("Triggering combat music");
        nextCombatMusicEnd = Time.time + combatMusicDuration;
        isInCombat = true;
        UpdateMusicNAmbientTracks();
    }

    private float nextCombatMusicEnd = 0f;
    private void Update()
    {
        if (Time.time > nextCombatMusicEnd && isInCombat)
        {
            if (showDebugs) Debug.Log("Combat music ended");
            isInCombat = false;
            UpdateMusicNAmbientTracks();
        }
    }

    #region Background Music

    private Coroutine backgroundTransitionCoroutine;
    private bool backgroundTransitioning = false;
    private void PlayBackgroundMusic(AudioClip clip)
    {
        if (bgmAudioSource.clip == clip || backgroundTransitioning) return;
        if (backgroundTransitionCoroutine != null) StopCoroutine(backgroundTransitionCoroutine);
        backgroundTransitionCoroutine = StartCoroutine(PlayBackgroundMusicCoroutine(clip));
    }

    public IEnumerator PlayBackgroundMusicCoroutine(AudioClip audioClip)
    {
        backgroundTransitioning = true;
        yield return LerpVolumeCoroutine(bgmAudioSource, BgmVolume, 0f, transitionDuration);

        bgmAudioSource.clip = audioClip;
        bgmAudioSource.Play();

        var volume = PlayerPrefs.GetFloat(BGM_VOLUME_VALUE_STRING, 0.5f);
        yield return LerpVolumeCoroutine(bgmAudioSource, 0f, volume, transitionDuration);
        backgroundTransitioning = false;
    }

    #endregion

    #region Ambient Sound

    private Coroutine ambientTransitionCoroutine;
    private bool ambientTransitioning = false;
    private void PlayAmbientSound(AudioClip clip)
    {
        if (abmAudioSource.clip == clip || ambientTransitioning) return;
        if (ambientTransitionCoroutine != null) StopCoroutine(ambientTransitionCoroutine);
        ambientTransitionCoroutine = StartCoroutine(PlayAmbientSoundCoroutine(clip));
    }

    private IEnumerator PlayAmbientSoundCoroutine(AudioClip audioClip)
    {
        ambientTransitioning = true;
        yield return LerpVolumeCoroutine(abmAudioSource, AbmVolume, 0f, transitionDuration);

        abmAudioSource.clip = audioClip;
        abmAudioSource.Play();

        var volume = PlayerPrefs.GetFloat(ABM_VOLUME_VALUE_STRING, 0.5f);
        yield return LerpVolumeCoroutine(abmAudioSource, 0f, volume, transitionDuration);
        ambientTransitioning = false;
    }

    #endregion

    private AudioClip GetAudioClip(SoundEffect sfx)
    {
        switch (sfx)
        {
            case SoundEffect.UIClick:
                return uiClickSfx;
            case SoundEffect.UIHover:
                return uiHoverSfx;
            case SoundEffect.UIError:
                return uiErrorSfx;
            default:
                Debug.LogError("Sound effect not found: " + sfx);
                return null;
        }
    }

    public void ResetPitch()
    {
        pitchAudioSource.pitch = 1f;
    }

    public void PlaySoundIncreasePitch(AudioClip clip)
    {
        pitchAudioSource.PlayOneShot(clip);
        pitchAudioSource.pitch = pitchAudioSource.pitch + 0.1f;
    }
}

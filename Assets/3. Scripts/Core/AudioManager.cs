using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum SoundEffect
{
    UIClick,
    UIHover,
    UIError
}

public enum MusicTrack
{
    MainMenu,
    MainGame,
}

public enum AmbientTrack
{
    Day,
    Night,
    Rain
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
    private AudioSource bgmAudioSource;
    [SerializeField]
    private AudioSource sfxAudioSource;
    [SerializeField]
    private AudioSource abmAudioSource;

    [Header("Sound Effects")]
    [SerializeField]
    private AudioClip uiClickSfx;
    [SerializeField]
    private AudioClip uiHoverSfx;
    [SerializeField]
    private AudioClip uiErrorSfx;

    [Header("Music Tracks")]
    [SerializeField]
    private AudioClip mainMenuMusic;
    [SerializeField]
    private AudioClip mainGameMusic;

    [Header("Ambient Tracks")]
    [SerializeField]
    private AudioClip dayClip;
    [SerializeField]
    private AudioClip nightClip;
    [SerializeField]
    private AudioClip rainClip;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;

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

        bgmAudioSource.volume = PlayerPrefs.GetFloat(BGM_VOLUME_VALUE_STRING, 0.5f);
        if (showDebugs) Debug.Log("BGM Volume: " + bgmAudioSource.volume);
        sfxAudioSource.volume = PlayerPrefs.GetFloat(SFX_VOLUME_VALUE_STRING, 0.5f);
        if (showDebugs) Debug.Log("SFX Volume: " + sfxAudioSource.volume);
        abmAudioSource.volume = PlayerPrefs.GetFloat(ABM_VOLUME_VALUE_STRING, 0.5f);
        if (showDebugs) Debug.Log("ABM Volume: " + abmAudioSource.volume);

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
            StartCoroutine(PlayBackgroundMusicCoroutine(MusicTrack.MainMenu));
        else
            StartCoroutine(PlayBackgroundMusicCoroutine(MusicTrack.MainGame));
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

    public IEnumerator PlayBackgroundMusicCoroutine(MusicTrack musicTrack)
    {
        yield return LerpVolumeCoroutine(bgmAudioSource, BgmVolume, 0f, transitionDuration);

        bgmAudioSource.clip = GetAudioClip(musicTrack);
        bgmAudioSource.Play();

        var volume = PlayerPrefs.GetFloat(BGM_VOLUME_VALUE_STRING, 0.5f);
        yield return LerpVolumeCoroutine(bgmAudioSource, 0f, volume, transitionDuration);
    }

    public void PlayClickSound()
    {
        PlaySoundEffect(SoundEffect.UIClick);
    }

    public void PlaySoundEffect(SoundEffect sfx)
    {
        PlayOneShot(GetAudioClip(sfx));
    }

    private AmbientTrack? cachedAmbientTrack = null;
    public void PlayAmbientSound(AmbientTrack ambientTrack)
    {
        if (cachedAmbientTrack == ambientTrack) return;
        cachedAmbientTrack = ambientTrack;

        StartCoroutine(PlayAmbientSoundCoroutine(ambientTrack));
    }

    private IEnumerator PlayAmbientSoundCoroutine(AmbientTrack ambientTrack)
    {
        yield return LerpVolumeCoroutine(abmAudioSource, AbmVolume, 0f, transitionDuration);

        abmAudioSource.clip = GetAudioClip(ambientTrack);
        abmAudioSource.Play();

        var volume = PlayerPrefs.GetFloat(ABM_VOLUME_VALUE_STRING, 0.5f);
        yield return LerpVolumeCoroutine(abmAudioSource, 0f, volume, transitionDuration);
    }

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

    private AudioClip GetAudioClip(MusicTrack track)
    {
        switch (track)
        {
            case MusicTrack.MainMenu:
                return mainMenuMusic;
            case MusicTrack.MainGame:
                return mainGameMusic;
            default:
                Debug.LogError("Music track not found: " + track);
                return null;
        }
    }

    private AudioClip GetAudioClip(AmbientTrack track)
    {
        switch (track)
        {
            case AmbientTrack.Day:
                return dayClip;
            case AmbientTrack.Night:
                return nightClip;
            case AmbientTrack.Rain:
                return rainClip;
            default:
                Debug.LogError("Ambient track not found: " + track);
                return null;
        }
    }
}

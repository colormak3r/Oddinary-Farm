/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/05/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioElement : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField]
    private bool ignoreWarnings = false;
    [SerializeField]
    private bool playOnAwake = false;
    [SerializeField]
    private AudioClip[] soundEffects;

    private AudioSource audioSource;

    private bool isInitialized;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource.rolloffMode != AudioRolloffMode.Linear && !ignoreWarnings)
        {
            Debug.LogWarning($"{gameObject} audioSource rolloff mode is not set to Linear. This may cause unexpected audio behavior.", this);
        }
        if (audioSource.spatialBlend != 1 && !ignoreWarnings)
        {
            Debug.LogWarning($"{gameObject} audioSource spatial blend is not set to 3D. This may cause unexpected audio behavior.", this);
        }
        if (audioSource.minDistance != 10 && !ignoreWarnings)
        {
            Debug.LogWarning($"{gameObject} audioSource minDistance is not 10. This may cause unexpected audio behavior.", this);
        }
        if (audioSource.maxDistance != 25 && !ignoreWarnings)
        {
            Debug.LogWarning($"{gameObject} audioSource maxDistance is not 25. This may cause unexpected audio behavior.", this);
        }
    }

    private void Start()
    {
        AudioManager.Main.OnSfxVolumeChange.AddListener(HandleSfxVolumeChange);
        HandleSfxVolumeChange(AudioManager.Main.SfxVolume);
        isInitialized = true;
        if (playOnAwake && soundEffects.Length > 0)
            PlayOneShot(soundEffects[0], true);
    }

    private void OnDestroy()
    {
        if (AudioManager.Main != null)
            AudioManager.Main.OnSfxVolumeChange.RemoveListener(HandleSfxVolumeChange);
    }

    private void HandleSfxVolumeChange(float volume)
    {
        audioSource.volume = volume;
    }

    public void PlayOneShot(AudioClip clip, bool randomPitch = true)
    {
        if (clip != null)
        {
            if (randomPitch) audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
            if (!isInitialized) HandleSfxVolumeChange(AudioManager.Main.SfxVolume);
            audioSource.PlayOneShot(clip);
        }
    }

    public void PlayOneShot(AudioClip clip, float pitch)
    {
        if (clip != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
        }
    }

    public void PlaySoundEffect(int index)
    {
        audioSource.PlayOneShot(soundEffects[index]);
    }

    public void Stop()
    {
        audioSource.Stop();
    }

    public void SetRange(float minRange, float maxRange)
    {
        audioSource.minDistance = minRange;
        audioSource.maxDistance = maxRange;
    }
}
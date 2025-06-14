using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(AudioSource))]
public class AudioElement : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField]
    private AudioClip[] soundEffects;

    private AudioSource audioSource;

    private bool isInitialized;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        AudioManager.Main.OnSfxVolumeChange.AddListener(HandleSfxVolumeChange);
        HandleSfxVolumeChange(AudioManager.Main.SfxVolume);
        isInitialized = true;
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
}
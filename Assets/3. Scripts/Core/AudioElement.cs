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

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        AudioManager.Main.OnSfxVolumeChange.AddListener(HandleSfxVolumeChange);
    }

    private void OnDestroy()
    {
        AudioManager.Main.OnSfxVolumeChange.RemoveListener(HandleSfxVolumeChange);
    }

    private void HandleSfxVolumeChange(float volume)
    {
        audioSource.volume = volume;
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void PlaySoundEffect(int index)
    {
        audioSource.PlayOneShot(soundEffects[index]);
    }
}
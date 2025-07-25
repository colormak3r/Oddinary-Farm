using UnityEngine;
using Unity.Netcode;
using System;
using ColorMak3r.Utility;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class Radio : Structure, IInteractable
{
    [Header("Radio Settings")]
    [SerializeField]
    private AudioClip cueSfx;
    [SerializeField]
    private AudioClip powerOnSfx;
    [SerializeField]
    private AudioClip powerOffSfx;
    [SerializeField]
    private AudioClip[] soundTracks;
    [SerializeField]
    private Color activeColor;
    [SerializeField]
    private Color inactiveColor;

    private SpriteRenderer spriteRenderer;
    private Light2D light2D;
    private AudioElement audioElement;
    private NetworkVariable<bool> PlaySoundTracks = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    protected override void Awake()
    {
        audioElement = GetComponent<AudioElement>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        light2D = GetComponentInChildren<Light2D>();
    }

    public override void OnNetworkSpawn()
    {
        PlaySoundTracks.OnValueChanged += HandlePlaySoundTracksChanged;
        HandlePlaySoundTracksChanged(false, PlaySoundTracks.Value);
        SetIsRemoveable(RadioManager.Main.IsActivatedValue);
        TimeManager.Main.OnHourChanged.AddListener(OnHourChanged);

        StartCoroutine(CueCoroutine());
    }

    public override void OnNetworkDespawn()
    {
        PlaySoundTracks.OnValueChanged -= HandlePlaySoundTracksChanged;
        TimeManager.Main.OnHourChanged.RemoveListener(OnHourChanged);
    }

    private int hourCounter = 0;
    private void OnHourChanged(int arg0)
    {
        if (PlaySoundTracks.Value)
        {
            if (hourCounter >= 4)
            {
                hourCounter = 0;
                PlayRandomSoundTrack();
            }
            else
            {
                hourCounter++;
            }
        }
    }

    private void HandleIsRemoveableChanged(bool previousValue, bool newValue)
    {
        SetIsRemoveable(newValue);
    }

    private void HandlePlaySoundTracksChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            if (turnOnCoroutine != null) StopCoroutine(turnOnCoroutine);
            turnOnCoroutine = StartCoroutine(TurnOnCoroutine());
        }
        else
        {
            audioElement.Stop();
            audioElement.PlayOneShot(powerOffSfx);
            light2D.enabled = false;
            spriteRenderer.color = inactiveColor;
        }
    }

    private IEnumerator CueCoroutine()
    {
        while (!RadioManager.Main.IsActivatedValue)
        {
            audioElement.PlayOneShot(cueSfx, false);
            float elapsed = 0f;

            // Wait while checking for activation
            while (elapsed < cueSfx.length)
            {
                if (RadioManager.Main.IsActivatedValue)
                {
                    audioElement.Stop(); // Immediately stop the cue
                    yield break;         // Exit the coroutine
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    private Coroutine turnOnCoroutine;
    private IEnumerator TurnOnCoroutine()
    {
        light2D.enabled = true;
        spriteRenderer.color = activeColor;
        audioElement.Stop();
        audioElement.PlayOneShot(powerOnSfx);
        yield return new WaitForSeconds(powerOnSfx.length);
        hourCounter = 0;
        PlayRandomSoundTrack();
    }

    public bool IsHoldInteractable => false;

    public void InteractionEnd(Transform source)
    {
        throw new System.NotImplementedException();
    }

    public void InteractionStart(Transform source)
    {
        throw new System.NotImplementedException();
    }

    public void Interact(Transform source)
    {
        if (RadioManager.Main.IsActivatedValue)
        {
            //Radio is activated, turn music on or off
            PlaySoundTracks.Value = !PlaySoundTracks.Value;
        }
        else
        {
            // Radio is not activated, play activation sequence
            RadioManager.Main.SetActivated();
            PlaySoundTracks.Value = true;
            OnRadioActivatedRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void OnRadioActivatedRpc()
    {
        // Mark this object as no longer observable (not respawn by procedural generation)
        if (IsServer) GetComponent<ObservabilityController>().EndObservabilityOnServer();
        SetIsRemoveable(true);
    }


    [ContextMenu("Play Random Sound Track")]
    private void PlayRandomSoundTrack()
    {
        audioElement.PlayOneShot(soundTracks.GetRandomElement());
    }
}

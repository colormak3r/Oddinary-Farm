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

    // Client authoritative variable
    private NetworkVariable<bool> PlaySoundTracks = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    protected override void Awake()
    {
        audioElement = GetComponent<AudioElement>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        light2D = GetComponentInChildren<Light2D>();
    }

    public override void OnNetworkSpawn()
    {
        // The radio object will handle playing soundtracks locally 
        PlaySoundTracks.OnValueChanged += HandlePlaySoundTracksChanged;
        HandlePlaySoundTracksChanged(false, PlaySoundTracks.Value);

        // The radio is only removeable if it is activated
        SetIsRemoveable(RadioManager.Main.IsActivatedValue);

        // Play music every 5 hours if is turned on
        TimeManager.Main.OnHourChanged.AddListener(HandleOnHourChanged);

        // Play proximity cue sound (radio static) to get player attention if it's not activated
        StartCoroutine(CueCoroutine());
    }

    public override void OnNetworkDespawn()
    {
        PlaySoundTracks.OnValueChanged -= HandlePlaySoundTracksChanged;
        TimeManager.Main.OnHourChanged.RemoveListener(HandleOnHourChanged);
    }

    private int hourCounter = 0;
    private void HandleOnHourChanged(int arg0)
    {
        // Play music every 5 hours if is turned on
        if (PlaySoundTracks.Value)
        {
            if (hourCounter >= 5)
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

    private void HandlePlaySoundTracksChanged(bool previousValue, bool newValue)
    {
        // Anything in callback subcribed to OnValueChanged is always executed on the server and all clients
        // If you want to execute something only on the server, use IsServer check
        // If you want to execute something only on the client, use IsClient check

        if (newValue)
        {
            // If the radio is turned on, play the power on sound then start playing music
            if (turnOnCoroutine != null) StopCoroutine(turnOnCoroutine);
            turnOnCoroutine = StartCoroutine(TurnOnCoroutine());
        }
        else
        {
            // Turn off radio immidiately
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
        // Mark this object as no longer observable (cannot be respawned by procedural generation)
        if (IsServer) GetComponent<ObservabilityController>().EndObservabilityOnServer();

        // Set the radio as removeable on all clients
        SetIsRemoveable(true);
    }


    [ContextMenu("Play Random Sound Track")]
    private void PlayRandomSoundTrack()
    {
        audioElement.PlayOneShot(soundTracks.GetRandomElement());
    }
}

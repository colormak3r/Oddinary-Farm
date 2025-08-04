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
    private NetworkVariable<bool> PlaySoundTracks = new NetworkVariable<bool>(false);

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

        // Play proximity cue sound (radio static) to get player attention if it's not activated
        StartCoroutine(CueCoroutine());
    }

    public override void OnNetworkDespawn()
    {
        PlaySoundTracks.OnValueChanged -= HandlePlaySoundTracksChanged;
    }

    private void HandlePlaySoundTracksChanged(bool previousValue, bool newValue)
    {
        // Anything in callback subcribed to OnValueChanged is always executed on the server and all clients
        // If you want to execute something only on the server, use IsServer check
        // If you want to execute something only on the client, use IsClient check

        if (newValue)
        {
            // If the radio is turned on, play the power on sound then start playing music
            if (playRadioCoroutine != null) StopCoroutine(playRadioCoroutine);
            playRadioCoroutine = StartCoroutine(PlayRadioCoroutine());
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

    private Coroutine playRadioCoroutine;
    private IEnumerator PlayRadioCoroutine()
    {
        light2D.enabled = true;
        spriteRenderer.color = activeColor;

        /// Play the power-on SFX
        audioElement.Stop();
        audioElement.PlayOneShot(powerOnSfx);
        yield return new WaitForSeconds(powerOnSfx.length);

        // Loop while PlaySoundTracks is true
        while (PlaySoundTracks.Value == true)
        {
            var clip = PlayRandomSoundTrack();
            var duration = clip.length;
            float elapsed = 0f;
            while (elapsed < clip.length)
            {
                if (!PlaySoundTracks.Value)
                {
                    audioElement.Stop();
                    yield break; // Exit the coroutine immediately
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
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
            SetPlaySoundTracksRpc(!PlaySoundTracks.Value);
        }
        else
        {
            // Radio is not activated, play activation sequence
            OnRadioActivatedRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void SetPlaySoundTracksRpc(bool value)
    {
        PlaySoundTracks.Value = !PlaySoundTracks.Value;
    }

    [Rpc(SendTo.Everyone)]
    private void OnRadioActivatedRpc()
    {

        // Mark this object as despawned (cannot be respawned by procedural generation)
        if (IsServer)
        {
            PlaySoundTracks.Value = true;
            RadioManager.Main.SetActivatedOnServer();
            GetComponent<ObservabilityController>().DespawnOnServer();
        }

        // Set the radio as removeable on all clients
        SetIsRemoveable(true);
    }


    [ContextMenu("Play Random Sound Track")]
    private AudioClip PlayRandomSoundTrack()
    {
        var clip = soundTracks.GetRandomElement();
        audioElement.PlayOneShot(clip);
        return clip;
    }
}

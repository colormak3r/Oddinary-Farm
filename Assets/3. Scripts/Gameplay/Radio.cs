using UnityEngine;
using Unity.Netcode;
using System;

public class Radio : Structure, IInteractable
{
    private AudioElement audioElement;
    [SerializeField]
    private AudioClip interactClip;
    [SerializeField]
    private AudioClip music1Clip;

    protected override void Awake()
    {
        audioElement = GetComponent<AudioElement>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        TimeManager.Main.OnHourChanged.AddListener(OnHourChanged);
    }

    private void OnHourChanged(int arg0)
    {
        if (arg0 == 10)
        {
            PlayMusic1();
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
        RadioManager.Main.SetActivated();
        Debug.Log("Player interacted with radio");
        audioElement.PlayOneShot(interactClip);
    }

    [ContextMenu("Play Music 1")]
    private void PlayMusic1()
    {
        PlayMusic1Rpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayMusic1Rpc()
    {
        audioElement.PlayOneShot(music1Clip);
    }
}

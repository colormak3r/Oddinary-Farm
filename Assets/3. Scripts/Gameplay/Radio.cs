using UnityEngine;
using Unity.Netcode;

public class Radio : Structure, IInteractable
{
    private AudioElement audio;
    [SerializeField]
    private AudioClip interactClip;

    protected override void Awake()
    {
        audio = GetComponent<AudioElement>();
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
        audio.PlayOneShot(interactClip);
    }

}

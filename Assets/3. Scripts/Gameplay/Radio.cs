using UnityEngine;
using Unity.Netcode;

public class Radio : Structure, IInteractable
{
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
    }

}

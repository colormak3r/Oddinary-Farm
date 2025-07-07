using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    public bool IsHoldInteractable { get; }
    public void Interact(Transform source);
    public void InteractionStart(Transform source);
    public void InteractionEnd(Transform source);
}

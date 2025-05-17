using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

// Derrived from a Unity.Netcode class NetworkAnimator
[DisallowMultipleComponent]     // Component cannot be added twice as a component of the same game object
public class ClientNetworkAnimator : NetworkAnimator
{
    /// <summary>
    /// Used to determine who can write to this transform. Owner client only.
    /// This imposes state to the server. This is putting trust on your clients. 
    /// Make sure no security-sensitive features use this transform.
    /// </summary>
    protected override bool OnIsServerAuthoritative()       // Never allow authority over client animator
    {
        return false;
    }
}

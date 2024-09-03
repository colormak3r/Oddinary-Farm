using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientNetworkAnimator : NetworkAnimator
{
    /// <summary>
    /// Used to determine who can write to this transform. Owner client only.
    /// This imposes state to the server. This is putting trust on your clients. 
    /// Make sure no security-sensitive features use this transform.
    /// </summary>
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}

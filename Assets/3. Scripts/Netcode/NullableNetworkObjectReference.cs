using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public struct NullableNetworkObjectReference
{
    public bool IsNull { get; private set; }
    public NetworkObjectReference Ref;

    public NullableNetworkObjectReference(Transform transform)
    {
        if (transform == null)
        {
            IsNull = true;
            Ref = default;
        }
        else
        {
            IsNull = false;
            Ref = transform.gameObject;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SerializationInitializer : MonoBehaviour
{
    private void Awake()
    {
        /*UserNetworkVariableSerialization<NetworkedScriptableObject>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<NetworkedScriptableObject>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<NetworkedScriptableObject>.DuplicateValue = (in NetworkedScriptableObject val, ref NetworkedScriptableObject dup) =>
        {
            if(val != null)
            {
                dup = ScriptableObject.CreateInstance<NetworkedScriptableObject>();
                dup.CopyFrom(val);
            }
            else
            {
                dup = val;
            }      
        };

        UserNetworkVariableSerialization<Sprite>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<Sprite>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<Sprite>.DuplicateValue = SerializationExtensions.DuplicateValue;*/
    }
}

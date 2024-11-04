using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SerializationInitializer : MonoBehaviour
{
    private void Awake()
    {
        UserNetworkVariableSerialization<ItemProperty>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<ItemProperty>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<ItemProperty>.DuplicateValue = SerializationExtensions.DuplicateValue;

        UserNetworkVariableSerialization<PlantProperty>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<PlantProperty>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<PlantProperty>.DuplicateValue = SerializationExtensions.DuplicateValue;

        UserNetworkVariableSerialization<TerrainUnitProperty>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<TerrainUnitProperty>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<TerrainUnitProperty>.DuplicateValue = SerializationExtensions.DuplicateValue;

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

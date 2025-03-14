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

        UserNetworkVariableSerialization<SpawnerProperty>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<SpawnerProperty>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<SpawnerProperty>.DuplicateValue = SerializationExtensions.DuplicateValue;

        UserNetworkVariableSerialization<RangedWeaponProperty>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<RangedWeaponProperty>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<RangedWeaponProperty>.DuplicateValue = SerializationExtensions.DuplicateValue;

        UserNetworkVariableSerialization<PlantProperty>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<PlantProperty>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<PlantProperty>.DuplicateValue = SerializationExtensions.DuplicateValue;

        UserNetworkVariableSerialization<TerrainUnitProperty>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<TerrainUnitProperty>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<TerrainUnitProperty>.DuplicateValue = SerializationExtensions.DuplicateValue;

        UserNetworkVariableSerialization<StructureProperty>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<StructureProperty>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<StructureProperty>.DuplicateValue = SerializationExtensions.DuplicateValue;

        UserNetworkVariableSerialization<Face>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<Face>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<Face>.DuplicateValue = SerializationExtensions.DuplicateValue;

        UserNetworkVariableSerialization<Head>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<Head>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<Head>.DuplicateValue = SerializationExtensions.DuplicateValue;

        UserNetworkVariableSerialization<Hat>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<Hat>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<Hat>.DuplicateValue = SerializationExtensions.DuplicateValue;

        UserNetworkVariableSerialization<Outfit>.WriteValue = SerializationExtensions.WriteValueSafe;
        UserNetworkVariableSerialization<Outfit>.ReadValue = SerializationExtensions.ReadValueSafe;
        UserNetworkVariableSerialization<Outfit>.DuplicateValue = SerializationExtensions.DuplicateValue;
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.UI.Image;


public static class SerializationExtensions
{
    private static string NULL_STRING = "null";

    /*#region Item
    public static void WriteValueSafe(this FastBufferWriter writer, in Item obj)
    {
        writer.WriteValueSafe(new NetworkBehaviourReference(obj));
    }

    public static void ReadValueSafe(this FastBufferReader reader, out Item obj)
    {

        reader.ReadValueSafe(out NetworkBehaviourReference objref);
        if (objref.TryGet(out var behaviour))
        {
            obj = (Item)behaviour;
        }
        else
        {
            obj = null;
        }
    }

    public static void DuplicateValue(in Item value, ref Item duplicatedValue)
    {
        duplicatedValue = value;
    }
    #endregion*/

    #region Item Property
    public static void WriteValueSafe(this FastBufferWriter writer, in ItemProperty obj)
    {
        writer.WriteValueSafeInternal(obj);
    }

    public static void ReadValueSafe(this FastBufferReader reader, out ItemProperty obj)
    {
        reader.ReadValueSafeInternal(out obj);
    }

    public static void DuplicateValue(in ItemProperty value, ref ItemProperty duplicatedValue)
    {
        duplicatedValue = value;
    }
    #endregion

    #region SpawnerProperty
    public static void WriteValueSafe(this FastBufferWriter writer, in SpawnerProperty obj)
    {
        writer.WriteValueSafeInternal(obj);
    }
    public static void ReadValueSafe(this FastBufferReader reader, out SpawnerProperty obj)
    {
        reader.ReadValueSafeInternal(out obj);
    }
    public static void DuplicateValue(in SpawnerProperty value, ref SpawnerProperty duplicatedValue)
    {
        DuplicateValueInternal(value, ref duplicatedValue);
    }
    #endregion

    #region RangedWeaponProperty
    public static void WriteValueSafe(this FastBufferWriter writer, in RangedWeaponProperty obj)
    {
        writer.WriteValueSafeInternal(obj);
    }
    public static void ReadValueSafe(this FastBufferReader reader, out RangedWeaponProperty obj)
    {
        reader.ReadValueSafeInternal(out obj);
    }
    public static void DuplicateValue(in RangedWeaponProperty value, ref RangedWeaponProperty duplicatedValue)
    {
        duplicatedValue = value;
    }
    #endregion

    #region PlantProperty
    public static void WriteValueSafe(this FastBufferWriter writer, in PlantProperty obj)
    {
        writer.WriteValueSafeInternal(obj);
    }

    public static void ReadValueSafe(this FastBufferReader reader, out PlantProperty obj)
    {
        reader.ReadValueSafeInternal(out obj);
    }

    public static void DuplicateValue(in PlantProperty value, ref PlantProperty duplicatedValue)
    {
        DuplicateValueInternal(value, ref duplicatedValue);
    }
    #endregion

    #region TerrainUnitProperty
    public static void WriteValueSafe(this FastBufferWriter writer, in TerrainUnitProperty obj)
    {
        writer.WriteValueSafeInternal(obj);
    }

    public static void ReadValueSafe(this FastBufferReader reader, out TerrainUnitProperty obj)
    {
        reader.ReadValueSafeInternal(out obj);
    }

    public static void DuplicateValue(in TerrainUnitProperty value, ref TerrainUnitProperty duplicatedValue)
    {
        DuplicateValueInternal(value, ref duplicatedValue);
    }
    #endregion

    #region StructureProperty
    public static void WriteValueSafe(this FastBufferWriter writer, in StructureProperty obj)
    {
        writer.WriteValueSafeInternal(obj);
    }

    public static void ReadValueSafe(this FastBufferReader reader, out StructureProperty obj)
    {
        reader.ReadValueSafeInternal(out obj);
    }

    public static void DuplicateValue(in StructureProperty value, ref StructureProperty duplicatedValue)
    {
        DuplicateValueInternal(value, ref duplicatedValue);
    }
    #endregion

    #region ConsummableProperty
    public static void WriteValueSafe(this FastBufferWriter writer, in ConsummableProperty obj)
    {
        writer.WriteValueSafeInternal(obj);
    }
    public static void ReadValueSafe(this FastBufferReader reader, out ConsummableProperty obj)
    {
        reader.ReadValueSafeInternal(out obj);
    }
    public static void DuplicateValue(in ConsummableProperty value, ref ConsummableProperty duplicatedValue)
    {
        DuplicateValueInternal(value, ref duplicatedValue);
    }
    #endregion

    #region Face
    public static void WriteValueSafe(this FastBufferWriter writer, in Face obj)
    {
        writer.WriteValueSafeInternal(obj);
    }

    public static void ReadValueSafe(this FastBufferReader reader, out Face obj)
    {
        reader.ReadValueSafeInternal(out obj);
    }

    public static void DuplicateValue(in Face value, ref Face duplicatedValue)
    {
        DuplicateValueInternal(value, ref duplicatedValue);
    }
    #endregion

    #region Head
    public static void WriteValueSafe(this FastBufferWriter writer, in Head obj)
    {
        writer.WriteValueSafeInternal(obj);
    }

    public static void ReadValueSafe(this FastBufferReader reader, out Head obj)
    {
        reader.ReadValueSafeInternal(out obj);
    }

    public static void DuplicateValue(in Head value, ref Head duplicatedValue)
    {
        DuplicateValueInternal(value, ref duplicatedValue);
    }
    #endregion

    #region Hat
    public static void WriteValueSafe(this FastBufferWriter writer, in Hat obj)
    {
        writer.WriteValueSafeInternal(obj);
    }

    public static void ReadValueSafe(this FastBufferReader reader, out Hat obj)
    {
        reader.ReadValueSafeInternal(out obj);
    }

    public static void DuplicateValue(in Hat value, ref Hat duplicatedValue)
    {
        DuplicateValueInternal(value, ref duplicatedValue);
    }
    #endregion

    #region Outfit
    public static void WriteValueSafe(this FastBufferWriter writer, in Outfit obj)
    {
        writer.WriteValueSafeInternal(obj);
    }

    public static void ReadValueSafe(this FastBufferReader reader, out Outfit obj)
    {
        reader.ReadValueSafeInternal(out obj);
    }

    public static void DuplicateValue(in Outfit value, ref Outfit duplicatedValue)
    {
        DuplicateValueInternal(value, ref duplicatedValue);
    }
    #endregion

    #region Internal
    private static void WriteValueSafeInternal<T>(this FastBufferWriter writer, in T obj) where T : ScriptableObject
    {
        writer.WriteValueSafe(obj == null ? NULL_STRING : obj.name);
    }

    private static void ReadValueSafeInternal<T>(this FastBufferReader reader, out T obj) where T : ScriptableObject
    {
        reader.ReadValueSafe(out string objName);
        if (objName == NULL_STRING)
            obj = null;
        else
            obj = AssetManager.Main.GetScriptableObjectByName<T>(objName);
    }

    private static void DuplicateValueInternal<T>(in T value, ref T duplicatedValue) where T : ScriptableObject
    {
        duplicatedValue = value;
    }
    #endregion
}

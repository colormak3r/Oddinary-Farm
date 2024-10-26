using System;
using UnityEngine;

[System.Serializable]
public enum ItemType
{
    Item,
    Currency
}

[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Item/Item(Test Only)")]
public class ItemProperty : ScriptableObject, IEquatable<ItemProperty>
{
    [Header("Item Settings")]
    [SerializeField]
    private ItemType type;
    [SerializeField]
    private Sprite sprite;
    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private bool isConsummable;
    [SerializeField]
    private uint maxStack = 20;
    [SerializeField]
    private float primaryCdr = 0.25f;
    [SerializeField]
    private float secondaryCdr = 0.25f;
    [SerializeField]
    private float range = 3f;

    public string Name => name.Replace(" Property", "");
    public ItemType Type => type;
    public Sprite Sprite => sprite;
    public GameObject Prefab => prefab;
    public uint MaxStack => maxStack;
    public bool IsConsummable => isConsummable;
    public float PrimaryCdr => primaryCdr;
    public float SecondaryCdr => secondaryCdr;
    public float Range => range;

    public bool IsStackable => maxStack > 1;

    public bool Equals(ItemProperty other)
    {
        return other == this;
    }
}

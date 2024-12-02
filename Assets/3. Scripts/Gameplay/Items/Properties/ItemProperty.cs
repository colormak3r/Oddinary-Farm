using System;
using UnityEngine;

[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Item/Item(Test Only)")]
public class ItemProperty : ScriptableObject, IEquatable<ItemProperty>
{
    [Header("Item Settings")]
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
    private float range = 5f;
    [SerializeField]
    private uint price = 1;

    [Header("Item Audio")]
    [SerializeField]
    private AudioClip pickupSound;
    [SerializeField]
    private AudioClip primarySound;
    [SerializeField]
    private AudioClip secondarySound;
    [SerializeField]
    private AudioClip alternativeSound;

    public string Name => name.Replace(" Property", "");
    public Sprite Sprite => sprite;
    public GameObject Prefab => prefab;
    public uint MaxStack => maxStack;
    public bool IsConsummable => isConsummable;
    public float PrimaryCdr => primaryCdr;
    public float SecondaryCdr => secondaryCdr;
    public float Range => range;
    public uint Price => price;

    public AudioClip PickupSound => pickupSound;
    public AudioClip PrimarySound => primarySound;
    public AudioClip SecondarySound => secondarySound;
    public AudioClip AlternativeSound => alternativeSound;

    public bool IsStackable => maxStack > 1;

    public bool Equals(ItemProperty other)
    {
        return other == this;
    }
}

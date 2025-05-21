using System;
using UnityEngine;

/// <summary>
/// Scriptable Object that holds a bunch of common data about an item
/// </summary>
[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Item/Item(Test Only)")]
public class ItemProperty : ScriptableObject, IEquatable<ItemProperty>
{
    [Header("Item Settings")]
    [SerializeField]
    private Sprite iconSprite;      // Sprite for UI
    [SerializeField]
    private Sprite objectSprite;        // Sprite for world
    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private bool isConsummable;
    [SerializeField]
    private uint maxStack = 20;
    [Space]                         // Adds a vertical space in the inspector
    [SerializeField]
    private bool isSellable = true;
    [SerializeField]
    private uint price = 1;
    [Space]
    [SerializeField]
    private float range = 5f;
    [SerializeField]
    private float primaryCdr = 0.25f;
    [SerializeField]
    private float secondaryCdr = 0.25f;

    [Header("Item Context")]
    [TextArea(3, 10)]       // Limit's string text to a range
    [SerializeField]
    private string itemContext = "LMB: Place\r\nRMB: Remove\r\nX  : Drop";

    [Header("Item Audio")]
    [SerializeField]
    private AudioClip pickupSound;
    [SerializeField]
    private AudioClip selectSound;
    [SerializeField]
    private AudioClip primarySound;
    [SerializeField]
    private AudioClip secondarySound;
    [SerializeField]
    private AudioClip alternativeSound;

    // Public Getters
    public string Name => name.Replace(" Property", "");
    public Sprite IconSprite => iconSprite;
    public Sprite ObjectSprite => objectSprite == null ? iconSprite : objectSprite;
    public GameObject Prefab => prefab;
    public bool IsConsummable => isConsummable;
    public uint MaxStack => maxStack;
    public bool IsSellable => isSellable;
    public uint Price => price;
    public float Range => range;
    public float PrimaryCdr => primaryCdr;
    public float SecondaryCdr => secondaryCdr;

    public string ItemContext => Name + "\n" + itemContext;

    public AudioClip PickupSound => pickupSound;
    public AudioClip SelectSound => selectSound;
    public AudioClip PrimarySound => primarySound;
    public AudioClip SecondarySound => secondarySound;
    public AudioClip AlternativeSound => alternativeSound;


    public bool IsStackable => maxStack > 1;

    public bool Equals(ItemProperty other)      // Returns true if the object is the same one in question; For instance ID
    {
        return other == this;
    }

    // QUESTION: What does this do exactly? What's it's purpose?
    // Safety method, All non-consumable items can only stack once
    private void OnValidate()       // When the item is changed in the inspector
    {
        if (!IsConsummable)
        {
            maxStack = 1;
        }
    }
}

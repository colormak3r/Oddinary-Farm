using System;
using UnityEngine;

[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Item/Item(Test Only)")]
public class ItemProperty : ScriptableObject, IEquatable<ItemProperty>
{
    [Header("Item Settings")]
    [SerializeField]
    private Sprite iconSprite;
    [SerializeField]
    private Sprite objectSprite;
    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private bool isConsummable;
    [SerializeField]
    private uint maxStack = 20;
    [Space]
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
    [TextArea(3, 10)]
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

    public bool Equals(ItemProperty other)
    {
        return other == this;
    }

    private void OnValidate()
    {
        if (!IsConsummable)
        {
            maxStack = 1;
        }
    }
}

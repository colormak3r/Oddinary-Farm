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
    private int maxStack = 99;
    [SerializeField]
    private float primaryCdr = 0.25f;
    [SerializeField]
    private float secondaryCdr = 0.25f;

    public Sprite Sprite => sprite;
    public GameObject Prefab => prefab;
    public int MaxStack => maxStack;
    public bool IsConsummable => isConsummable;
    public float PrimaryCdr => primaryCdr;
    public float SecondaryCdr => secondaryCdr;

    public bool Equals(ItemProperty other)
    {
        return other == this;
    }
}

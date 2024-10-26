using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shop Inventory", menuName = "Scriptable Objects/Shop Inventory")]
public class ShopInventory : ScriptableObject
{
    [SerializeField]
    private string shopName;
    [SerializeField]
    private ItemProperty[] itemProperties;

    public string ShopName => shopName;
    public ItemProperty[] ItemProperties => itemProperties;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shop Inventory", menuName = "Scriptable Objects/Shop Inventory")]
public class ShopInventory : ScriptableObject
{
    [SerializeField]
    private string shopName;
    [SerializeField]
    private ShopEntry[] shopEntries;

    public string ShopName => shopName;
    public ShopEntry[] ShopEntries => shopEntries;
}
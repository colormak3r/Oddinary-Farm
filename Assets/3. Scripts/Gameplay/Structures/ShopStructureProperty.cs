using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shop Structure Property", menuName = "Scriptable Objects/Structure Property/Shop")]
public class ShopStructureProperty : StructureProperty
{
    [Header("Shop Structure Properties")]
    [SerializeField]
    private ShopInventory shopInventory;
    public ShopInventory ShopInventory => shopInventory;
}

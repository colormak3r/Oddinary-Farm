using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public struct ItemPropertyCount
{
    public ItemProperty property;
    public int count;
}

public class PlayerInventory : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float pickupRange = 0.5f;
    [SerializeField]
    private int inventorySlot = 20;

    [Header("Debugs")]
    [SerializeField]
    private List<ItemPropertyCount> inventory = new List<ItemPropertyCount> ();

   
    public void Add(ItemProperty property, int count = 1)
    {

    }



    public void Remove(ItemProperty property, int count = 1)
    {

    }
}

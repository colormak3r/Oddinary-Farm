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
    [SerializeField]
    private List<ItemPropertyCount> inventory = new List<ItemPropertyCount> ();

   
    public void Add(ItemProperty property, int count = 1)
    {

    }



    public void Remove(ItemProperty property, int count = 1)
    {

    }
}

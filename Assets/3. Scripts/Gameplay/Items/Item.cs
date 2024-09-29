using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Item : NetworkBehaviour
{
    protected NetworkVariable<ItemProperty> Property = new NetworkVariable<ItemProperty>();

    public ItemProperty PropertyValue
    {
        get { return Property.Value; }
        set { Property.Value = value; }
    }   

    public virtual bool OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        return false;
    }

    public virtual bool CanPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        return false;
    }

    public virtual bool OnSecondaryAction(Vector2 position, PlayerInventory inventory)
    {
        return false;
    }

    public virtual bool OnAlternativeAction(Vector2 position, PlayerInventory inventory)
    {
        return false;
    }
}

using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Hammer : Item
{
    private HammerProperty property;
    private int currentCharge;

    public override void OnNetworkSpawn()
    {
        HandleOnPropertyChanged(null, PropertyValue);
        Property.OnValueChanged += HandleOnPropertyChanged;
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandleOnPropertyChanged;
    }

    private void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        property = (HammerProperty)newValue;
    }

    public override bool OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        base.OnPrimaryAction(position, inventory);

        position = position.SnapToGrid();
        HammerPrimaryRpc(position);
        return true;
    }

    public override bool OnSecondaryAction(Vector2 position, PlayerInventory inventory)
    {
        base.OnSecondaryAction(position, inventory);

        position = position.SnapToGrid();
        HammerSecondaryRpc(position);
        return true;
    }

    [Rpc(SendTo.Server)]
    private void HammerPrimaryRpc(Vector2 position)
    {
        var hits = Physics2D.OverlapCircleAll(position, property.Radius, property.InvalidLayers);
        if (hits.Length > 0) return;

        var hit = Physics2D.OverlapPoint(position, property.TerrainLayers);
        if (hit && hit.TryGetComponent(out TerrainUnit t))
        {
            if (t.Property.IsAccessible)
            {
                GameObject go = Instantiate(property.Structures[0], position, Quaternion.identity);
                go.GetComponent<NetworkObject>().Spawn();
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void HammerSecondaryRpc(Vector2 position)
    {
        var hit = Physics2D.OverlapCircle(position, property.Radius, property.StructureLayers);
        if (hit && hit.TryGetComponent(out Structure structure))
        {
            structure.Remove();
        }
    }
}

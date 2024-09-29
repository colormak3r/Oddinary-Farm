using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Hoe : Item
{
    private HoeProperty property;

    public override void OnNetworkSpawn()
    {
        Property.OnValueChanged += HandleOnPropertyChanged;
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandleOnPropertyChanged;
    }

    private void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        property = (HoeProperty)newValue;
    }

    public override bool OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        base.OnPrimaryAction(position, inventory);
        position = position.SnapToGrid();
        var hits = Physics2D.OverlapPointAll(position, property.HoeableLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<TerrainUnit>(out var terrain))
                {
                    if (terrain.Property.IsAccessible)
                    {
                        HoeRpc(terrain.transform.position);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public override bool OnSecondaryAction(Vector2 position, PlayerInventory inventory)
    {
        return base.OnSecondaryAction(position, inventory);

        // Remove Garden Plot
    }


    [Rpc(SendTo.Server)]
    private void HoeRpc(Vector2 position)
    {
        GameObject go = Instantiate(AssetManager.Main.FarmPlotPrefab, position, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
    }
}

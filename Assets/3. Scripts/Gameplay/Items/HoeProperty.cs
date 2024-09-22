using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "Hoe Property", menuName = "Scriptable Objects/Item/Hoe")]
public class HoeProperty : ToolProperty
{
    [Header("Hoe Settings")]
    [SerializeField]
    private LayerMask hoeableLayer;

    public override bool OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        base.OnPrimaryAction(position, inventory);
        position = position.SnapToGrid();
        var hits = Physics2D.OverlapPointAll(position, hoeableLayer);
        if(hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if(hit.TryGetComponent<TerrainUnit>(out var terrain))
                {
                    if (terrain.Property.IsAccessible)
                    {
                        Hoe(terrain.transform.position);
                        return true;
                    }                        
                }
            }
        }

        return false;
    }

    public override bool OnSecondaryAction(Vector2 position)
    {
        return base.OnSecondaryAction(position);

        // Remove Garden Plot
    }


    [Rpc(SendTo.Server)]
    private void Hoe(Vector2 position)
    {
        GameObject go = Instantiate(AssetManager.Main.FarmPlotPrefab, position, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
    }
}

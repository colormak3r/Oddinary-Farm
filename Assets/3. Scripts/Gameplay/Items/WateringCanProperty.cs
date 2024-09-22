using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "Watering Can Property", menuName = "Scriptable Objects/Item/Watering Can")]
public class WateringCanProperty : ToolProperty
{
    [Header("Watering Can Settings")]
    [SerializeField]
    private int maxCharge = 3;
    private int currentCharge;

    [SerializeField]
    private LayerMask waterableLayer;

    public int MaxCharge => maxCharge;

    public override bool OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        //if (currentCharge <= 0) return;
        base.OnPrimaryAction(position, inventory);

        Water(position);
        return true;
    }

    [Rpc(SendTo.Server)]
    private void Water(Vector2 position)
    {
        var hits = Physics2D.OverlapCircleAll(position, Radius, waterableLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if(hit.TryGetComponent<IWaterable>(out var waterable))
                {
                    waterable.GetWatered();
                }
            }
        }

        /*GameObject go = Instantiate(plantPrefab, position.SnapToGrid(), Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        var plant = go.GetComponent<Plant>();
        plant.MockPropertyChange();*/
    }
}

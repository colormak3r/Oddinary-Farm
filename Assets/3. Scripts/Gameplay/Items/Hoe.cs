using ColorMak3r.Utility;
using Unity.Netcode;
using UnityEngine;


public class Hoe : Tool
{
    private HoeProperty hoeProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        hoeProperty = (HoeProperty)baseProperty;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();

        // Check if the action is in range
        if (!ItemSystem.IsInRange(position, hoeProperty.Range))
        {
            if (showDebug) Debug.Log($"Cannot hoe primary at {position}, out of range");
            return false;
        }

        // Check if there is any invalid object in the area
        var invalid = ItemSystem.OverlapArea(position, hoeProperty.Size, hoeProperty.InvalidLayers);
        if (invalid)
        {
            if (showDebug) Debug.Log($"Cannot hoe at {position}, {invalid.name} is blocking", invalid);
            return false;
        }

        // Check if the terrain is accessible
        var terrainHits = ItemSystem.OverlapAreaAll(position, hoeProperty.Size, hoeProperty.TerrainLayer);
        foreach (var terrainHit in terrainHits)
        {
            if (terrainHit && terrainHit.TryGetComponent<TerrainUnit>(out var terrain))
            {
                if (!terrain.Property.IsAccessible)
                {
                    if (showDebug) Debug.Log($"Cannot hoe at {position}, {terrain.name} is not accessible");
                    return false;
                }
            }
        }

        return true;
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        base.OnPrimaryAction(position);

        ItemSystem.SpawnFarmPlot(position);
        ItemSystem.ClearFoliage(position);
    }

    public override bool CanSecondaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        if (!ItemSystem.IsInRange(position, hoeProperty.Range))
        {
            if (showDebug) Debug.Log($"Cannot hoe secondary at {position}, out of range");
            return false;
        }
        else
        {
            var farmPlot = Physics2D.OverlapPoint(position, hoeProperty.FarmPlotLayer);
            if (!farmPlot)
            {
                if (showDebug) Debug.Log($"Cannot hoe secondary at {position}, no farm plot found");
                return false;
            }

            if (showDebug) Debug.Log($"Hoe secondary success at {position}");
            return true;
        }
    }

    public override void OnSecondaryAction(Vector2 position)
    {
        base.OnSecondaryAction(position);
        ItemSystem.RemoveFarmPlot(position);
        ItemSystem.RemovePlant(position, transform.root.gameObject);
    }

    /*[Rpc(SendTo.Server)]
    private void HoeSecondaryRpc(Vector2 position)
    {
        var plantCollider = Physics2D.OverlapPoint(position, hoeProperty.PlantLayer);
        var farmPlotCollider = Physics2D.OverlapPoint(position, hoeProperty.FarmPlotLayer);
        if (plantCollider)
        {
            var plant = plantCollider.GetComponent<Plant>();
            if (plant.IsHarvestable)
            {
                if (showDebug) Debug.Log($"Cannot remove farmPlot: Plant at {position} is harvestable");
                return;
            }
            else
            {
                if (showDebug) Debug.Log($"Plant and Farm Plot at {position} is removed");
                farmPlotCollider.GetComponent<NetworkObject>().Despawn();
                AssetManager.Main.SpawnItem(plant.Seed, position, transform.root.gameObject);
            }
        }
        else
        {
            var farmPlot = Physics2D.OverlapPoint(position, hoeProperty.FarmPlotLayer);
            if (!farmPlot)
            {
                if (showDebug) Debug.Log($"Cannot hoe secondary at {position}, no farm plot found");

            }
            else
            {
                if (showDebug) Debug.Log($"Farm Plot at {position} is removed");
                Destroy(farmPlotCollider.gameObject);
            }
        }
    }*/

    public override void OnAlternativeAction(Vector2 position)
    {
        /*currentHoeAltMode++;
        if (currentHoeAltMode >= hoeSize.Count) currentHoeAltMode = 0;
        if (showDebug) Debug.Log($"Hoe size changed to {hoeSize[currentHoeAltMode]}");*/
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        /*var size = hoeSize[currentHoeAltMode];
        var position = PlayerController.LookPosition.SnapToGrid((int)(size.x < 1 ? 1 : size.x));

        Gizmos.DrawCube(position, hoeSize[currentHoeAltMode] * 0.9f);*/
    }
}

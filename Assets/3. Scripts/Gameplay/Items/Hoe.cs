using ColorMak3r.Utility;
using Unity.Netcode;
using UnityEngine;


public class Hoe : Tool
{
    private HoeProperty hoeProperty;

    /*private List<Vector2> hoeSize = new List<Vector2>
    {
        new Vector2(1, 1),
        new Vector2(2, 2),
        new Vector2(3, 3)
    };

    private int currentHoeAltMode = 0;*/

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        hoeProperty = (HoeProperty)newValue;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();

        // Check if the action is in range
        if (!IsInRange(position))
        {
            if (showDebug) Debug.Log($"Cannot hoe primary at {position}, out of range");
            return false;
        }

        // Check if there is any invalid object in the area
        var invalid = OverlapArea(hoeProperty.Size, position, hoeProperty.InvalidLayers);
        if (invalid)
        {
            if (showDebug) Debug.Log($"Cannot hoe at {position}, {invalid.name} is blocking", invalid);
            return false;
        }

        // Check if the terrain is accessible
        var terrainHits = OverlapAreaAll(hoeProperty.Size, position, hoeProperty.TerrainLayer);
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
        HoePrimaryRpc(position);

        // Run on both client and server
        ClearFolliage(position);
    }

    [Rpc(SendTo.Server)]
    private void HoePrimaryRpc(Vector2 position)
    {
        GameObject go = Instantiate(AssetManager.Main.FarmPlotPrefab, position - TransformUtility.HALF_UNIT_Y_V2, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        go.GetComponent<FarmPlot>().ChangeSizeOnServer(Vector2.one);
    }

    public override bool CanSecondaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        if (!IsInRange(position))
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
        position = position.SnapToGrid();
        base.OnSecondaryAction(position);
        HoeSecondaryRpc(position);
    }

    [Rpc(SendTo.Server)]
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
                Destroy(farmPlotCollider.gameObject);
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
    }

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

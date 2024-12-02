using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class Hoe : Tool
{
    private HoeProperty hoeProperty;

    private List<Vector2> hoeSize = new List<Vector2>
    {
        new Vector2(1, 1),
        new Vector2(2, 2),
        new Vector2(3, 3)
    };

    private int currentHoeAltMode = 0;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        hoeProperty = (HoeProperty)newValue;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        if (!IsInRange(position)) return false;

        var size = hoeSize[currentHoeAltMode];
        var halfSize = size / 2;

        var snap = (int)(size.x < 1 ? 1 : size.x);
        position = position.SnapToGrid((snap));
        var pointA = position + halfSize * 0.9f;
        var pointB = position - halfSize * 0.9f;

        var invalid = Physics2D.OverlapArea(pointA, pointB, hoeProperty.UnhoeableLayer);
        if (invalid)
        {
            if (showDebug) Debug.Log($"Cannot hoe at {position}, {invalid.name} is blocking", invalid);
            return false;
        }

        var terrainHits = Physics2D.OverlapAreaAll(pointA, pointB, hoeProperty.HoeableLayer);
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

        if (showDebug) Debug.Log($"Hoe success at {position}");
        return true;
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        HoePrimaryRpc(position, hoeSize[currentHoeAltMode]);
    }


    [Rpc(SendTo.Server)]
    private void HoePrimaryRpc(Vector2 position, Vector2 size)
    {
        var s = (int)(size.x < 1 ? 1 : size.x);
        position = position.SnapToGrid(s) - (s == 1 ? TransformUtility.HALF_UNIT_Y_V2 : Vector2.zero);
        GameObject go = Instantiate(AssetManager.Main.FarmPlotPrefab, position, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        go.GetComponent<FarmPlot>().ChangeSizeOnServer(size);
    }

    public override void OnAlternativeAction(Vector2 position)
    {
        currentHoeAltMode++;
        if (currentHoeAltMode >= hoeSize.Count) currentHoeAltMode = 0;
        if (showDebug) Debug.Log($"Hoe size changed to {hoeSize[currentHoeAltMode]}");
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        var size = hoeSize[currentHoeAltMode];
        var position = PlayerController.LookPosition.SnapToGrid((int)(size.x < 1 ? 1 : size.x));

        Gizmos.DrawCube(position, hoeSize[currentHoeAltMode] * 0.9f);
    }
}

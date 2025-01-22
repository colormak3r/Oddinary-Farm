using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Tool : Item
{
    private ToolProperty toolProperty;
    public ToolProperty ToolProperty => toolProperty;

    public override void OnPreview(Vector2 position, Previewer previewer)
    {
        position = position.SnapToGrid(toolProperty.Size);
        previewer.MoveTo(position);
        previewer.Show(true);
        previewer.SetIconOffset(toolProperty.PreviewIconOffset);
        previewer.SetIcon(toolProperty.PreviewIconSprite);
        previewer.SetSize(toolProperty.Size);
        if (CanPrimaryAction(position))
        {
            previewer.SetColor(toolProperty.PreviewValidColor);
        }
        else
        {
            previewer.SetColor(toolProperty.PreviewInvalidColor);
        }
    }

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        toolProperty = (ToolProperty)newValue;
    }

    protected void ClearFolliage(Vector2 position)
    {
        ClearFolliageRpc(position);
    }

    [Rpc(SendTo.Everyone)]
    private void ClearFolliageRpc(Vector2 position)
    {
        position = position.SnapToGrid();

        var collider = Physics2D.OverlapPoint(position, toolProperty.TerrainLayer);
        if (collider && collider.TryGetComponent<TerrainUnit>(out var terrainUnit))
        {
            terrainUnit.BreakFolliage();
            /*if (IsServer)
            {
                WorldGenerator.Main.InvalidateFolliagePositionOnServer(position);
            }*/
        }
    }

    protected Collider2D OverlapArea(Vector2 size, Vector2 position, LayerMask layers, float precision = 0.9f)
    {
        // Calculate the scaled half-size based on the given precision
        Vector2 scaledHalfSize = size * 0.5f * precision;

        // Define the corners of the overlap area
        Vector2 pointA = position + scaledHalfSize;
        Vector2 pointB = position - scaledHalfSize;

        // Perform the area overlap check and return the result
        return Physics2D.OverlapArea(pointA, pointB, layers);
    }

    protected Collider2D[] OverlapAreaAll(Vector2 size, Vector2 position, LayerMask layers, float precision = 0.9f)
    {
        // Calculate the scaled half-size based on the given precision
        Vector2 scaledHalfSize = size * 0.5f * precision;

        // Define the corners of the overlap area
        Vector2 pointA = position + scaledHalfSize;
        Vector2 pointB = position - scaledHalfSize;

        // Perform the area overlap check and return the result
        return Physics2D.OverlapAreaAll(pointA, pointB, layers);
    }

    protected bool IsTerrainAccessible(Vector2 position)
    {
        var terrain = Physics2D.OverlapPoint(position, toolProperty.TerrainLayer);
        if (terrain && terrain.TryGetComponent<TerrainUnit>(out var terrainUnit))
        {
            return terrainUnit.Property.IsAccessible;
        }
        else
        {
            return false;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, toolProperty.Range);
    }
}

using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class Shovel : Tool
{
    private ShovelProperty shovelProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        shovelProperty = baseProperty as ShovelProperty;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        if (!ItemSystem.IsInRange(position, shovelProperty.Range)) return false;

        var hit = Physics2D.OverlapPoint(position, LayerManager.Main.DiggableLayer);
        if (hit && hit.TryGetComponent(out IDiggable diggable))
        {
            return true;
        }
        else
        {
            if (showDebug) Debug.Log($"Not Diggable at {position}, hit = {hit?.gameObject.name}", hit?.gameObject);
            return false;
        }
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        base.OnPrimaryAction(position);
        ItemSystem.Dig(position);
    }
}

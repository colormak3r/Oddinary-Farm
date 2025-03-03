using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureItem : Spawner
{
    private StructureItemProperty blueprintProperty;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        blueprintProperty = (StructureItemProperty)newValue;
    }

    public override bool CanSecondaryAction(Vector2 position)
    {
        // Check if a valid structure exist at the position
        var structureHit = Physics2D.OverlapPoint(position, blueprintProperty.StructureLayer);
        if (structureHit && structureHit.TryGetComponent(out Structure structure))
        {
            // Check if the position of the structure is in range
            position = (Vector2)structureHit.transform.position + structure.Property.Offset;
            if (!IsInRange(position)) return false;

            return true;
        }
        else
        {
            return false;
        }
    }

    public override void OnSecondaryAction(Vector2 position)
    {
        base.OnSecondaryAction(position);
        RemoveStructureOnServer(position);
    }

    private void RemoveStructureOnServer(Vector2 position)
    {
        var structureHit = Physics2D.OverlapPoint(position, blueprintProperty.StructureLayer);
        if (structureHit && structureHit.TryGetComponent(out Structure structure))
        {
            structure.RemoveStructure();
            Previewer.Main.Show(false);
        }
    }
}
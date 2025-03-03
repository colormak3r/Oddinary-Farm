using ColorMak3r.Utility;
using System;
using UnityEngine;

public class Hammer : Tool
{
    private HammerProperty hammerProperty;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        hammerProperty = (HammerProperty)newValue;
    }

    public override void OnPreview(Vector2 position, Previewer previewer)
    {
        previewer.Show(true);
        previewer.SetIconOffset(Vector2.zero);
        previewer.SetIcon(hammerProperty.FixIconSprite);

        var structureHit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayer);
        if (structureHit && structureHit.TryGetComponent(out Structure structure))
        {
            var size = structure.Property.Size;
            previewer.MoveTo((Vector2)structureHit.transform.position + structure.Property.Offset);
            previewer.SetSize(size);
        }
        else
        {
            previewer.MoveTo(position.SnapToGrid());
            previewer.SetSize(hammerProperty.Size);
        }

        if (CanPrimaryAction(position))
        {
            previewer.SetColor(hammerProperty.PreviewValidColor);
        }
        else
        {
            previewer.SetColor(hammerProperty.PreviewInvalidColor);
        }
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        // Check if a valid structure exist at the position
        var structureHit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayer);
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

    public override void OnPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        base.OnPrimaryAction(position);
        HammerPrimary(position);
    }

    private void HammerPrimary(Vector2 position)
    {
        base.OnPrimaryAction(position);
        FixStructureOnServer(position);
    }

    private void FixStructureOnServer(Vector2 position)
    {
        var structureHit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayer);
        if (structureHit && structureHit.TryGetComponent(out StructureStatus structureStatus))
        {
            structureStatus.GetHealed(1);
        }
    }

    public override bool CanSecondaryAction(Vector2 position)
    {
        // Check if a valid structure exist at the position
        var structureHit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayer);
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
        var structureHit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayer);
        if (structureHit && structureHit.TryGetComponent(out Structure structure))
        {
            structure.RemoveStructure();
            Previewer.Main.Show(false);
        }
    }
}
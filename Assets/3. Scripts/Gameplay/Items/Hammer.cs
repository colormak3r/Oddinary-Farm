using ColorMak3r.Utility;
using System;
using UnityEngine;

public class Hammer : Tool
{
    private HammerProperty hammerProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        hammerProperty = baseProperty as HammerProperty;
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
            if (!ItemSystem.IsInRange(position, hammerProperty.Range)) return false;

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
        ItemSystem.FixStructure(position, hammerProperty.StructureLayer);
    }


    public override bool CanSecondaryAction(Vector2 position)
    {
        // Check if a valid structure exist at the position
        var structureHit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayer);
        if (structureHit && structureHit.TryGetComponent(out Structure structure))
        {
            // Check if the position of the structure is in range
            position = (Vector2)structureHit.transform.position + structure.Property.Offset;
            if (!ItemSystem.IsInRange(position, hammerProperty.Range)) return false;

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
        ItemSystem.RemoveStructure(position, hammerProperty.StructureLayer);
    }


}
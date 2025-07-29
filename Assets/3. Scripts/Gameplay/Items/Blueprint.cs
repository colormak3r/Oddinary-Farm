/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/18/2025 (Khoa)
 * Notes:           <write here>
*/


using UnityEngine;

public class Blueprint : Spawner
{
    private BlueprintProperty blueprintProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        blueprintProperty = (BlueprintProperty)baseProperty;
    }

    public override bool CanSecondaryAction(Vector2 position)
    {
        // Check if a valid structure exist at the position
        var structureHit = Physics2D.OverlapPoint(position, LayerManager.StructureLayer);
        if (structureHit && structureHit.TryGetComponent(out Structure structure))
        {
            // Check if the position of the structure is in range
            position = (Vector2)structureHit.transform.position + structure.Property.Offset;
            if (!ItemSystem.IsInRange(position, blueprintProperty.Range)) return false;

            return true;
        }
        else
        {
            return false;
        }
    }
}
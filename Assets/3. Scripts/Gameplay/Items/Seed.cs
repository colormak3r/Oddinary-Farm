using ColorMak3r.Utility;
using UnityEngine;

public class Seed : Spawner
{
    private SeedProperty seedProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        seedProperty = (SeedProperty)baseProperty;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        if (!ItemSystem.IsInRange(position, seedProperty.Range)) return false;

        position = position.SnapToGrid();
        var farmPlotHits = Physics2D.OverlapPointAll(position, seedProperty.FarmPlotLayer);
        if (farmPlotHits.Length > 0)
        {
            var plantHits = Physics2D.OverlapPointAll(position, seedProperty.PlantLayer);
            if (plantHits.Length > 0)
            {
                if (showDebug) Debug.Log($"A plant already exist at {position}");
                return false;
            }
            else
            {
                if (showDebug) Debug.Log($"A crop is created at {position}");
                return true;
            }
        }
        else
        {
            if (showDebug) Debug.Log($"Need Farm Plot, use the Hoe to create Farm Plot at {position}");
            return false;
        }
    }
}

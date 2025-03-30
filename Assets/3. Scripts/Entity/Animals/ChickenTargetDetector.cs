using System;
using UnityEngine;

public class ChickenTargetDetector : TargetDetector
{
    protected override bool ValidateValidTarget(Transform target, out EntityStatus targetStatus)
    {
        return base.ValidateValidTarget(target, out targetStatus) && target.GetComponent<Plant>().IsHarvestable;
    }

    protected override void OnPostTargetDetected(EntityStatus targetStatus)
    {
        if (targetStatus.TryGetComponent(out Plant plant))
        {
            plant.OnHarvested += HandleOnPlantHarvested;
        }
    }

    private void HandleOnPlantHarvested(Plant plant)
    {
        plant.OnHarvested -= HandleOnPlantHarvested;
        DeselectTarget("Plant harvested");
    }
}
using UnityEngine;

public class HamsterTargetDetector : TargetDetector
{
    protected override bool ValidateValidTarget(Transform target, out EntityStatus targetStatus)
    {
        var plant = target.GetComponent<Plant>();
        if (plant.IsHarvestable && plant.Hunters.Count != 0) Debug.Log($"Found targeted plant, ignoring");
        return base.ValidateValidTarget(target, out targetStatus)
            && plant.IsHarvestable
            && plant.Hunters.Count == 0; // Ensure no other hunger stimulus is hunting the plant
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
using UnityEngine;

public class HarvestState : BehaviourState
{
    public HarvestState(Animal animal) : base(animal) { }

    public override void ExecuteState()
    {
        base.ExecuteState();
        if (Animal.TargetDetector.CurrentTarget == null) return;

        if (Animal.TargetDetector.DistanceToTarget <= Animal.ItemProperty.Range * 2f)
        {
            if (Animal.TargetDetector.CurrentTarget.TryGetComponent<Plant>(out var plant) && plant.IsHarvestable)
            {
                plant.GetHarvested(Animal.FollowStimulus.Owner);
                Animal.TargetDetector.DeselectTarget($"Harvested by {Animal.name}");
            }
            else
            {
                Animal.TargetDetector.DeselectTarget($"Invalid object or plant is not harvestable");
            }
        }
    }
}

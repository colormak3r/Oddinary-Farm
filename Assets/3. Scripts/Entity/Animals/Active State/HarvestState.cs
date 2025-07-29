using UnityEngine;

public class HarvestState : BehaviourState
{
    public HarvestState(Animal animal) : base(animal) { }

    public override void ExecuteState()
    {
        base.ExecuteState();
        if (AnimalBase.TargetDetector.CurrentTarget == null) return;

        if (AnimalBase.TargetDetector.DistanceToTarget <= AnimalBase.ItemProperty.Range * 2f)
        {
            if (AnimalBase.TargetDetector.CurrentTarget.TryGetComponent<Plant>(out var plant) && plant.IsHarvestable)
            {
                plant.GetHarvested(AnimalBase.FollowStimulus.Owner);
                AnimalBase.PlaySoundEffect(0);
                AnimalBase.TargetDetector.DeselectTarget($"HarvestState: Harvested by {AnimalBase.name}");
            }
            else
            {
                AnimalBase.TargetDetector.DeselectTarget($"HarvestState: Invalid object {AnimalBase.TargetDetector.CurrentTarget}");
            }
        }
    }
}

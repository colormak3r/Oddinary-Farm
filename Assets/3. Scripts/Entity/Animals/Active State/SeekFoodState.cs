using UnityEngine;

public class SeekFoodState : BehaviourState
{
    private Transform targetFood;
    private Transform cached_targetFood;

    public SeekFoodState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();
        animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, true);
    }

    public override void ExecuteState()
    {
        base.ExecuteState();
        targetFood = animal.HungerStimulus.TargetFood?.Transform;
        if (cached_targetFood != targetFood)
        {
            cached_targetFood = targetFood;
            if (targetFood != null) animal.MoveTo(targetFood.position);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        animal.StopMovement();
    }
}

using UnityEngine;

public class SeekFoodState : BehaviourState
{
    public SeekFoodState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();
        AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, true);
    }

    public override void ExecuteState()
    {
        base.ExecuteState();

        if (AnimalBase.HungerStimulus != null && AnimalBase.HungerStimulus.TargetFood != null)
        {
            AnimalBase.MoveDirection((AnimalBase.HungerStimulus.TargetFood.transform.position - AnimalBase.transform.position).normalized);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        AnimalBase.StopMovement();
    }
}

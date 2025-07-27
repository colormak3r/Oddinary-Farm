using UnityEngine;

public class SeekFoodState : BehaviourState
{
    public SeekFoodState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();
        Animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, true);
    }

    public override void ExecuteState()
    {
        base.ExecuteState();

        if (Animal.HungerStimulus != null && Animal.HungerStimulus.TargetFood != null)
        {
            Animal.MoveDirection((Animal.HungerStimulus.TargetFood.transform.position - Animal.transform.position).normalized);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        Animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        Animal.StopMovement();
    }
}

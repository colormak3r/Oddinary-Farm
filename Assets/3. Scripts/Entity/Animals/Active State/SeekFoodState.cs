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

        // Make sure the transform exists before accessing it
        if (Animal.HungerStimulus != null && Animal.HungerStimulus.TargetFood != null && Animal.HungerStimulus.TargetFood.Transform != null)
        {
            var targetFood = Animal.HungerStimulus.TargetFood.Transform;
            if (targetFood == null) return;
            Animal.MoveDirection((targetFood.position - Animal.transform.position).normalized);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        Animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        Animal.StopMovement();
    }
}

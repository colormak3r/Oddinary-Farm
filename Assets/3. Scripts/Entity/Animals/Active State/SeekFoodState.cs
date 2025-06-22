using UnityEngine;

public class SeekFoodState : BehaviourState
{
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

        // Make sure the transform exists before accessing it
        if (animal.HungerStimulus && animal.HungerStimulus.TargetFood != null && animal.HungerStimulus.TargetFood.Transform)
        {
            var targetFood = animal.HungerStimulus.TargetFood.Transform;
            if (targetFood == null) return;
            animal.MoveDirection((targetFood.position - animal.transform.position).normalized);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        animal.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
        animal.StopMovement();
    }
}

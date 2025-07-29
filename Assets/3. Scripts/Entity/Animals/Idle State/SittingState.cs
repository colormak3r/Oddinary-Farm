using UnityEngine;

public class SittingState : BehaviourState
{
    public SittingState(Animal animal) : base(animal)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_SITTING, true);
        AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_MOVING, false);
    }

    public override void ExitState()
    {
        base.ExitState();
        AnimalBase.Animator.SetBool(Animal.ANIMATOR_IS_SITTING, false);
    }
}

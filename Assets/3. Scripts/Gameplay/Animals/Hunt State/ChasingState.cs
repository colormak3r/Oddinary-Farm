using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChasingState : BehaviourState
{
    public ChasingState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();
        animal.Animator.SetBool("IsMoving", true);
    }

    public override void ExecuteState()
    {
        base.ExecuteState();
        if (animal.PreyDetector.CurrentPrey == null) return;
        var direction = animal.PreyDetector.CurrentPrey.transform.position - animal.transform.position;
        animal.MoveDirection(direction);
    }

    public override void ExitState()
    {
        base.ExitState();
        animal.Animator.SetBool("IsMoving", false);
        animal.StopMovement();
    }
}

using UnityEngine;

public class RunawayState : BehaviourState
{
    public RunawayState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();
        animal.Animator.SetBool("IsMoving", true);
        animal.Movement.SetSpeedMultiplier(1.5f);
    }

    public override void ExecuteState()
    {
        base.ExecuteState();
        if (animal.ThreatDetector.CurrentThreat != null)
        {
            var direction = (animal.transform.position - animal.ThreatDetector.CurrentThreat.position).normalized;
            animal.MoveDirection(direction);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        animal.Animator.SetBool("IsMoving", false);
        animal.Movement.SetSpeedMultiplier(1f);
        animal.StopMovement();
    }
}

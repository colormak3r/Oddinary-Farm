using UnityEngine;

public class RunawayState : BehaviourState
{

    public RunawayState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();
        AnimalBase.Animator.SetBool("IsMoving", true);
        AnimalBase.Movement.SetSpeedMultiplier(1.5f);
    }

    public override void ExecuteState()
    {
        base.ExecuteState();
        if (AnimalBase.ThreatDetector.CurrentThreat != null)
        {
            var direction = (AnimalBase.transform.position - AnimalBase.ThreatDetector.CurrentThreat.position).normalized;
            AnimalBase.MoveDirection(direction);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        AnimalBase.Animator.SetBool("IsMoving", false);
        AnimalBase.Movement.SetSpeedMultiplier(1f);
        AnimalBase.StopMovement();
    }
}

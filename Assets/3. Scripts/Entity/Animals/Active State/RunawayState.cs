using UnityEngine;

public class RunawayState : BehaviourState
{

    public RunawayState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();
        Animal.Animator.SetBool("IsMoving", true);
        Animal.Movement.SetSpeedMultiplier(1.5f);
    }

    public override void ExecuteState()
    {
        base.ExecuteState();
        if (Animal.ThreatDetector.CurrentThreat != null)
        {
            var direction = (Animal.transform.position - Animal.ThreatDetector.CurrentThreat.position).normalized;
            Animal.MoveDirection(direction);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        Animal.Animator.SetBool("IsMoving", false);
        Animal.Movement.SetSpeedMultiplier(1f);
        Animal.StopMovement();
    }
}

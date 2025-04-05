using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RoamingState : BehaviourState
{
    public RoamingState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();
        animal.Animator.SetBool("IsMoving", true);
        animal.MoveTo(animal.GetRandomPointInRange());
        animal.OnDestinationReached.AddListener(HandleOnDestinationReached);
    }

   /* private Vector3 position_cached = Vector3.one;
    private float nextStallCheck;
    private float lastMoveCommandTime;
    private float stallGracePeriod = 2f;
    public override void ExecuteState()
    {
        base.ExecuteState();
        if (Time.time > nextStallCheck)
        {
            nextStallCheck = Time.time + 1f;
            bool isStalled = Vector2.Distance(position_cached, animal.transform.position) < 0.01f;

            if (isStalled)
            {
                if (Time.time - lastMoveCommandTime > stallGracePeriod)
                {
                    animal.MoveTo(animal.GetRandomPointInRange());
                    lastMoveCommandTime = Time.time;
                }
            }
            else
            {
                position_cached = animal.transform.position;
            }
        }
    }*/

    public override void ExitState()
    {
        base.ExitState();
        animal.Animator.SetBool("IsMoving", false);
        animal.StopMovement();
        animal.OnDestinationReached.RemoveListener(HandleOnDestinationReached);
    }

    private void HandleOnDestinationReached()
    {
        animal.MoveTo(animal.GetRandomPointInRange());
    }
}


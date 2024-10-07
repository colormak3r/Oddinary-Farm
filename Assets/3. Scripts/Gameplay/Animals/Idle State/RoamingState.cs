using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Roaming", menuName = "Scriptable Objects/Behavior State/Idle/Roaming")]
public class RoamingState : IdleStateData
{
    [Header("Roaming Settings")]
    public float roamRange = 5f;

    private Vector2 destination;
    private float stateDuration;

    public override void Enter(IdleBehaviour idleBehaviour)
    {
        base.Enter(idleBehaviour);
        destination = (Vector2)Random.insideUnitSphere * roamRange;
        stateDuration = Duration.value;
        idleBehaviour.Animator.SetBool("IsMoving", true);
    }

    public override void Execute(IdleBehaviour idleBehaviour)
    {
        base.Execute(idleBehaviour);

        idleBehaviour.EntityMovement.MoveTo(destination);

        if (Vector2.Distance(idleBehaviour.transform.position, destination) < 0.1f)
        {
            idleBehaviour.ChooseNextState();
        }

        if (stateTimer >= stateDuration)
        {
            if (idleBehaviour.ShowDebug) Debug.Log($"{idleBehaviour.gameObject.name} got bored of moving.", this);
            idleBehaviour.ChooseNextState();
        }
    }

    public override void Exit(IdleBehaviour idleBehaviour)
    {
        base.Exit(idleBehaviour);
        idleBehaviour.Animator.SetBool("IsMoving", false);
    }
}

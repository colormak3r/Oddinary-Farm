using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Nibbling", menuName = "Scriptable Objects/Behavior State/Idle/Nibbling")]
public class NibblingState : IdleStateData
{
    private float stateDuration;

    public override void Enter(IdleBehaviour idleBehaviour)
    {
        base.Enter(idleBehaviour);
        stateDuration = Duration.value;
        idleBehaviour.Animator.SetBool("IsNibbling", true);
    }

    public override void Execute(IdleBehaviour idleBehaviour)
    {
        base.Execute(idleBehaviour);
        if (stateTimer >= stateDuration)
        {
            idleBehaviour.ChooseNextState();
        }
    }

    public override void Exit(IdleBehaviour idleBehaviour)
    {
        base.Exit(idleBehaviour);
        idleBehaviour.Animator.SetBool("IsNibbling", false);
    }
}

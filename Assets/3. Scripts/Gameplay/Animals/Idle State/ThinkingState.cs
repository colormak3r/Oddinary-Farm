using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Thinking", menuName = "Scriptable Objects/Behavior State/Idle/Thinking")]
public class ThinkingState : IdleStateData
{
    private float stateDuration;

    public override void Enter(IdleBehaviour idleBehaviour)
    {
        base.Enter(idleBehaviour);
        stateDuration = Duration.value;
    }

    public override void Execute(IdleBehaviour idleBehaviour)
    {
        base.Execute(idleBehaviour);
        if (stateTimer >= stateDuration)
        {
            idleBehaviour.ChooseNextState();
        }
    }
}

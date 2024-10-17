using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBehaviourState
{
    public void EnterState();

    public void ExecuteState();

    public void ExitState();
}
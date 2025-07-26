using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BehaviourState
{
    protected Animal Animal { get; private set; }

    public BehaviourState(Animal animal)
    {
        Animal = animal;
    }

    public virtual void EnterState() { }
    public virtual void ExecuteState() { }
    public virtual void ExitState() { }
}


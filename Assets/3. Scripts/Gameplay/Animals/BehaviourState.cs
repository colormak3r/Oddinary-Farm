using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BehaviourState
{
    protected Animal animal;

    public BehaviourState(Animal animal)
    {
        this.animal = animal;
    }

    public virtual void EnterState()
    {

    }

    public virtual void ExecuteState()
    {

    }

    public virtual void ExitState()
    {

    }
}

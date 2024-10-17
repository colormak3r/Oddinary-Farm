using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnimalState : IBehaviourState
{
    protected Animal animal;

    public AnimalState(Animal animal)
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

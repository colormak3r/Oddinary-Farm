using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPrimaryState : AnimalState
{
    private float nextAction;
    public AttackPrimaryState(Animal animal) : base(animal)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        animal.NetworkAnimator.SetTrigger("PrimaryAction");
    }

    public override void ExecuteState()
    {
        base.ExecuteState();
        if (Time.time > nextAction)
        {
            animal.Item.OnPrimaryAction(animal.PreyDetector.CurrentPrey.position);
            nextAction = Time.time + animal.Item.PropertyValue.PrimaryCdr;
        }
    }
}
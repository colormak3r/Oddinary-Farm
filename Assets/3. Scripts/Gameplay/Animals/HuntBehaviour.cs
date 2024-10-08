using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HuntStateData : BehaviorStateData
{

}


[RequireComponent(typeof(PreyDetector))]
public class HuntBehaviour : MonoBehaviour, IAnimalBehavior
{
    private PreyDetector preyDetector;
    private EntityMovement entityMovement;
    private Animator animator;

    private void Awake()
    {
        preyDetector = GetComponent<PreyDetector>();
        entityMovement = GetComponent<EntityMovement>();
        animator = GetComponentInChildren<Animator>();
    }


    public void StartBehavior()
    {
        animator.SetBool("IsMoving", true);
    }

    public void ExecuteBehavior()
    {
        if (preyDetector.CurrentPrey == null) return;

        entityMovement.MoveTo(preyDetector.CurrentPrey.position);
    }

    public void ExitBehavior()
    {
        animator.SetBool("IsMoving", false);
    }
}

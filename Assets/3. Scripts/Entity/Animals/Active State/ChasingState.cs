using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChasingState : BehaviourState
{
    private const float dodgeChance = 0.35f;
    private const float dodgeDuration = 0.5f;
    private const float dodgeStrength = 0.7f;
    private const float dodgeCooldown = 0.12f;

    private bool isDodging = false;
    private float dodgeTimer = 0f;
    private float nextDodge = 0f;
    private Vector2 dodgeDirection;

    public ChasingState(Animal animal) : base(animal)
    {

    }

    public override void EnterState()
    {
        base.EnterState();
        AnimalBase.Animator.SetBool("IsMoving", true);
        isDodging = false;
        nextDodge = Time.time + dodgeCooldown;
    }

    public override void ExecuteState()
    {
        base.ExecuteState();
        if (AnimalBase.TargetDetector.CurrentTarget == null) return;

        Vector2 direction = AnimalBase.TargetDetector.CurrentTarget.transform.position - AnimalBase.transform.position;
        var side = Vector2.Perpendicular(direction).normalized * (Random.value < 0.5f ? 1f : -1f);
        dodgeDirection = (direction.normalized * (1f - dodgeStrength) + side * dodgeStrength).normalized;

        if (!isDodging && Time.time > nextDodge && Random.value < dodgeChance * Time.deltaTime)
        {
            isDodging = true;
            dodgeTimer = Time.time + dodgeDuration;
        }

        if (isDodging && Time.time > dodgeTimer)
        {
            isDodging = false;
            nextDodge = Time.time + dodgeCooldown;
        }

        AnimalBase.MoveDirection(isDodging ? dodgeDirection : direction);
    }

    public override void ExitState()
    {
        base.ExitState();
        AnimalBase.Animator.SetBool("IsMoving", false);
        AnimalBase.StopMovement();
    }
}

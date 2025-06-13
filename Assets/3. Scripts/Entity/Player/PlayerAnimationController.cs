using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimationControllerClip
{
    Idle,
    Move,
    Chop,
    Shoot
}

public class PlayerAnimationController : AnimationBehaviour
{
    [Header("Player Animation Settings")]
    [SerializeField]
    private AnimationControllerClip clip = AnimationControllerClip.Idle;
    [SerializeField]
    private float animationLength = 0.25f;

    private PlayerController playerController;
    private Animator animator;

    public AnimationMode ChopAnimationMode { get; set; }

    private void Awake()
    {
        playerController = transform.root.GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
    }

    public void Chop()
    {
        //if (playerController) playerController.Chop(ChopAnimationMode);
    }

    [ContextMenu("Play Animation")]
    public void PlayAnimation()
    {
        PlayAnimation(clip);
    }

    protected override IEnumerator PlayAnimationCoroutineInternal()
    {
        //AudioManager.Main.PlaySoundIncreasePitch(TutorialUI.Main.TutorialSound);
        PlayAnimation(clip);
        yield return new WaitForSeconds(animationLength);
    }

    public void PlayAnimation(AnimationControllerClip clip)
    {
        StopAllCoroutines();
        switch (clip)
        {
            case AnimationControllerClip.Idle:
                SetAnimation(false);
                break;
            case AnimationControllerClip.Move:
                SetAnimation(true);
                break;
            case AnimationControllerClip.Chop:
                SetAnimation(false, true, false);
                break;
            case AnimationControllerClip.Shoot:
                SetAnimation(false, false, true);
                break;
        }
    }

    private IEnumerator ChopCoroutine(float delay)
    {
        while (true)
        {
            SetAnimation(false, true, false);
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator ShootCoroutine(float delay)
    {
        while (true)
        {
            SetAnimation(false, false, true);
            yield return new WaitForSeconds(delay);
        }
    }

    public void SetAnimation(bool isMoving, bool chop = false, bool shoot = false)
    {
        animator.SetBool("IsMoving", isMoving);
        if (chop) animator.SetTrigger("Chop");
        if (shoot) animator.SetTrigger("Shoot");
    }
}

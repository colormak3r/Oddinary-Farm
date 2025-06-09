using System.Collections;
using UnityEngine;

public class SpriteAnimation : AnimationBehaviour
{
    [Header("Sprite Animation Settings")]
    [SerializeField]
    private float animationLength = 0.25f;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public override void StartAnimation()
    {
        animator.SetTrigger("Reset");
    }

    public override void ResetAnimation()
    {
        animator.SetTrigger("Reset");
    }

    protected override IEnumerator PlayAnimationCoroutineInternal()
    {
        AudioManager.Main.PlaySoundIncreasePitch(TutorialUI.Main.TutorialSound);
        animator.SetTrigger("Play");
        yield return new WaitForSeconds(animationLength);
    }
}

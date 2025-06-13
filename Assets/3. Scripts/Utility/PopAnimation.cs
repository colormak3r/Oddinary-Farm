using ColorMak3r.Utility;
using System.Collections;
using UnityEngine;

public class PopAnimation : AnimationBehaviour
{
    [Header("Pop Animation Settings")]
    [SerializeField]
    private Vector3 defaultScale = Vector3.one;
    [SerializeField]
    private bool popIn = true;
    [SerializeField]
    private bool popOut = true;
    [SerializeField]
    private Vector2 popInScale = new Vector2(1f, 1.5f);
    [SerializeField]
    private float popInDuration = 0.15f;
    [SerializeField]
    private Vector2 popOutScale = new Vector2(1.5f, 1f);
    [SerializeField]
    private float popOutDuration = 0.15f;

    public override void StartAnimation()
    {
        transform.localScale = defaultScale;
    }

    public override void ResetAnimation()
    {
        StartCoroutine(transform.PopCoroutine(transform.localScale.x, defaultScale.x, popOutDuration));
    }

    protected override IEnumerator PlayAnimationCoroutineInternal()
    {
        AudioManager.Main.PlaySoundIncreasePitch(TutorialUI.Main.TutorialSound);
        if (popIn) yield return transform.PopCoroutine(popInScale.x, popInScale.y, popInDuration);
        if (popOut) yield return transform.PopCoroutine(popOutScale.x, popOutScale.y, popOutDuration);
    }
}

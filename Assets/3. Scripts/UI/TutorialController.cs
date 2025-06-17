using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialController : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField]
    private AnimationBehaviour[] animationBehaviours;
    [SerializeField]
    private float animationDelay = 0.1f;
    [SerializeField]
    private float loopDelay = 3f;
    [SerializeField]
    private float resetDelay = 1f;

    private AnimationBehaviour[] allBehaviours;
    private bool isPlaying = false;

    private void Start()
    {
        allBehaviours = GetComponentsInChildren<AnimationBehaviour>();
    }

    public void PlayAnimation()
    {
        StopAllCoroutines();

        isPlaying = true;
        StartCoroutine(PlayAnimationCoroutine());
    }

    private IEnumerator PlayAnimationCoroutine()
    {
        while (isPlaying)
        {
            AudioManager.Main.ResetPitch();

            foreach (var behaviour in allBehaviours)
            {
                if (behaviour != null) behaviour.StartAnimation();
            }

            foreach (var animationBehaviour in animationBehaviours)
            {
                if (animationBehaviour != null) yield return animationBehaviour.PlayAnimationCoroutine();
                yield return new WaitForSeconds(animationDelay);
            }

            yield return new WaitForSeconds(loopDelay);

            foreach (var behaviour in allBehaviours)
            {
                if (behaviour != null) behaviour.ResetAnimation();
            }

            yield return new WaitForSeconds(resetDelay);
        }
    }

    public void StopAnimation()
    {
        isPlaying = false;
        StopAllCoroutines();
        foreach (var behaviour in allBehaviours)
        {
            if (behaviour != null)
            {
                behaviour.StopAnimation();
                behaviour.StopAllCoroutines();
            }
        }
    }
}

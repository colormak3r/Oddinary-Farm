using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationBehaviour : MonoBehaviour
{
    [Header("Animation Behaviour Settings")]
    [SerializeField]
    private float animationDelay = 0f;
    [SerializeField]
    [Tooltip("List of animation behaviours to play in parallel with this behaviour")]
    private AnimationBehaviour[] parallelBehaviours;

    public virtual void StartAnimation()
    {

    }

    public virtual void ResetAnimation()
    {

    }

    public IEnumerator PlayAnimationCoroutine()
    {
        yield return new WaitForSeconds(animationDelay);
        var coroutines = new List<Coroutine>();

        // Start the animation for this behaviour
        var selfCoroutine = StartCoroutine(PlayAnimationCoroutineInternal());
        coroutines.Add(selfCoroutine);

        // Start the animations for all parallel behaviours
        foreach (var behaviour in parallelBehaviours)
        {
            if (behaviour != null)
            {
                yield return new WaitForSeconds(behaviour.animationDelay);
                var coroutine = StartCoroutine(behaviour.PlayAnimationCoroutineInternal());
                coroutines.Add(coroutine);
            }
        }

        // Wait for all animations to complete
        foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }
    }


    protected virtual IEnumerator PlayAnimationCoroutineInternal()
    {
        yield return null;
    }
}

using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementAnimation : AnimationBehaviour
{
    [Header("Movement Animation Settings")]
    [SerializeField]
    private bool loop = false;
    [SerializeField]
    private bool popInOut = false;
    [SerializeField]
    private Vector2 startPosition;
    [SerializeField]
    private List<Vector2> coordPosition = new List<Vector2>();
    [SerializeField]
    private float moveDuration = 0.1f;

    [Header("Debug Settings")]
    [SerializeField]
    private bool showGizmos = true;

    private Coroutine loopCoroutine;

    public override void StartAnimation()
    {
        if (popInOut) transform.localScale = Vector3.zero;

        if (loop)
        {
            if (loopCoroutine == null) loopCoroutine = StartCoroutine(LoopCoroutine());
        }
        else
        {
            transform.position = startPosition;
        }
    }

    public override void ResetAnimation()
    {
        if (!loop) StartCoroutine(transform.LerpMoveCoroutine(startPosition, moveDuration));
    }

    private IEnumerator LoopCoroutine()
    {
        while (loop)
        {
            if (popInOut) yield return transform.PopCoroutine(0f, 1f, moveDuration);

            for (int i = 0; i < coordPosition.Count; i++)
            {
                yield return transform.LerpMoveCoroutine(startPosition + coordPosition[i], moveDuration);
            }

            for (int i = coordPosition.Count - 1; i >= 0; i--)
            {
                yield return transform.LerpMoveCoroutine(startPosition + coordPosition[i], moveDuration);
            }

            if (popInOut) yield return transform.PopCoroutine(1f, 0f, moveDuration);
        }
    }

    protected override IEnumerator PlayAnimationCoroutineInternal()
    {
        if (popInOut) yield return transform.PopCoroutine(0f, 1f, moveDuration);

        AudioManager.Main.PlaySoundIncreasePitch(TutorialUI.Main.TutorialSound);

        for (int i = 0; i < coordPosition.Count; i++)
        {
            yield return transform.LerpMoveCoroutine(startPosition + coordPosition[i], moveDuration);
        }

        if (popInOut) yield return transform.PopCoroutine(1f, 0f, moveDuration);

        if (loop)
        {
            StartCoroutine(PlayAnimationCoroutineInternal());
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(startPosition, 0.1f);
        for (int i = 1; i < coordPosition.Count; i++)
        {
            Gizmos.DrawLine(startPosition + coordPosition[i - 1], startPosition + coordPosition[i]);
            Gizmos.DrawSphere(startPosition + coordPosition[i], 0.1f);
        }
    }

    public void UpdateStartPosition()
    {
        startPosition = transform.position;
    }

    public void UpdateCoordPosition()
    {
        Vector2 offset = (Vector2)transform.position - startPosition;
        coordPosition.Add(offset);
    }
}
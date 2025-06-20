using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class GraphicsUI : MonoBehaviour, IPointerEnterHandler
{
    //for hot air balloon graphics in main menu
    [Header("Graphics UI Settings")]
    [SerializeField]
    private float xDriftRange = 0.2f;
    [SerializeField]
    private float yDriftRange = 0.3f;
    [SerializeField]
    private float driftSpeed = 1.0f;

    [Header("Cursor Interaction Variables")]
    [SerializeField]
    private float pushAmount = 5f;
    [SerializeField]
    private float pushDuration = 0.1f;
    [SerializeField]
    private float scaleFactor = 0.1f;

    private Vector2 originalLocalPos;
    private Coroutine driftRoutine;
    private Coroutine pushRoutine;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        originalLocalPos = rectTransform.anchoredPosition;
        driftRoutine = StartCoroutine(FloatDrift());
    }

    private void OnDisable()
    {
        if (driftRoutine != null)
        {
            StopCoroutine(driftRoutine);
            driftRoutine = null;
        }

        if (pushRoutine != null)
        {
            StopCoroutine(pushRoutine);
            pushRoutine = null;
        }
    }

    //for hot air balloon cursor interaction
    public void OnPointerEnter(PointerEventData eventData)
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, 
            eventData.position, 
            canvas.worldCamera, 
            out localMousePos
        );
        
        Vector2 balloonLocalPos = rectTransform.anchoredPosition;
        Vector2 pushDir = (balloonLocalPos - localMousePos).normalized;
        Vector2 targetPos = originalLocalPos + (pushDir * pushAmount * scaleFactor);

        if (pushRoutine != null)
            StopCoroutine(pushRoutine);

        pushRoutine = StartCoroutine(PushAndReturn(targetPos));
    }

    private IEnumerator PushAndReturn(Vector2 pushedPosition)
    {
        float elapsed = 0f;

        while (elapsed < pushDuration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(originalLocalPos, pushedPosition, elapsed / pushDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = pushedPosition;

        yield return new WaitForSeconds(0.3f);

        elapsed = 0f;
        while (elapsed < pushDuration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(pushedPosition, originalLocalPos, elapsed / pushDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = originalLocalPos;
        pushRoutine = null;
    }

    //for hot air balloon idle animation
    private IEnumerator FloatDrift()
    {
        float timeOffset = Random.Range(0f, 100f);

        while (true)
        {
            float time = Time.time + timeOffset;
            float offsetX = Mathf.PerlinNoise(time * driftSpeed, 0f) * 2f - 1f;
            float offsetY = Mathf.PerlinNoise(0f, time * driftSpeed) * 2f - 1f;

            if (pushRoutine == null)
            {
                rectTransform.anchoredPosition = originalLocalPos + new Vector2(offsetX * xDriftRange, offsetY * yDriftRange);
            }

            yield return null;
        }
    }
}

/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/28/2025
 * Last Modified:   07/28/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AutoScroll : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField]
    private ScrollRect scrollRect;
    [SerializeField]
    private bool backToTopOnEnable = true;
    [SerializeField]
    private float scrollSpeed = 0.1f;
    [SerializeField]
    private float resumeDelay = 1f; // Optional delay before resuming full speed

    private float currentSpeed = 0f;
    private float resumeTimer = 0f;
    private float previousPosition;
    private bool userInteracted = false;

    private void OnEnable()
    {
        if (backToTopOnEnable)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
        userInteracted = false;
        currentSpeed = scrollSpeed;
        previousPosition = scrollRect.verticalNormalizedPosition;
    }

    void Update()
    {
        float currentPosition = scrollRect.verticalNormalizedPosition;
        float delta = currentPosition - previousPosition;

        // Detect scroll direction
        if (Mathf.Abs(delta) > 0.001f)
        {
            if (delta > 0f)
            {
                // Scrolling up
                currentSpeed = 0f;
                resumeTimer = resumeDelay;
            }
            else if (delta < 0f)
            {
                // Scrolling down (by user)
                resumeTimer = resumeDelay;
            }
        }

        // Resume speed after delay
        if (resumeTimer > 0f)
        {
            resumeTimer -= Time.deltaTime;
            if (resumeTimer <= 0f)
            {
                currentSpeed = scrollSpeed;
            }
        }

        // Apply auto scroll only if not at bottom and speed > 0
        if (!userInteracted && currentPosition > 0f && currentSpeed > 0f)
        {
            scrollRect.verticalNormalizedPosition -= currentSpeed * Time.deltaTime;
        }

        previousPosition = currentPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        userInteracted = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        userInteracted = true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorUI : MonoBehaviour
{
    public static CursorUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    [Header("Cursor Settings")]
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private RectTransform parentTransform;
    [SerializeField]
    private RectTransform cursorTransform;
    [SerializeField]
    private float cursorSpeed = 500f;
    [SerializeField]
    private float lerpSpeed = 10f;


    /// <summary>
    /// Moves the cursor based on an input direction (e.g., from a controller stick),
    /// ensuring it stays within the bounds of the parent RectTransform.
    /// </summary>
    /// <param name="direction">Movement direction (e.g., from a joystick)</param>
    public void MoveCursor(Vector2 direction)
    {
        // --- 1) Get the current anchoredPosition
        Vector2 currentPos = cursorTransform.anchoredPosition;

        // --- 2) Movement in SCREEN SPACE: 
        //     Multiply by (1 / scaleFactor) so that if your Canvas is scaled up (scaleFactor > 1),
        //     you don't move extra-fast in local coordinates. 
        float scaleFactor = (canvas != null) ? canvas.scaleFactor : 1f;

        // Time-based input
        // If you want the same "visual" speed regardless of scaling, divide by scaleFactor:
        Vector2 delta = direction * (cursorSpeed * Time.deltaTime / scaleFactor);

        // Target position in local space
        Vector2 targetPos = currentPos + delta;

        // --- 3) Clamp inside parent bounds
        //
        // Because the parent pivot is (0.5, 0.5), its local coordinate space in X goes:
        //  from -parentWidth/2   to +parentWidth/2,
        // and Y goes from -parentHeight/2  to +parentHeight/2.
        //
        // If your Canvas is in Overlay mode, parentTransform.rect.width is likely the *reference* width
        // (e.g. 1920 if that’s your reference resolution). At runtime, the *actual* rendered size
        // might be scaleFactor * referenceWidth, but anchoredPosition remains in reference-pixel space.

        float parentWidth = parentTransform.rect.width;
        float parentHeight = parentTransform.rect.height;

        // Calculate half of the cursor rect, based on pivot
        float halfWidth = cursorTransform.rect.width * cursorTransform.pivot.x;
        float halfHeight = cursorTransform.rect.height * cursorTransform.pivot.y;

        // The local space bounding:
        float minX = -parentWidth / 2f + halfWidth;
        float maxX = parentWidth / 2f - halfWidth;
        float minY = -parentHeight / 2f + halfHeight;
        float maxY = parentHeight / 2f - halfHeight;

        // Apply clamp
        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        // --- 4) Lerp from currentPos -> targetPos for smooth movement
        Vector2 smoothPos = Vector2.Lerp(currentPos, targetPos, lerpSpeed * Time.deltaTime);

        // --- 5) Assign final position
        cursorTransform.anchoredPosition = smoothPos;
    }
}

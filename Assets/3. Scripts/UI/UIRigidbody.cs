/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/12/2025
 * Last Modified:   07/12/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEngine;

public class UIRigidbody : MonoBehaviour
{
    [Header("UI Rigidbody Settings")]
    [SerializeField]
    private float launchForce = 800f;  // Initial speed
    [SerializeField]
    private float gravity = -1500f;    // Gravity force
    [SerializeField]
    private float destroyAfter = -1f; // Time to destroy the object, -1 means never destroy

    [Header("Debugs")]
    [SerializeField]
    private Vector2 velocity;

    private RectTransform rectTransform;
    private float destroyTimer;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Launch in a random direction (upward bias)
        float angle = Random.Range(60f, 120f); // Degrees
        float rad = angle * Mathf.Deg2Rad;
        velocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * launchForce;
        destroyTimer = Time.time + destroyAfter;
    }

    private void Update()
    {
        if (destroyAfter > 0 && Time.time > destroyTimer)
        {
            Destroy(gameObject);
            return;
        }

        // Apply gravity to vertical velocity
        velocity.y += gravity * Time.deltaTime;

        // Update position
        Vector2 pos = rectTransform.anchoredPosition;
        pos += velocity * Time.deltaTime;
        rectTransform.anchoredPosition = pos;

        /*// Optional: clamp to bottom of screen
        if (pos.y < 0)
        {
            pos.y = 0;
            velocity = Vector2.zero;
            rectTransform.anchoredPosition = pos;
        }*/
    }
}

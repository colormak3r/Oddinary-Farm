using UnityEngine;

public class RotateAnimation : MonoBehaviour
{
    [Header("Torque Settings")]
    [SerializeField]
    private float torqueMultiplier = 1.0f;
    [SerializeField]
    private float minimumTorque = 5.0f;

    [Header("Rigidbody References")]
    [SerializeField]
    private Rigidbody2D mainRb;
    [SerializeField]
    private Rigidbody2D spriteRb;

    // This is called by the animation event
    [ContextMenu("Rotate")]
    public void Rotate()
    {
        // Get the Cow's vertical speed (Y axis)
        float verticalSpeed = mainRb.linearVelocity.y;

        // The faster downward = more torque, take absolute value
        float torqueAmount = Mathf.Abs(verticalSpeed) * torqueMultiplier;

        // Enforce minimum torque
        if (torqueAmount < minimumTorque)
        {
            torqueAmount = minimumTorque;
        }

        // Apply random direction
        float direction = Random.value < 0.5f ? -1f : 1f;

        spriteRb.AddTorque(torqueAmount * direction);
    }
}

/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/23/2025
 * Last Modified:   06/25/2025 (Ryan)
 * Notes:           Handles all mount actions including
 *                  Player movement input
*/
using Unity.Netcode;
using UnityEngine;

public abstract class MountController : NetworkBehaviour
{
    [SerializeField] protected float movementSpeed;     // Speed of mount when mounted

    [SerializeField] protected bool debug = false;

    public virtual void SubscribeMovementInput()
    {
        PlayerController.OnPlayerMovementInput += Move;
        if (debug) Debug.Log("Mount Movement Subscribed On Client.");
    }

    public virtual void UnsubscribeMovementInput()
    {
        PlayerController.OnPlayerMovementInput -= Move;
        if (debug) Debug.Log("Mount Movement Unsubscribed On Client.");
    }

    // Must include a Move Method
    protected abstract void Move(Vector2 motion, float deltaTime);
}


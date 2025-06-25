/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/25/2025
 * Last Modified:   06/25/2025 (Ryan)
 * Notes:           Basic mount behavior for testing
*/
using UnityEngine;

public class BasicMountController : MountController
{
    protected override void Move(Vector2 motion, float deltaTime)
    {
        // Handle Movement
        transform.position += (Vector3)motion * movementSpeed * deltaTime;
    }
}


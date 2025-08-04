/*
 * Created By:      Khoa Nguyen
 * Date Created:    08/03/2025
 * Last Modified:   08/03/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEngine;

public class CowMountController : MountController
{
    public override void Move(Vector2 direction)
    {
        mountMovement.SetDirection(direction);
        mountAnimator.SetBool("IsMoving", direction != Vector2.zero);
    }
}

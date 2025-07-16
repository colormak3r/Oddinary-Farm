/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/23/2025
 * Last Modified:   06/27/2025 (Ryan)
 * Notes:           Handles the mounting and movement of the hot air balloon
*/
using UnityEngine;

public class HotAirBalloonMountController : MountController
{
    [SerializeField] private Collider2D  physicCollider;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    protected override void HandleOnMount(Transform source)
    {
        base.HandleOnMount(source);
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    protected override void HandleOnDismount(Transform source)
    {
        base.HandleOnDismount(source);
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    public override void Move(Vector2 direction)
    {
        Debug.Log($"Player is Moving Balloon = {direction}");
        //movement.SetDirection(direction);
    }
}

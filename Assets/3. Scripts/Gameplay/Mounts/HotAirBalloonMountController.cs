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
        if (!CanMove)
            return;

        // Player can only move horizontally while in the hot air balloon
        Vector2 totalDir = new Vector2(direction.x, 1f);
        movement.SetDirection(totalDir);
        Debug.Log($"Player is Moving Balloon = {totalDir}");
    }
}

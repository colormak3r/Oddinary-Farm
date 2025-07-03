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
        movement.SetDirection(direction);
    }
}

using UnityEngine;

public class LassoProjectile : Projectile
{
    [Header("Lasso Settings")]
    [SerializeField]
    private Transform lassoPoint;
    public Transform LassoPoint => lassoPoint;

    protected override void OnTriggerEnter2D(Collider2D collider)
    {
        if (!isInitialized || owner == null) return;

        if (showDebugs) Debug.Log(collider.transform.root.name);

        if (collider.transform.root.TryGetComponent<CaptureController>(out var captureController))
        {
            if (!captureController.IsLassoedValue)
            {
                if (isAuthoritative)
                {
                    HitDespawn(collider.transform.root);
                }
                else
                {
                    Despawn();
                }
            }
            else
            {
                if (showDebugs) Debug.Log("Already lassoed");
            }
        }
    }
}
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

        if (collider.transform.root.TryGetComponent<CaptureController>(out var captureController) && captureController.CaptureType == CaptureType.Lasso)
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
        else
        {
            if (showDebugs) Debug.Log("No CaptureController found on " + collider.transform.root.name + ".Keep Moving.");
        }
    }
}
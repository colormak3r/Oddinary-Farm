using System;
using Unity.Netcode;
using UnityEngine;


public enum LassoState
{
    Hidden,
    Visible,
    Thrown,
}


public class LassoController : NetworkBehaviour
{
    [Header("Lasso Settings")]
    [SerializeField]
    private LassoRenderer handLassoRenderer;
    [SerializeField]
    private LassoRenderer lineLassoRenderer;
    [SerializeField]
    private GameObject lassoHoop;
    [SerializeField]
    private GameObject lassoExtra;
    [SerializeField]
    private Transform muzzleTransform;
    [SerializeField]
    private ProjectileProperty projectileProperty;

    [SerializeField]
    private NetworkVariable<LassoState> currentLassoState = new NetworkVariable<LassoState>(LassoState.Hidden, default, NetworkVariableWritePermission.Owner);
    public LassoState CurrentStateValue => currentLassoState.Value;

    private CaptureController captureController;

    public override void OnNetworkSpawn()
    {
        currentLassoState.OnValueChanged += HandleLassoStateChanged;
        HandleLassoStateChanged(LassoState.Hidden, currentLassoState.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentLassoState.OnValueChanged -= HandleLassoStateChanged;
    }

    private void HandleLassoStateChanged(LassoState previousValue, LassoState newValue)
    {
        switch (newValue)
        {
            case LassoState.Hidden:
                handLassoRenderer.SetRenderLine(false);
                lineLassoRenderer.SetRenderLine(false);
                lineLassoRenderer.SetTarget(null);
                lassoHoop.SetActive(false);
                lassoExtra.SetActive(false);
                break;
            case LassoState.Visible:
                handLassoRenderer.SetRenderLine(true);
                lineLassoRenderer.SetRenderLine(false);
                lineLassoRenderer.SetTarget(null);
                lassoHoop.SetActive(true);
                lassoExtra.SetActive(true);
                break;
            case LassoState.Thrown:
                handLassoRenderer.SetRenderLine(true);
                lineLassoRenderer.SetRenderLine(false);
                lassoHoop.SetActive(false);
                lassoExtra.SetActive(true);
                break;
        }
    }

    public void SetLassoState(LassoState newState)
    {
        if (IsOwner)
        {
            currentLassoState.Value = newState;
        }
    }

    public void ThrowLasso(Vector2 position)
    {
        if (currentLassoState.Value == LassoState.Thrown) return;
        currentLassoState.Value = LassoState.Thrown;

        ThrowLassoRpc(position);
    }

    [Rpc(SendTo.Everyone)]
    private void ThrowLassoRpc(Vector2 position)
    {
        var direction = ((Vector3)position - muzzleTransform.position).normalized;
        var rotation = Quaternion.LookRotation(Vector3.forward, Quaternion.Euler(0, 0, 90) * direction);
        var projectile = LocalObjectPooling.Main.Spawn(AssetManager.Main.LassoProjectilePrefab);
        projectile.transform.position = muzzleTransform.position;
        projectile.transform.rotation = rotation;
        var lassoProjectile = projectile.GetComponent<LassoProjectile>();
        lassoProjectile.Initialize(transform, projectileProperty, IsOwner);
        lassoProjectile.OnDespawned += OnLassoDespawned;
        if (IsOwner) lassoProjectile.OnHitTarget += OnLassoHitTarget;
        lineLassoRenderer.SetTarget(lassoProjectile.LassoPoint);
        lineLassoRenderer.SetRenderLine(true);
    }

    private void OnLassoDespawned(Projectile projectile)
    {
        lineLassoRenderer.SetTarget(null);
        lineLassoRenderer.SetRenderLine(false);
        projectile.OnDespawned -= OnLassoDespawned;
        if (IsOwner)
        {
            currentLassoState.Value = LassoState.Visible;
            projectile.OnHitTarget -= OnLassoHitTarget;
        }
    }

    private void OnLassoHitTarget(Projectile projectile, Transform target)
    {
        LinkLassoRpc(target.gameObject);
        if (IsOwner)
        {
            projectile.OnHitTarget -= OnLassoHitTarget;
        }
    }


    [Rpc(SendTo.Everyone)]
    private void LinkLassoRpc(NetworkObjectReference targetObject)
    {
        if (targetObject.TryGet(out var target))
        {
            if (target.TryGetComponent<CaptureController>(out var captureController))
            {
                lineLassoRenderer.SetTarget(captureController.LassoPoint);
                lineLassoRenderer.SetRenderLine(true);
                if (IsOwner) captureController.Capture(CaptureType.Lasso);
            }
        }
        else
        {
            Debug.LogError("Target object not found");
        }
    }

    public void SetCaptureController(CaptureController captureController)
    {
        this.captureController = captureController;
    }

    public void CancelLasso()
    {
        if (currentLassoState.Value == LassoState.Thrown)
        {
            currentLassoState.Value = LassoState.Visible;
            captureController?.CancelLasso();
        }
    }
}

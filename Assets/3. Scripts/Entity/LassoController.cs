using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;


public enum LassoState
{
    Hidden,
    Visible,
    Thrown,
    Capturing,
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
    private float pullForce = 20f;
    [SerializeField]
    private float captureDistance = 2f;
    [SerializeField]
    private float speedMultiplier = 0.4f;

    [SerializeField]
    private NetworkVariable<LassoState> currentLassoState = new NetworkVariable<LassoState>(LassoState.Hidden, default, NetworkVariableWritePermission.Owner);
    public LassoState CurrentStateValue => currentLassoState.Value;

    [Header("Lasso Settings")]
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private bool isRecovering = false;
    public bool IsRecovering => isRecovering;

    private CaptureController currentCaptureController;
    private LassoProjectile currentLassoProjectile;
    private EntityMovement entityMovement;
    private Coroutine lassoRecoveryCoroutine;

    private void Awake()
    {
        entityMovement = GetComponent<EntityMovement>();
    }

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
                if (showDebugs) Debug.Log("Lasso is hidden");
                handLassoRenderer.SetRenderLine(false);
                lineLassoRenderer.SetRenderLine(false);
                lineLassoRenderer.SetTarget(null);
                lassoHoop.SetActive(false);
                lassoExtra.SetActive(false);
                break;
            case LassoState.Visible:
                if (showDebugs) Debug.Log("Lasso is visible");
                handLassoRenderer.SetRenderLine(true);
                lineLassoRenderer.SetRenderLine(false);
                lineLassoRenderer.SetTarget(null);
                lassoHoop.SetActive(true);
                lassoExtra.SetActive(true);
                break;
            case LassoState.Thrown:
                if (showDebugs) Debug.Log("Lasso is thrown");
                handLassoRenderer.SetRenderLine(true);
                lassoHoop.SetActive(false);
                lassoExtra.SetActive(true);
                break;
            case LassoState.Capturing:
                if (showDebugs) Debug.Log("Lasso is capturing");
                handLassoRenderer.SetRenderLine(true);
                lineLassoRenderer.SetRenderLine(true);
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

    #region Lasso Throw

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
        currentLassoProjectile = projectile.GetComponent<LassoProjectile>();
        currentLassoProjectile.Initialize(transform, projectileProperty, IsOwner);
        currentLassoProjectile.OnDespawned += OnLassoDespawned;
        if (IsOwner) currentLassoProjectile.OnHitTarget += OnLassoHitTarget;
        lineLassoRenderer.SetTarget(currentLassoProjectile.LassoPoint);
        lineLassoRenderer.SetRenderLine(true);
    }

    #endregion

    #region Lasso Callback

    private void OnLassoDespawned(Projectile projectile)
    {
        //if (currentLassoState.Value == LassoState.Thrown) return;

        lineLassoRenderer.SetTarget(null);
        lineLassoRenderer.SetRenderLine(false);
        projectile.OnDespawned -= OnLassoDespawned;
        if (IsOwner)
        {
            currentLassoState.Value = LassoState.Visible;
            projectile.OnHitTarget -= OnLassoHitTarget;
        }

        currentLassoProjectile = null;
    }

    private void OnLassoHitTarget(Projectile projectile, Transform target)
    {
        LinkLassoRpc(target.gameObject);
        if (IsOwner)
        {
            projectile.OnHitTarget -= OnLassoHitTarget;
        }

        currentLassoProjectile = null;
    }

    #endregion

    [Rpc(SendTo.Everyone)]
    private void LinkLassoRpc(NetworkObjectReference targetObject)
    {
        if (targetObject.TryGet(out var target))
        {
            if (target.TryGetComponent<CaptureController>(out var captureController))
            {
                lineLassoRenderer.SetTarget(captureController.LassoPoint);
                lineLassoRenderer.SetRenderLine(true);

                if (IsOwner)
                {
                    currentLassoState.Value = LassoState.Capturing;
                    currentCaptureController = captureController;
                    currentCaptureController.Capture(CaptureType.Lasso);
                    entityMovement.SetSpeedMultiplier(speedMultiplier);
                }

                TryDespawnProjectile();
            }
            else
            {
                Debug.LogError($"{target.name} does not have a CaptureController component", target);
            }
        }
        else
        {
            Debug.LogError($"Target object not found");
        }
    }

    #region Lasso Cancel

    public void CancelLasso()
    {
        if (currentLassoState.Value == LassoState.Thrown)
        {
            currentLassoState.Value = LassoState.Visible;
            currentCaptureController?.CancelLasso();
        }
        else if (currentLassoState.Value == LassoState.Capturing)
        {
            currentLassoState.Value = LassoState.Visible;
            currentCaptureController?.CancelLasso();
            entityMovement.SetSpeedMultiplier(1f);
        }

        CancelLassoRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void CancelLassoRpc()
    {
        TryDespawnProjectile();
    }

    #endregion

    #region Lasso Action

    public void LassoPull()
    {
        if (currentLassoState.Value != LassoState.Capturing || currentCaptureController == null)
        {
            Debug.LogError("State failure", this);
        }

        var targetPos = currentCaptureController.transform.position;
        var distance = Vector3.Distance(targetPos, transform.position);
        if (distance < captureDistance)
        {
            currentCaptureController.CaptureLassoSuccess();
            if (lassoRecoveryCoroutine != null) StopCoroutine(lassoRecoveryCoroutine);
            lassoRecoveryCoroutine = StartCoroutine(LassoRecoveryCoroutine());
            CancelLasso();
        }
        else
        {
            currentCaptureController.EntityMovement.KnockbackDirection(pullForce, (transform.position - targetPos).normalized);
        }
    }

    private IEnumerator LassoRecoveryCoroutine()
    {
        isRecovering = true;
        yield return new WaitForSeconds(1f);
        isRecovering = false;
    }

    #endregion

    private void TryDespawnProjectile()
    {
        if (currentLassoProjectile && currentLassoProjectile.gameObject.activeInHierarchy)
        {
            currentLassoProjectile.DespawnOnClient();
            currentLassoProjectile = null;
        }
    }
}

using System;
using Unity.Netcode;
using UnityEngine;

public enum CaptureType
{
    Net,
    Lasso,
}

public class CaptureController : NetworkBehaviour
{
    [Header("Capture Settings")]
    [SerializeField]
    private bool isCaptureable = true;
    public bool IsCaptureable => isCaptureable && captureItemProperty != null;
    [SerializeField]
    private CaptureType captureType;
    public CaptureType CaptureType => captureType;
    [SerializeField]
    private ItemProperty captureItemProperty;
    public ItemProperty CaptureItemProperty => captureItemProperty;
    [SerializeField]
    private Transform lassoPoint;
    public Transform LassoPoint => lassoPoint;
    [SerializeField]
    private SpriteRenderer lassoedRenderer;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;


    private NetworkVariable<bool> IsLassoed = new NetworkVariable<bool>(false);
    public bool IsLassoedValue => IsLassoed.Value;

    private EntityMovement entityMovement;
    public EntityMovement EntityMovement => entityMovement;

    private void Awake()
    {
        entityMovement = GetComponent<EntityMovement>();
    }

    public override void OnNetworkSpawn()
    {
        IsLassoed.OnValueChanged += HandleLassoedChanged;
    }

    public override void OnNetworkDespawn()
    {
        IsLassoed.OnValueChanged -= HandleLassoedChanged;
    }

    private void HandleLassoedChanged(bool previousValue, bool newValue)
    {
        lassoedRenderer.enabled = newValue;
    }

    public void Capture(CaptureType type)
    {
        if (!isCaptureable)
        {
            Debug.LogError("This entity is not captureable.");
            return;
        }

        if (captureItemProperty == null)
        {
            Debug.LogError("Capture item property is not set.");
            return;
        }

        if (type != captureType)
        {
            if (showDebugs) Debug.Log("Wrong capture type");
            return;
        }

        switch (type)
        {
            case CaptureType.Net:
                NetCaptureRpc();
                break;
            case CaptureType.Lasso:
                LassoCaptureRpc();
                break;
            default:
                Debug.LogError("Invalid capture type");
                break;
        }
    }

    [Rpc(SendTo.Server)]
    private void NetCaptureRpc()
    {
        AssetManager.Main.SpawnItem(captureItemProperty, transform.position);
        Destroy(gameObject);
    }

    [Rpc(SendTo.Server)]
    private void LassoCaptureRpc()
    {
        IsLassoed.Value = true;
    }

    public void CancelLasso()
    {
        if (!IsLassoedValue) return;

        LassoCancelRpc();
    }

    [Rpc(SendTo.Server)]
    private void LassoCancelRpc()
    {
        IsLassoed.Value = false;
    }

    public void CaptureLassoSuccess()
    {
        // Update stats
        StatisticManager.Main.UpdateStat(StatisticType.AnimalsCaptured, gameObject.name, 1);

        CaptureLassoSuccessRpc();
    }

    [Rpc(SendTo.Server)]
    private void CaptureLassoSuccessRpc()
    {
        AssetManager.Main.SpawnItem(captureItemProperty, transform.position);
        Destroy(gameObject);
    }
}

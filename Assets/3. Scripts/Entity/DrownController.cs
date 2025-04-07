using ColorMak3r.Utility;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DrownController : NetworkBehaviour
{
    [Header("Drown Settings")]
    [SerializeField]
    private float drownTickRate = 1f;
    [SerializeField]
    private NetworkVariable<bool> CanBeDrowned = new NetworkVariable<bool>(true);

    private Rigidbody2D rbody;
    private EntityStatus entityStatus;
    private FloodManager floodManager;
    private WorldGenerator worldGenerator;
    private DrownGraphic drownGraphic;

    private Coroutine drownCoroutine;

    void Awake()
    {
        rbody = GetComponent<Rigidbody2D>();
        entityStatus = GetComponent<EntityStatus>();
        drownGraphic = GetComponentInChildren<DrownGraphic>();
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        CanBeDrowned.OnValueChanged += HandleCanBeDrownedChanged;

        if (IsServer)
        {
            floodManager = FloodManager.Main;
            worldGenerator = WorldGenerator.Main;
            floodManager.OnFloodLevelChanged += HandleOnFloodLevelChange;
            HandleOnFloodLevelChange(floodManager.CurrentFloodLevelValue, floodManager.CurrentWaterLevel);
            if (rbody != null) StartCoroutine(DynamicDrownBootstrap());
        }

    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        CanBeDrowned.OnValueChanged -= HandleCanBeDrownedChanged;

        if (IsServer)
        {
            floodManager.OnFloodLevelChanged -= HandleOnFloodLevelChange;
        }
    }

    private void HandleCanBeDrownedChanged(bool previousValue, bool newValue)
    {
        if (drownGraphic) drownGraphic.SetCanBeDrowned(newValue);
    }

    #region Flood Level

    private void HandleOnFloodLevelChange(float currentFloodLevel, float waterLevel)
    {
        if (!CanBeDrowned.Value) return;

        if (rbody == null)
        {
            StartCoroutine(StaticDrownBootstrap(currentFloodLevel));
        }
        else
        {
            if (worldGenerator.IsInitialized)
                CheckFloodLevel(((Vector2)transform.position).SnapToGrid());
        }
    }

    private Vector2 position_cached = Vector2.one;
    private IEnumerator DynamicDrownBootstrap()
    {
        yield return new WaitUntil(() => worldGenerator.IsInitialized);
        while (IsSpawned)
        {
            var position = ((Vector2)transform.position).SnapToGrid();
            if (position != position_cached)
            {
                position_cached = position;
                CheckFloodLevel(position);
            }
            yield return null;
        }
    }

    private void CheckFloodLevel(Vector2 position)
    {
        if (!CanBeDrowned.Value) return;

        if (floodManager.CurrentFloodLevelValue > worldGenerator.GetElevation((int)position.x, (int)position.y))
        {
            if (drownCoroutine == null) drownCoroutine = StartCoroutine(DrownCoroutine());
        }
        else
        {
            if (drownCoroutine != null) { StopCoroutine(drownCoroutine); drownCoroutine = null; }
        }
    }

    private IEnumerator StaticDrownBootstrap(float currentFloodLevel)
    {
        yield return new WaitUntil(() => worldGenerator.IsInitialized);

        var position = ((Vector2)transform.position).SnapToGrid();
        if (currentFloodLevel > worldGenerator.GetElevation((int)position.x, (int)position.y))
        {
            if (drownCoroutine != null) StopCoroutine(drownCoroutine);
            drownCoroutine = StartCoroutine(DrownCoroutine());
        }
    }

    private IEnumerator DrownCoroutine()
    {
        if (entityStatus == null)
        {
            if (IsServer)
                Destroy(gameObject);
        }
        else
        {
            while (entityStatus.CurrentHealthValue > 0)
            {
                yield return new WaitForSeconds(drownTickRate);
                entityStatus.TakeDamage(1, DamageType.Water, Hostility.Neutral, transform);
            }
        }
    }

    public void SetCanBeDrowned(bool canBeDrowned)
    {
        SetCanBeDrownedRpc(canBeDrowned);
    }

    [Rpc(SendTo.Server)]
    private void SetCanBeDrownedRpc(bool canBeDrowned)
    {
        CanBeDrowned.Value = canBeDrowned;
        if (!canBeDrowned)
        {
            StopAllCoroutines();
        }
    }

    #endregion
}

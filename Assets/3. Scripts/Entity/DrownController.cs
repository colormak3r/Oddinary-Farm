using ColorMak3r.Utility;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

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
            HandleOnFloodLevelChange(floodManager.CurrentFloodLevelValue, floodManager.CurrentSafeLevel, floodManager.CurrentDepthLevel);
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
        //if (drownGraphic) drownGraphic.SetCanBeDrowned(newValue);
    }

    #region Flood Level

    private void HandleOnFloodLevelChange(float floodLevel, float waterLevel, float depthLevel)
    {
        if (!CanBeDrowned.Value) return;

        if (rbody == null)
        {
            StartCoroutine(StaticDrownBootstrap(depthLevel));
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

        var elevation = WorldGenerator.Main.GetElevation(position.x, position.y);
        if (floodManager.CurrentDepthLevel > elevation)
        {
            if (drownCoroutine == null) drownCoroutine = StartCoroutine(DrownCoroutine());
        }
        else
        {
            if (drownCoroutine != null) { StopCoroutine(drownCoroutine); drownCoroutine = null; }
        }
    }

    private IEnumerator StaticDrownBootstrap(float currentDepthLevel)
    {
        yield return new WaitUntil(() => worldGenerator.IsInitialized);
        CheckFloodLevel(transform.position);
        /*var position = ((Vector2)transform.position).SnapToGrid();
        if (currentDepthLevel > worldGenerator.GetElevation(position.x, position.y))
        {
            if (drownCoroutine != null) StopCoroutine(drownCoroutine);
            drownCoroutine = StartCoroutine(DrownCoroutine());
        }*/
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

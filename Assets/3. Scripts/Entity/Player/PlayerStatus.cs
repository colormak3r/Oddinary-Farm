using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerStatus : EntityStatus
{
    [Header("Player Settings")]
    private Transform respawnPoint;

    private NetworkVariable<FixedString128Bytes> GUID = new NetworkVariable<FixedString128Bytes>();

    public string GUIDValue => GUID.Value.ToString();

    private IControllable[] controllables;

    private void Start()
    {
        controllables = GetComponentsInChildren<IControllable>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            GUID.Value = Guid.NewGuid().ToString();
        }
    }

    protected override void HandleCurrentHealthChange(uint previousValue, uint newValue)
    {
        base.HandleCurrentHealthChange(previousValue, newValue);
        if (!IsOwner) return;

        PlayerStatusUI.Main.UpdateHealth(CurrentHealthValue);
    }

    protected override void OnEntityDeathOnServer()
    {
        OnDeathOnServer?.Invoke();
        OnDeathOnServer.RemoveAllListeners();
        OnEntitySpawnOnServer();
    }

    protected override void OnEntityDeathOnClient()
    {
        base.OnEntityDeathOnClient();

        var colliders = GetComponentsInChildren<Collider2D>();
        var renderer = GetComponentsInChildren<SpriteRenderer>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
        foreach (var render in renderer)
        {
            render.enabled = false;
        }

        if (!IsOwner) return;

        var respawnPosition = Vector3.zero;
        if (respawnPoint != null)
            respawnPosition = respawnPoint.position;

        StartCoroutine(DelayMoveCamera(transform.position, respawnPosition));

    }

    private IEnumerator DelayMoveCamera(Vector3 deathPos, Vector3 respawnPos)
    {
        Camera.main.transform.position = new Vector3(deathPos.x, deathPos.y, Camera.main.transform.position.z);
        foreach (var controllable in controllables)
        {
            controllable.SetControllable(false);
        }

        yield return new WaitForSeconds(3f);
        yield return TransitionUI.Main.ShowCoroutine();
        transform.position = respawnPos;
        yield return new WaitUntil(() => !WorldGenerator.Main.IsGenerating);
        yield return TransitionUI.Main.UnShowCoroutine();

        var colliders = GetComponentsInChildren<Collider2D>();
        var renderer = GetComponentsInChildren<SpriteRenderer>();
        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }
        foreach (var render in renderer)
        {
            render.enabled = true;
        }

        foreach (var controllable in controllables)
        {
            controllable.SetControllable(true);
        }

        Camera.main.transform.localPosition = new Vector3(0, 0, Camera.main.transform.position.z);
    }
}

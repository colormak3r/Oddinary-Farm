using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ComponentStatus : EntityStatus
{
    [Header("Component Status")]
    [SerializeField]
    private string componentName;
    [SerializeField]
    private SubComponent[] subComponents;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        foreach (var subComponent in subComponents)
        {
            if (subComponent != null)
            {
                subComponent.Initialize(this);
            }
        }
    }

    protected override void OnEntityDamagedOnClient(uint damage, NetworkObjectReference attackerRef)
    {
        foreach (var subComponent in subComponents)
        {
            if (subComponent != null)
            {
                subComponent.DamageFlash();
            }
        }
    }

    protected override IEnumerator DeathOnClientCoroutine()
    {
        foreach (var subComponent in subComponents)
        {
            if (subComponent != null)
            {
                subComponent.Destroyed();
            }
        }
        yield return null;
    }

    protected override void OnEntityDeathOnServer()
    {

    }
}
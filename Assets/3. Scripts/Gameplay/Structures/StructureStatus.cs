using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureStatus : EntityStatus
{
    private Structure structure;

    protected override void Awake()
    {
        base.Awake();
        structure = GetComponent<Structure>();
    }

    protected override IEnumerator DeathOnClientCoroutine()
    {
        structure.DestroyOnClient();
        return base.DeathOnClientCoroutine();
    }
}

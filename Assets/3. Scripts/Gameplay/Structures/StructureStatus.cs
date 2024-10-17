using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureStatus : EntityStatus
{
    private Structure structure;

    private void Awake()
    {
        structure = GetComponent<Structure>();    
    }

    protected override void OnEntityDeathOnServer()
    {
        structure.Remove();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureStatus : EntityStatus
{
    private SpriteBlender spriteBlender;

    protected override void Awake()
    {
        base.Awake();
        spriteBlender = GetComponentInChildren<SpriteBlender>();
    }

    protected override IEnumerator DeathOnClientCoroutine()
    {
        spriteBlender.ReblendNeighbors();
        yield return base.DeathOnClientCoroutine();
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

public class Axe : MeleeWeapon
{
    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        DealDamage(position);
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PickAxe : MeleeWeapon
{
    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        DealDamage(position);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pistol : RangedWeapon
{
    override public void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        ShootProjectiles(position);
    }
}

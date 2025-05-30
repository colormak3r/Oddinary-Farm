using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileGun : RangedWeapon
{
    override public void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        ItemSystem.ShootProjectiles(position, rangedWeaponProperty);
    }
}

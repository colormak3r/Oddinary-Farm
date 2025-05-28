using Unity.Netcode;
using UnityEngine;

public class RangedWeapon : Item
{
    protected RangedWeaponProperty rangedWeaponProperty { get; private set; }

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        rangedWeaponProperty = (RangedWeaponProperty)baseProperty;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || rangedWeaponProperty == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(PlayerController.MuzzleTransform.position, PlayerController.LookPosition);
    }
}
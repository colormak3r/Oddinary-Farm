using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class Hand : MeleeWeapon
{
    private HandProperty handProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        handProperty = (HandProperty)baseProperty;
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        ItemSystem.DealDamage(position, handProperty);
    }
}
using UnityEngine;

public class Hand : MeleeWeapon
{
    private HandProperty handProperty;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        handProperty = (HandProperty)newValue;
    }

    public override bool CanPrimaryAction(Vector2 position)
    {
        return IsInRange(position);
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        HandAction(position);
    }

    private void HandAction(Vector2 position)
    {
        Harvest(position);
        DealDamage(position);
    }
}
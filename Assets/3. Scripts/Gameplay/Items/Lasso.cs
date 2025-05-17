using UnityEngine;

public class Lasso : Item
{
    private LassoProperty lassoProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        lassoProperty = baseProperty as LassoProperty;
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        ItemSystem.ThrowLasso(position);
    }

    public override void OnSecondaryAction(Vector2 position)
    {
        base.OnSecondaryAction(position);
        ItemSystem.CancelLasso();
    }
}
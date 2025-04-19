using ColorMak3r.Utility;
using UnityEngine;

public class Consummable : Item
{
    private ConsummableProperty consummableProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        consummableProperty = (ConsummableProperty)baseProperty;
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        ItemSystem.UseConsummableSelf(consummableProperty);
    }
}
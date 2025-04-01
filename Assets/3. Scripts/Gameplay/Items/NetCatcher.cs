using UnityEngine;

public class NetCatcher : Item
{
    [SerializeField]
    private NetCatcherProperty netCatcherProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        netCatcherProperty = (NetCatcherProperty)baseProperty;
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        ItemSystem.NetCapture(position, netCatcherProperty.Range);
    }
}

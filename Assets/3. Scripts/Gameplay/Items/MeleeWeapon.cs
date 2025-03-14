using UnityEngine;

public class MeleeWeapon : Item
{
    [SerializeField]
    private MeleeWeaponProperty meleeWeaponProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        meleeWeaponProperty = (MeleeWeaponProperty)baseProperty;
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        ItemSystem.DealDamage(position, meleeWeaponProperty);
    }

    public override void OnSecondaryAction(Vector2 position)
    {
        base.OnSecondaryAction(position);
        ItemSystem.RemoveStructure(position, LayerManager.Main.StructureLayer);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || meleeWeaponProperty == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeWeaponProperty.Range);
    }
}

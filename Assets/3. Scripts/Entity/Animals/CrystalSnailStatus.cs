/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/26/2025
 * Last Modified:   07/26/2025 (Khoa)
 * Notes:           <write here>
*/

using Unity.Netcode;
using UnityEngine;

public class CrystalSnailStatus : EntityStatus
{
    [Header("Crystal Snail Settings")]
    [SerializeField]
    private CurrencyProperty currencyProperty;

    /*public void Mine(Transform source)
    {
        Debug.Log($"Mining from Crystal Snail at {transform.position} by {source.name}");
        TakeDamage(1, DamageType.Absolute, Hostility.Absolute, source);
    }*/

    protected override void OnEntityDamagedOnServer(uint damage, NetworkObjectReference attackerRef)
    {
        base.OnEntityDamagedOnServer(damage, attackerRef);
        for (int i = 0; i < damage; i++)
        {
            AssetManager.Main.SpawnItem(currencyProperty, transform.position, attackerRef);
        }
    }
}

using System;
using UnityEngine;

[System.Serializable]
public struct ShopTier
{
    public ulong netIncome;
    public ulong upgradeCost;
    public ItemProperty[] itemProperties;
}

[CreateAssetMenu(fileName = "Shop Inventory", menuName = "Scriptable Objects/Shop Inventory")]
public class ShopInventory : ScriptableObject
{
    [SerializeField]
    private string shopName;
    [SerializeField]
    private float saleMultiplier = 0.9f;
    [SerializeField]
    private float penaltyMultiplier = 0.7f;
    [SerializeField]
    private ShopTier[] tiers;
    [SerializeField]
    private ItemProperty[] itemProperties;  // Obsolete, use ItemByTier instead

    public string ShopName => shopName;
    public float SaleMultiplier => saleMultiplier;
    public float PenaltyMultiplier => penaltyMultiplier;
    public ShopTier[] Tiers => tiers;
    [Obsolete("Use ShopTier instead")]
    public ItemProperty[] ItemProperties => itemProperties; // Obsolete, use ItemByTier instead
}
using UnityEngine;

[System.Serializable]
public struct ShopTierItem
{
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
    private ulong[] shopTierBaseCost;
    [SerializeField]
    private ShopTierItem[] shopTierItems;
    [SerializeField]
    private ItemProperty[] itemProperties;

    public string ShopName => shopName;
    public float SaleMultiplier => saleMultiplier;
    public float PenaltyMultiplier => penaltyMultiplier;
    public ulong[] ShopTierBaseCost => shopTierBaseCost;
    public ShopTierItem[] ShopTierItems => shopTierItems;
    public ItemProperty[] ItemProperties => itemProperties;
}
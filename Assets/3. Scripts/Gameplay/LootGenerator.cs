using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LootGenerator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private LootTable lootTable;

    /*[ContextMenu("Drop Loot")]
    public void DropLoot()
    {
        DropLootOnServer(true);
    }*/

    public void Initialize(LootTable lootTable)
    {
        this.lootTable = lootTable;
    }

    public void DropLootOnServer(Transform prefer = null)
    {
        if (lootTable == null) return;

        var loots = GenerateLoot(lootTable);
        var position = transform.position;

        foreach (ItemStack loot in loots)
        {
            for (int i = 0; i < loot.Count; i++)
            {
                if(prefer)
                    AssetManager.Main.SpawnItemPrefer(loot.Property, position, prefer.gameObject, 0);
                else
                    AssetManager.Main.SpawnItem(loot.Property, position, 0);
            }
        }
    }

    private List<ItemStack> GenerateLoot(LootTable lootTable)
    {
        List<ItemStack> loots = new List<ItemStack>();

        foreach (ItemCountProbability rngGod in lootTable.Table)
        {
            if (Random.value > rngGod.probability) continue;

            ItemStack itemStack = new ItemStack(rngGod.item, (uint)Random.Range(rngGod.minMaxCount.min, rngGod.minMaxCount.max));
            loots.Add(itemStack);
        }

        return loots;
    }
}

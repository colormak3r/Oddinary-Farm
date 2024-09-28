using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Plant : NetworkBehaviour, IWaterable, IHarvestable
{
    [Header("Settings")]
    [SerializeField]
    private PlantProperty mockProperty;
    [SerializeField]
    private LayerMask farmPlotLayer;

    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<int> CurrentStage = new NetworkVariable<int>();
    [SerializeField]
    private NetworkVariable<bool> isWatered;

    private NetworkVariable<PlantProperty> Property = new NetworkVariable<PlantProperty>();

    private SpriteRenderer spriteRenderer;
    private LootGenerator lootGenerator;

    private bool IsHarvestable => Property.Value.Stages[CurrentStage.Value].isHarvestStage;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lootGenerator = GetComponentInChildren<LootGenerator>();
    }

    public override void OnNetworkSpawn()
    {
        HandlePropertyChanged(Property.Value, Property.Value);
        Property.OnValueChanged += HandlePropertyChanged;
        CurrentStage.OnValueChanged += HandleStageChanged;
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandlePropertyChanged;
    }

    private void HandlePropertyChanged(PlantProperty previous, PlantProperty current)
    {
        HandlePropertyChanged(current);
    }

    private void HandlePropertyChanged(PlantProperty property)
    {
        HandleStageChanged(CurrentStage.Value);
    }

    private void HandleStageChanged(int previous, int current)
    {
        HandleStageChanged(current);
    }

    private void HandleStageChanged(int stage)
    {
        if (Property == null || Property.Value == null) return;

        spriteRenderer.sprite = Property.Value.Stages[stage].sprite;
    }

    [ContextMenu("Mock Property Change")]
    public void MockPropertyChange()
    {
        if (!IsHost) return;
        Property.Value = mockProperty;
    }

    public void Initialize(PlantProperty property)
    {
        Property.Value = property;
        CurrentStage.Value = 0;
    }

    public void GetWatered(float duration)
    {
        GetWateredRpc();
    }

    [Rpc(SendTo.Server)]
    private void GetWateredRpc()
    {
        if (isWatered.Value) return;

        if (CurrentStage.Value < Property.Value.Stages.Length - 1)
        {
            StartCoroutine(GrowthCoroutine());
        }
    }

    private IEnumerator GrowthCoroutine()
    {
        var stage = Property.Value.Stages[CurrentStage.Value];
        yield return new WaitForSeconds(stage.duration);

        CurrentStage.Value++;

        isWatered.Value = false;
    }

    public void GetHarvested()
    {
        GetHarvestedRpc();
    }

    [Rpc(SendTo.Server)]
    private void GetHarvestedRpc()
    {
        if (!IsHarvestable) return;
                
        lootGenerator.DropLoot(true);
        Destroy(gameObject);
    }
}

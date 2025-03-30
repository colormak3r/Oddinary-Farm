using ColorMak3r.Utility;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Plant : NetworkBehaviour, IWaterable, IItemInitable
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
    private EntityStatus entityStatus;
    private FarmPlot farmPlot;

    public bool IsHarvestable => Property.Value.Stages[CurrentStage.Value].isHarvestStage;
    public ItemProperty Seed => Property.Value.SeedProperty;

    public Action<Plant> OnHarvested;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lootGenerator = GetComponentInChildren<LootGenerator>();
        entityStatus = GetComponent<EntityStatus>();
    }

    public override void OnNetworkSpawn()
    {
        HandlePropertyChanged(Property.Value, Property.Value);
        Property.OnValueChanged += HandlePropertyChanged;
        CurrentStage.OnValueChanged += HandleStageChanged;

        if (IsServer)
        {
            WeatherManager.Main.OnRainStarted.AddListener(HandleRainStarted);
        }
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandlePropertyChanged;

        if (IsServer)
        {
            WeatherManager.Main.OnRainStarted.RemoveListener(HandleRainStarted);
        }
    }

    private void HandlePropertyChanged(PlantProperty previous, PlantProperty current)
    {
        HandleStageChanged(0, CurrentStage.Value);
    }

    private void HandleStageChanged(int previous, int current)
    {
        if (Property == null || Property.Value == null) return;

        spriteRenderer.sprite = Property.Value.Stages[current].sprite;

        if (IsServer)
            if (WeatherManager.Main.IsRainning) HandleRainStarted();
    }

    private void HandleRainStarted()
    {
        GetWateredRpc();
    }


    [ContextMenu("Mock Property Change")]
    public void MockPropertyChange()
    {
        if (!IsHost) return;
        Property.Value = mockProperty;
    }

    public void Initialize(ScriptableObject baseProperty)
    {
        var property = (PlantProperty)baseProperty;
        if (property == null)
        {
            Debug.LogError("PlantProperty is null, cannot initialize Plant.");
            return;
        }

        Property.Value = property;
        lootGenerator.Initialize(property.LootTable);
        CurrentStage.Value = 0;

        var farmPlotHit = Physics2D.OverlapPoint(((Vector2)transform.position).SnapToGrid(), farmPlotLayer);
        if (farmPlotHit != null)
        {
            farmPlot = farmPlotHit.GetComponent<FarmPlot>();
            farmPlot.GetDriedOnServer();
        }
    }


    public void GetWatered()
    {
        GetWateredRpc();
    }

    [Rpc(SendTo.Server)]
    private void GetWateredRpc()
    {
        if (isWatered.Value) return;
        isWatered.Value = true;

        if (CurrentStage.Value < Property.Value.Stages.Length - 1)
        {
            StartCoroutine(GrowthCoroutine());
        }

        entityStatus.GetHealed(1);
    }

    private IEnumerator GrowthCoroutine()
    {
        var stage = Property.Value.Stages[CurrentStage.Value];
        yield return new WaitForSeconds(stage.duration * TimeManager.Main.HourDuration);

        if (CurrentStage.Value < Property.Value.Stages.Length - 1)
            CurrentStage.Value += stage.stageIncrement;

        isWatered.Value = false;

        if (WeatherManager.Main.IsRainning)
        {
            GetWateredRpc();
        }
        else
        {
            if (!IsHarvestable)
                farmPlot.GetDriedOnServer();
        }
    }

    public void GetHarvested(Transform harvester)
    {
        GetHarvestedRpc(harvester.gameObject);
    }

    [Rpc(SendTo.Server)]
    private void GetHarvestedRpc(NetworkObjectReference harvester)
    {
        if (!IsHarvestable) return;

        lootGenerator.DropLootOnServer(harvester);
        OnHarvested?.Invoke(this);

        var stage = Property.Value.Stages[CurrentStage.Value];
        CurrentStage.Value += stage.stageIncrement;

        farmPlot.GetDriedOnServer();
        isWatered.Value = false;
        if (WeatherManager.Main.IsRainning)
        {
            GetWateredRpc();
        }
    }
}

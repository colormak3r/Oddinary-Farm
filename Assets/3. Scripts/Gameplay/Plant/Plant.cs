using ColorMak3r.Utility;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Plant : NetworkBehaviour, IWaterable, IItemInitable, IConsummable
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
    public PlantProperty PropertyValue => Property.Value;

    private SpriteRenderer spriteRenderer;
    private LootGenerator lootGenerator;
    private EntityStatus entityStatus;
    private FarmPlot farmPlot;

    public bool IsHarvestable => Property.Value.Stages[CurrentStage.Value].isHarvestStage;
    public ItemProperty Seed => Property.Value.SeedProperty;
    public FoodColor FoodColor => Property.Value.FoodColor;
    public FoodType FoodType => Property.Value.FoodType;
    public Transform Transform => transform;
    public bool CanBeConsumed => IsHarvestable;

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
        if (!IsServer)
        {
            Debug.LogError("Initialize call not on server", this);
            return;
        }

        var property = (PlantProperty)baseProperty;
        if (property == null)
        {
            Debug.LogError($"Cannot initialize Plant, baseProperty = {baseProperty}", this);
            return;
        }

        Property.Value = property;
        lootGenerator.Initialize(property.LootTable);
        CurrentStage.Value = 0;

        var farmPlotHit = Physics2D.OverlapPoint(((Vector2)transform.position).SnapToGrid(), farmPlotLayer);
        if (farmPlotHit != null)
        {
            farmPlot = farmPlotHit.GetComponent<FarmPlot>();
            if (farmPlot.IsWateredValue) GetWateredOnServer();
        }
    }

    public void GetWatered()
    {
        GetWateredRpc();
    }

    [Rpc(SendTo.Server)]
    private void GetWateredRpc()
    {
        GetWateredOnServer();
    }

    private void GetWateredOnServer()
    {
        if (isWatered.Value) return;
        isWatered.Value = true;

        StartGrowing();
    }

    private void StartGrowing()
    {
        if (CurrentStage.Value < Property.Value.Stages.Length - 1)
        {
            StartCoroutine(GrowthCoroutine());
        }
    }

    private IEnumerator GrowthCoroutine()
    {
        var stage = Property.Value.Stages[CurrentStage.Value];
        yield return new WaitForSeconds(stage.duration * TimeManager.Main.HourDuration);

        if (CurrentStage.Value < Property.Value.Stages.Length - 1)
            CurrentStage.Value += stage.stageIncrement;

        // Heal plant when grown a stage
        entityStatus.GetHealed(1);

        if (IsHarvestable)
        {
            isWatered.Value = false;
            farmPlot.GetDriedOnServer();
        }
        else
        {
            StartGrowing();
            farmPlot.GetWatered();
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
        GetHarvestedOnServer();
    }


    public bool Consume()
    {
        if (!IsHarvestable)
        {
            return false;
        }
        else
        {
            GetHarvestedOnServer();
            return true;
        }
    }

    private void GetHarvestedOnServer()
    {
        OnHarvested?.Invoke(this);

        if (Property.Value.DestroyOnHarvest)
        {
            farmPlot.GetDriedOnServer();
            Destroy(gameObject);
        }
        else
        {
            var stage = Property.Value.Stages[CurrentStage.Value];
            CurrentStage.Value += stage.stageIncrement;

            if (WeatherManager.Main.IsRainning)
            {
                GetWateredRpc();
            }
            else
            {
                farmPlot.GetDriedOnServer();
                isWatered.Value = false;
            }
        }
    }

    [ContextMenu("FullyGrow")]
    public void FullyGrown()
    {
        FullyGrownRpc();
    }

    [Rpc(SendTo.Server)]
    private void FullyGrownRpc()
    {
        CurrentStage.Value = Property.Value.Stages.Length - 1;
    }
}

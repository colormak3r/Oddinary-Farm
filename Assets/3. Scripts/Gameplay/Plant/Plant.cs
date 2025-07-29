/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/22/2025 (Khoa)
 * Notes:           <write here>
*/

using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Plant : NetworkBehaviour, IWaterable, IItemInitable, IConsummable
{
    [Header("Settings")]
    [SerializeField]
    private PlantProperty mockProperty;
    [SerializeField]
    private PlantProperty goldenCarrotProperty;
    [SerializeField]
    private CurrencyProperty copperCoinProperty;
    [SerializeField]
    private LayerMask farmPlotLayer;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private NetworkVariable<int> CurrentStage = new NetworkVariable<int>();
    [SerializeField]
    private NetworkVariable<bool> isWatered;
    [SerializeField]
    private NetworkVariable<PlantProperty> Property = new NetworkVariable<PlantProperty>();
    public PlantProperty PropertyValue => Property.Value;

    private SpriteRenderer spriteRenderer;
    private LootGenerator lootGenerator;
    private EntityStatus entityStatus;
    private FarmPlot farmPlot;
    private MapElement mapElement;

    public bool IsHarvestable => Property.Value.Stages[CurrentStage.Value].isHarvestStage;
    public ItemProperty Seed => Property.Value.SeedProperty;
    public FoodColor FoodColor => Property.Value.FoodColor;
    public FoodType FoodType => Property.Value.FoodType;
    public GameObject GameObject => gameObject;
    public bool CanBeConsumed => IsHarvestable;

    private HashSet<HungerStimulus> hunters = new HashSet<HungerStimulus>();
    public HashSet<HungerStimulus> Hunters => hunters;
    public void AddToHunterList(HungerStimulus hungerStimulus) => hunters.Add(hungerStimulus);
    public void RemoveFromHunterList(HungerStimulus hungerStimulus) => hunters.Remove(hungerStimulus);

    public Action<Plant> OnHarvested;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lootGenerator = GetComponentInChildren<LootGenerator>();
        entityStatus = GetComponent<EntityStatus>();
        mapElement = GetComponent<MapElement>();
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
        CurrentStage.OnValueChanged -= HandleStageChanged;

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
        if (showDebugs) Debug.Log($"Plant {name} watered on server", this);

        StartGrowing();
    }


    private Coroutine growthCoroutine;
    private void StartGrowing()
    {
        if (CurrentStage.Value < Property.Value.Stages.Length - 1)
        {
            if (growthCoroutine != null) return;
            growthCoroutine = StartCoroutine(GrowthCoroutine());
        }
    }

    private IEnumerator GrowthCoroutine()
    {
        if (showDebugs) Debug.Log($"Plant {name} started growing", this);

        var stage = Property.Value.Stages[CurrentStage.Value];
        yield return new WaitForSeconds(stage.duration * TimeManager.Main.HourDuration);

        if (CurrentStage.Value < Property.Value.Stages.Length - 1)
            CurrentStage.Value = Mathf.Min(CurrentStage.Value + stage.stageIncrement, Property.Value.Stages.Length - 1);

        // Heal plant when grown a stage
        entityStatus.GetHealed(1);
        mapElement.ResetHeatValue();

        growthCoroutine = null;

        if (IsHarvestable)
        {
            if (showDebugs) Debug.Log($"Plant {name} is harvestable now", this);
            isWatered.Value = false;
            farmPlot.GetDriedOnServer();
        }
        else
        {
            if (showDebugs) Debug.Log($"Plant {name} is not harvestable yet, continuing to grow", this);
            StartGrowing();
            farmPlot.GetWatered();
        }
    }

    public void GetHarvested(Transform harvester)
    {
        if (harvester.TryGetComponent(out PlayerStatus playerStatus))
        {
            if (playerStatus.CurrentCurse == PlayerCurse.GoldenCarrot)
            {
                // If player has Golden Carrot effect, they get a bad harvest if the plant is not a golden carrot
                GetHarvestedRpc(harvester.gameObject, Property.Value != goldenCarrotProperty);
            }
            else
            {

                // If player does not have Golden Carrot effect, they get a bad harvest if the plant is a golden carrot
                GetHarvestedRpc(harvester.gameObject, Property.Value == goldenCarrotProperty);
            }
        }
        else
        {
            GetHarvestedRpc(harvester.gameObject, false);
        }
    }

    [Rpc(SendTo.Server)]
    private void GetHarvestedRpc(NetworkObjectReference harvester, bool badHarvest)
    {
        if (!IsHarvestable) return;

        if (badHarvest)
            AssetManager.Main.SpawnItem(copperCoinProperty, transform.position, harvester);
        else
            lootGenerator.DropLoot(harvester);

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
            CurrentStage.Value = Mathf.Min(CurrentStage.Value + stage.stageIncrement, Property.Value.Stages.Length - 1);

            if (WeatherManager.Main.IsRainning)
            {
                if (showDebugs) Debug.Log($"Plant {name} is watered by rain after harvest", this);
                // isWatered should already be true here
                // isWatered.Value = true;
                StartGrowing();
            }
            else
            {
                if (showDebugs) Debug.Log($"Plant {name} is dried after harvest", this);
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
